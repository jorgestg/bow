namespace Bow.Compiler.Syntax;

public class SyntaxTree
{
    internal SyntaxTree(
        SourceText sourceText,
        Func<SyntaxFactory, CompilationUnitSyntax> rootFactory
    )
    {
        SourceText = sourceText;
        Root = rootFactory(new SyntaxFactory(this));
    }

    public SourceText SourceText { get; }

    public CompilationUnitSyntax Root { get; }
}
