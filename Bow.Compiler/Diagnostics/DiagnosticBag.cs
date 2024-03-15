using System.Collections;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private Diagnostic[]? _diagnostics;
    private int _count;

    public void Add(Diagnostic diagnostic)
    {
        ResizeIfNecessary();
        _diagnostics![_count++] = diagnostic;
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

    public void AddWarning(SyntaxNode syntax, string message)
    {
        Diagnostic diagnostic =
            new(DiagnosticSeverity.Warning, syntax.SyntaxTree.SourceText, syntax.Location, message);

        Add(diagnostic);
    }

    private void ResizeIfNecessary()
    {
        if (_diagnostics == null)
        {
            _diagnostics = new Diagnostic[2];
            return;
        }

        if (_count < _diagnostics.Length)
        {
            return;
        }

        Array.Resize(ref _diagnostics, _count * 2);
    }

    public DiagnosticBagView AsView()
    {
        return new DiagnosticBagView(
            _diagnostics == null
                ? ArraySegment<Diagnostic>.Empty
                : new ArraySegment<Diagnostic>(_diagnostics, 0, _count)
        );
    }
}

public readonly struct DiagnosticBagView : IEnumerable<Diagnostic>
{
    private readonly ArraySegment<Diagnostic> _diagnostics;

    internal DiagnosticBagView(ArraySegment<Diagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public int Count => _diagnostics.Count;

    public ArraySegment<Diagnostic>.Enumerator GetEnumerator()
    {
        return _diagnostics.GetEnumerator();
    }

    IEnumerator<Diagnostic> IEnumerable<Diagnostic>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
