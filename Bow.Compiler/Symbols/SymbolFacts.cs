using System.Diagnostics.CodeAnalysis;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public static class SymbolFacts
{
    public const string DefaultModuleName = "main";

    public static SymbolAccessibility GetAccessibilityFromToken(
        Token? token,
        SymbolAccessibility defaultVisibility
    )
    {
        if (token == null)
        {
            return defaultVisibility;
        }

        if (token.Kind == SyntaxKind.PubKeyword)
        {
            return SymbolAccessibility.Public;
        }

        if (token.Kind == SyntaxKind.ModKeyword)
        {
            return SymbolAccessibility.Module;
        }

        return token.ContextualKeywordKind switch
        {
            ContextualKeywordKind.File => SymbolAccessibility.File,
            _ => defaultVisibility
        };
    }

    public static bool IsNumericType(this TypeSymbol type)
    {
        return type.PrimitiveTypeKind
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
    public static bool TryUnifyTypes(
        TypeSymbol left,
        TypeSymbol right,
        [MaybeNullWhen(false)] out TypeSymbol result
    )
    {
        result = null;
        if (left == right)
        {
            result = left;
            return true;
        }

        // Non numeric types cannot be promoted and must be equal
        if (!left.IsNumericType() || !right.IsNumericType())
        {
            return false;
        }

        var leftAsPrimitive = (PrimitiveTypeSymbol)left;
        var rightAsPrimitive = (PrimitiveTypeSymbol)right;

        bool typesAreCompatible =
            leftAsPrimitive.IsFloat() && rightAsPrimitive.IsFloat()
            || leftAsPrimitive.IsUnsigned() && rightAsPrimitive.IsUnsigned()
            || !leftAsPrimitive.IsUnsigned() && !rightAsPrimitive.IsUnsigned();

        if (typesAreCompatible)
        {
            var leftTypeSize = leftAsPrimitive.GetSizeInBytes();
            var rightTypeSize = rightAsPrimitive.GetSizeInBytes();
            result = leftTypeSize > rightTypeSize ? left : right;
            return true;
        }

        return false;
    }
}
