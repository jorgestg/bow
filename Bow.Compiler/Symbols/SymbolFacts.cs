using System.Text.RegularExpressions;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public static class SymbolFacts
{
    public static SymbolAccessibility GetAccessibilityFromToken(
        Token? token,
        SymbolAccessibility mostPrivate = SymbolAccessibility.Private
    )
    {
        if (token == null)
        {
            return mostPrivate;
        }

        if (token.Kind == TokenKind.Pub)
        {
            return SymbolAccessibility.Public;
        }

        return token.ContextualKeywordKind switch
        {
            ContextualKeywordKind.Mod => SymbolAccessibility.Module,
            ContextualKeywordKind.File => SymbolAccessibility.File,
            _ => mostPrivate
        };
    }
}
