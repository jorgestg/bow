namespace Bow.Compiler.Syntax;

public static class SyntaxFacts
{
    public static string GetKindDisplayText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.NewLine => "new line",
            TokenKind.Identifier => "identifier",
            TokenKind.Comma => ",",
            TokenKind.Dot => ".",
            TokenKind.OpenBrace => "{",
            TokenKind.CloseBrace => "}",
            TokenKind.OpenParenthesis => "(",
            TokenKind.CloseParenthesis => ")",
            _ => throw new UnreachableException()
        };
    }
}
