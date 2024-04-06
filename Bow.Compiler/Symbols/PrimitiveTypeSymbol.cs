using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

internal sealed class PrimitiveTypeSymbol(string name, PrimitiveTypeKind primitiveTypeKind)
    : TypeSymbol
{
    public override string Name { get; } = name;
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public override PrimitiveTypeKind PrimitiveTypeKind { get; } = primitiveTypeKind;

    public bool IsFloat()
    {
        return PrimitiveTypeKind is PrimitiveTypeKind.Float32 or PrimitiveTypeKind.Float64;
    }

    public bool IsUnsigned()
    {
        return PrimitiveTypeKind
            is PrimitiveTypeKind.Unsigned8
                or PrimitiveTypeKind.Unsigned16
                or PrimitiveTypeKind.Unsigned32
                or PrimitiveTypeKind.Unsigned64;
    }

    public int GetSizeInBytes()
    {
        return PrimitiveTypeKind switch
        {
            PrimitiveTypeKind.Signed8 or PrimitiveTypeKind.Unsigned8 => 1,
            PrimitiveTypeKind.Signed16 or PrimitiveTypeKind.Unsigned16 => 2,
            PrimitiveTypeKind.Float32 => 4,
            PrimitiveTypeKind.Signed32 or PrimitiveTypeKind.Unsigned32 => 4,
            PrimitiveTypeKind.Float64 => 8,
            PrimitiveTypeKind.Signed64 or PrimitiveTypeKind.Unsigned64 => 8,
            _ => throw new UnreachableException()
        };
    }
}
