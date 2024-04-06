using Bow.Compiler.Syntax;

namespace Bow.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private ImmutableArray<Diagnostic>.Builder? _diagnostics;

    public void Add(Diagnostic diagnostic)
    {
        _diagnostics ??= ImmutableArray.CreateBuilder<Diagnostic>();
        _diagnostics.Add(diagnostic);
    }

    public void AddError(SyntaxNode syntax, string message)
    {
        Diagnostic diagnostic =
            new(DiagnosticSeverity.Error, syntax.SyntaxTree.SourceText, syntax.Location, message);

        Add(diagnostic);
    }

    public void AddError(SyntaxNode syntax, string format, string arg)
    {
        var message = string.Format(format, arg);
        Diagnostic diagnostic =
            new(DiagnosticSeverity.Error, syntax.SyntaxTree.SourceText, syntax.Location, message);

        Add(diagnostic);
    }

    public void AddError(SyntaxNode syntax, string format, string arg0, string arg1)
    {
        var message = string.Format(format, arg0, arg1);
        Diagnostic diagnostic =
            new(DiagnosticSeverity.Error, syntax.SyntaxTree.SourceText, syntax.Location, message);

        Add(diagnostic);
    }

    public void AddError(SyntaxNode syntax, string format, string arg0, string arg1, string arg2)
    {
        var message = string.Format(format, arg0, arg1, arg2);
        Diagnostic diagnostic =
            new(DiagnosticSeverity.Error, syntax.SyntaxTree.SourceText, syntax.Location, message);

        Add(diagnostic);
    }

    public void AddWarning(SyntaxNode syntax, string message)
    {
        Diagnostic diagnostic =
            new(DiagnosticSeverity.Warning, syntax.SyntaxTree.SourceText, syntax.Location, message);

        Add(diagnostic);
    }

    public ImmutableArray<Diagnostic> ToImmutableArray()
    {
        return _diagnostics?.DrainToImmutable() ?? [];
    }
}
