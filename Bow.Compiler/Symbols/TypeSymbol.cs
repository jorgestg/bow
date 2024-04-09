using System.Diagnostics.CodeAnalysis;

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

    public bool IsNumericType()
    {
        return PrimitiveTypeKind
            is PrimitiveTypeKind.Float32
                or PrimitiveTypeKind.Float64
                or PrimitiveTypeKind.Signed8
                or PrimitiveTypeKind.Signed16
                or PrimitiveTypeKind.Signed32
                or PrimitiveTypeKind.Signed64
                or PrimitiveTypeKind.Unsigned8
                or PrimitiveTypeKind.Unsigned16
                or PrimitiveTypeKind.Unsigned32
                or PrimitiveTypeKind.Unsigned64;
    }

    /// <summary>
    /// Determines whether the current type is the same as the specified type.
    /// </summary>
    public virtual bool IsSameType(TypeSymbol other)
    {
        return ReferenceEquals(this, other);
    }

    /// <summary>
    /// Determines whether a value of the current type can be assigned to a variable of the specified type without explicit casts.
    /// </summary>
    public virtual bool IsAssignableTo(TypeSymbol other)
    {
        return IsSameType(other);
    }

    /// <summary>
    /// Tries to unify two types.
    ///
    /// <list type="bullet">
    ///     <item>Non-numeric types cannot be promoted and must be equal.</item>
    ///     <item>Integer and floats cannot be unified, both must be either integers or floats.</item>
    ///     <item>Signed types can only be unified with other signed types.</item>
    ///     <item>Unsigned types can only be unified with other unsigned types.</item>
    ///     <item>If the types are of different sizes, the largest one is returned.</item>
    /// </list>
    /// </summary>
    public virtual bool TryUnify(TypeSymbol other, [MaybeNullWhen(false)] out TypeSymbol result)
    {
        if (IsSameType(other))
        {
            result = this;
            return true;
        }

        result = null;
        return false;
    }
}
