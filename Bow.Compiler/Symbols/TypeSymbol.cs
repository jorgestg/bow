using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public enum PrimitiveTypeKind
{
    None,
    Unit,
    Unsigned8,
    Unsigned16,
    Unsigned32,
    Unsigned64,
    Signed8,
    Signed16,
    Signed32,
    Signed64,
    Float32,
    Float64
}

public abstract class TypeSymbol : Symbol
{
    public virtual PrimitiveTypeKind PrimitiveTypeKind => PrimitiveTypeKind.None;
}

public sealed class MissingTypeSymbol(SyntaxNode syntax) : TypeSymbol
{
    public override string Name => "???";
    public override SyntaxNode Syntax { get; } = syntax;
    internal override Binder Binder => throw new InvalidOperationException();
    public override bool IsMissing => true;
}
