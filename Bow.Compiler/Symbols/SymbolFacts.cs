using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public static class SymbolFacts
{
    public const string DefaultModuleName = "main";

    public static SymbolAccessibility GetAccessibilityFromToken(Token? token, SymbolAccessibility defaultVisibility)
    {
        return token?.Kind switch
        {
            SyntaxKind.PubKeyword => SymbolAccessibility.Public,
            SyntaxKind.ModKeyword => SymbolAccessibility.Module,

            SyntaxKind.IdentifierToken when token.ContextualKeywordKind == ContextualKeywordKind.File
                => SymbolAccessibility.File,

            _ => defaultVisibility
        };
    }
}
