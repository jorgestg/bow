using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class MissingSymbol : Symbol
{
    public static readonly MissingSymbol Instance = new();

    private MissingSymbol() { }

    public override string Name => "???";
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override bool IsMissing => true;
}
