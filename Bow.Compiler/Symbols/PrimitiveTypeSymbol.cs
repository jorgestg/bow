using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

internal sealed class PrimitiveTypeSymbol(string name, PrimitiveTypeKind primitiveTypeKind)
    : TypeSymbol
{
    public override string Name { get; } = name;
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    internal override Binder Binder => throw new InvalidOperationException();
    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public override PrimitiveTypeKind PrimitiveTypeKind { get; } = primitiveTypeKind;
}
