using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class PrimitiveTypeSymbol(
    BuiltInModule module,
    string name,
    PrimitiveTypeKind primitiveTypeKind
) : TypeSymbol
{
    public override string Name { get; } = name;
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    internal override Binder Binder => throw new InvalidOperationException();
    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public override PrimitiveTypeKind PrimitiveTypeKind { get; } = primitiveTypeKind;

    public BuiltInModule Module { get; } = module;
}
