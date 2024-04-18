namespace Bow.Compiler.Syntax;

public static class SyntaxFacts
{
    public static string GetKindDisplayText(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.NewLineToken => "new line",
            SyntaxKind.IdentifierToken => "identifier",
            SyntaxKind.BreakKeyword => "break",
            SyntaxKind.ContinueKeyword => "continue",
            SyntaxKind.ModKeyword => "mod",
            SyntaxKind.NotKeyword => "not",
            SyntaxKind.CommaToken => ",",
            SyntaxKind.DotToken => ".",
            SyntaxKind.OpenBraceToken => "{",
            SyntaxKind.CloseBraceToken => "}",
            SyntaxKind.OpenParenthesisToken => "(",
            SyntaxKind.CloseParenthesisToken => ")",
            SyntaxKind.StarToken => "*",
            SyntaxKind.SlashToken => "/",
            SyntaxKind.PercentToken => "%",
            SyntaxKind.PlusToken => "+",
            SyntaxKind.MinusToken => "-",
            SyntaxKind.GreaterThanToken => ">",
            SyntaxKind.GreaterThanEqualToken => ">=",
            SyntaxKind.LessThanToken => "<",
            SyntaxKind.LessThanEqualToken => "<=",
            SyntaxKind.EqualEqualToken => "==",
            SyntaxKind.DiamondToken => "<>",
            SyntaxKind.AmpersandToken => "&",
            SyntaxKind.PipeToken => "|",
            _ => throw new UnreachableException(kind.ToString())
        };
    }

    public static int GetUnaryOperatorPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.MinusToken => 9,
            SyntaxKind.NotKeyword => 9,
            _ => 0
        };
    }

    public static int GetBinaryOperatorPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.PercentToken => 8,
            SyntaxKind.PlusToken or SyntaxKind.MinusToken => 7,
            SyntaxKind.GreaterThanToken or SyntaxKind.GreaterThanEqualToken => 6,
            SyntaxKind.LessThanToken or SyntaxKind.LessThanEqualToken => 6,
            SyntaxKind.EqualEqualToken or SyntaxKind.DiamondToken => 5,
            SyntaxKind.AmpersandToken => 4,
            SyntaxKind.PipeToken => 3,
            SyntaxKind.AndKeyword => 2,
            SyntaxKind.OrKeyword => 1,
            _ => 0
        };
    }
}
