using Bow.Compiler.Diagnostics;

namespace Bow.Compiler.Syntax;

public class SyntaxTree(SourceText sourceText)
{
    public SourceText SourceText { get; } = sourceText;

    private Parser? _lazyParser;
    private Parser Parser => _lazyParser ??= new(new SyntaxFactory(this));
    public DiagnosticBagView Diagnostics => Parser.Diagnostics;

    private CompilationUnitSyntax? _lazyRoot;
    public CompilationUnitSyntax Root => _lazyRoot ??= Parser.ParseCompilationUnit();
}
