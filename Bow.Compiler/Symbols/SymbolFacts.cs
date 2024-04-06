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
}
