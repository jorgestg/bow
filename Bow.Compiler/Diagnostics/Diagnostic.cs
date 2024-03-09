namespace Bow.Compiler.Diagnostics;

public enum DiagnosticSeverity
{
    Warning,
    Error
}

public readonly struct Diagnostic(
    DiagnosticSeverity severity,
    SourceText? sourceText,
    Location? location,
    string message
)
{
    public DiagnosticSeverity Severity { get; } = severity;
    public SourceText? SourceText { get; } = sourceText;
    public Location? Location { get; } = location;
    public string Message { get; } = message;
}
