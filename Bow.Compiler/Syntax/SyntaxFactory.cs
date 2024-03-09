namespace Bow.Compiler.Syntax;

public partial class SyntaxFactory(SyntaxTree syntaxTree)
{
    private readonly SyntaxTree _syntaxTree = syntaxTree;

    public Token Token(TokenKind kind, int start, int length)
    {
        return new Token(_syntaxTree, kind, new Location(start, length));
    }

    public IdentifierToken Identifier(int start, int length)
    {
        return new IdentifierToken(_syntaxTree, new Location(start, length));
    }

    public SyntaxList<TSyntax> SyntaxList<TSyntax>(params TSyntax[] nodes)
        where TSyntax : SyntaxNode
    {
        return new SyntaxList<TSyntax>(_syntaxTree, nodes);
    }
}
