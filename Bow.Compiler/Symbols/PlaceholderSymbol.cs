using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

/// <summary>
/// Represents a symbol that was not found.
/// </summary>
public sealed class PlaceholderSymbol : Symbol
{
    public static readonly PlaceholderSymbol Instance = new();

    private PlaceholderSymbol() { }

    public override string Name => "???";
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override bool IsPlaceholder => true;
}
