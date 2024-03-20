using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class MissingSymbol(SyntaxNode syntax) : Symbol
{
    public override string Name => "???";
    public override SyntaxNode Syntax { get; } = syntax;
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override bool IsMissing => true;
}
