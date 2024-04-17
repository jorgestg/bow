using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

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

    public static T? ElementAtOrDefault<T>(this ImmutableArray<T> array, int index)
    {
        if (index < 0 || index >= array.Length)
        {
            return default;
        }

        return array[index];
    }

    public static TSymbol? FindBySyntax<TSymbol>(this ImmutableArray<TSymbol> symbols, SyntaxNode syntax)
        where TSymbol : Symbol
    {
        for (int i = 0; i < symbols.Length; i++)
        {
            TSymbol? symbol = symbols[i];
            if (symbol.Syntax == syntax)
            {
                return symbol;
            }
        }

        return null;
    }
}
