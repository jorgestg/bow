namespace Bow.Compiler.Syntax;

public partial class SyntaxFactory(SyntaxTree syntaxTree)
{
    public SyntaxTree SyntaxTree { get; } = syntaxTree;

    public MissingTypeReferenceSyntax MissingTypeReference(Token found)
    {
        return new MissingTypeReferenceSyntax(SyntaxTree, found);
    }

    public MissingExpressionSyntax MissingExpression(Token found)
    {
        return new MissingExpressionSyntax(SyntaxTree, found);
    }

    public Token Token(TokenKind kind, int start, int length)
    {
        return new Token(SyntaxTree, kind, new Location(start, length), isMissing: false);
    }

    public Token MissingToken(TokenKind kind, int start, int length)
    {
        return new Token(SyntaxTree, kind, new Location(start, length), isMissing: true);
    }

    public IdentifierToken Identifier(int start, int length)
    {
        return new IdentifierToken(SyntaxTree, new Location(start, length), isMissing: false);
    }

    public IdentifierToken MissingIdentifier(int start, int length)
    {
        return new IdentifierToken(SyntaxTree, new Location(start, length), isMissing: true);
    }

    public SyntaxList<TSyntax> SyntaxList<TSyntax>(params TSyntax[] nodes)
        where TSyntax : SyntaxNode
    {
        return new SyntaxList<TSyntax>(SyntaxTree, nodes);
    }

    public SyntaxListBuilder<TSyntax> SyntaxListBuilder<TSyntax>()
        where TSyntax : SyntaxNode
    {
        return new SyntaxListBuilder<TSyntax>(SyntaxTree);
    }
}
