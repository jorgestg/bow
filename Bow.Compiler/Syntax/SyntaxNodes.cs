namespace Bow.Compiler.Syntax;

public sealed class MissingTypeReferenceSyntax(SyntaxTree syntaxTree, Token found)
    : TypeReferenceSyntax(syntaxTree)
{
    public override bool IsMissing => true;
    public Token Found { get; } = found;

    public override Location Location => Found.Location;
}

public sealed class MissingExpressionSyntax(SyntaxTree syntaxTree, Token found)
    : ExpressionSyntax(syntaxTree)
{
    public override bool IsMissing => true;
    public Token Found { get; } = found;

    public override Location Location => Found.Location;
}
