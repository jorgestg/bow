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
}
