using Bow.Compiler.Diagnostics;

namespace Bow.Compiler.Syntax;

public sealed class SyntaxTree
{
    private SyntaxTree(SourceText sourceText)
    {
        SourceText = sourceText;

        SyntaxFactory syntaxFactory = new(this);
        Parser = new Parser(syntaxFactory);
    }

    public SourceText SourceText { get; }

    internal Parser Parser { get; }

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (_lazyDiagnostics.IsDefault)
            {
                _lazyRoot ??= Parser.ParseCompilationUnit();
                _lazyDiagnostics = Parser.GetDiagnostics();
            }

            return _lazyDiagnostics;
        }
    }

    private CompilationUnitSyntax? _lazyRoot;
    public CompilationUnitSyntax Root => _lazyRoot ??= Parser.ParseCompilationUnit();

    public static SyntaxTree Create(string fileName, string text)
    {
        return new SyntaxTree(new SourceText(fileName, text));
    }
}
