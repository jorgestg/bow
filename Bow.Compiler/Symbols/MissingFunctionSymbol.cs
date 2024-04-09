using System.Collections.Frozen;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class MissingFunctionSymbol : FunctionSymbol
{
    public static readonly MissingFunctionSymbol Instance = new();

    private MissingFunctionSymbol() { }

    public override string Name => "???";
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override bool IsMissing => true;

    public override ImmutableArray<ParameterSymbol> Parameters => [];
    public override TypeSymbol ReturnType => PlaceholderTypeSymbol.UnknownType;
    public override FrozenDictionary<string, ParameterSymbol> ParameterMap =>
        FrozenDictionary<string, ParameterSymbol>.Empty;
}
