using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class MissingTypeSymbol : TypeSymbol
{
    public static readonly MissingTypeSymbol Instance = new();

    private MissingTypeSymbol() { }

    public override string Name => "???";
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override bool IsMissing => true;
}
