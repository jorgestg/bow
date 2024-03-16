using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public static class SymbolFacts
{
    public static SymbolAccessibility GetAccessibilityFromToken(
        Token? token,
        SymbolAccessibility defaultVisibility
    )
    {
        if (token == null)
        {
            return defaultVisibility;
        }

        if (token.Kind == TokenKind.Pub)
        {
            return SymbolAccessibility.Public;
        }

        if (token.Kind == TokenKind.Mod)
        {
            return SymbolAccessibility.Module;
        }

        return token.ContextualKeywordKind switch
        {
            ContextualKeywordKind.File => SymbolAccessibility.File,
            _ => defaultVisibility
        };
    }
}
