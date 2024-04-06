using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class PointerTypeSymbol(SyntaxNode syntax, TypeSymbol type) : TypeSymbol
{
    public override string Name { get; } = '*' + type.Name;
    public override SyntaxNode Syntax { get; } = syntax;
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public TypeSymbol Type { get; } = type;
}
