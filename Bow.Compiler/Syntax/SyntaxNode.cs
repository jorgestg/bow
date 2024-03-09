namespace Bow.Compiler.Syntax;

public abstract class SyntaxNode(SyntaxTree syntaxTree)
{
    public SyntaxTree SyntaxTree { get; } = syntaxTree;
    public abstract Location Location { get; }

    public ReadOnlyMemory<char> GetText()
    {
        return SyntaxTree.SourceText.GetTextRange(Location);
    }

    public override string ToString()
    {
        return GetText().ToString();
    }
}
