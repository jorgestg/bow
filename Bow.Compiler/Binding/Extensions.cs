using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal static class Extensions
{
    public static TSymbol? FindByName<TSymbol>(this ImmutableArray<TSymbol> symbols, string name)
        where TSymbol : Symbol
    {
        for (int i = 0; i < symbols.Length; i++)
        {
            TSymbol? symbol = symbols[i];
            if (symbol.Name == name)
            {
                return symbol;
            }
        }

        return null;
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
}
