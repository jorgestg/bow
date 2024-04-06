namespace Bow.Compiler.Symbols;

public enum PrimitiveTypeKind
{
    None,
    Bool,
    Float32,
    Float64,
    Never,
    Signed8,
    Signed16,
    Signed32,
    Signed64,
    Unit,
    Unsigned8,
    Unsigned16,
    Unsigned32,
    Unsigned64
}

public abstract class TypeSymbol : Symbol
{
    public virtual PrimitiveTypeKind PrimitiveTypeKind => PrimitiveTypeKind.None;

    /// <summary>
    /// Determines whether a value of the current type can be assigned to a variable of the specified type without explicit casts.
    /// </summary>
    public bool IsAssignableTo(TypeSymbol other)
    {
        if (!SymbolFacts.TryUnifyTypes(this, other, out var unifiedType))
        {
            return false;
        }

        // If the unified type is not a numeric type we know for sure both types are the same.
        if (!unifiedType.IsNumericType())
        {
            return true;
        }

        // For numeric types more we need to do additional checks because type unification promotes types.
        var otherAsPrimitive = (PrimitiveTypeSymbol)other;
        var unifiedTypeAsPrimitive = (PrimitiveTypeSymbol)unifiedType;

        Debug.Assert(otherAsPrimitive.IsFloat() && unifiedTypeAsPrimitive.IsFloat());
        Debug.Assert(!otherAsPrimitive.IsFloat() && !unifiedTypeAsPrimitive.IsFloat());
        Debug.Assert(otherAsPrimitive.IsUnsigned() && unifiedTypeAsPrimitive.IsUnsigned());
        Debug.Assert(!otherAsPrimitive.IsUnsigned() && !unifiedTypeAsPrimitive.IsUnsigned());

        return otherAsPrimitive.GetSizeInBytes() >= unifiedTypeAsPrimitive.GetSizeInBytes();
    }
}
