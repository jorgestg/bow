using System.Collections.Frozen;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class PlaceholderFunctionSymbol : FunctionSymbol
{
    public static readonly PlaceholderFunctionSymbol Instance = new();

    private PlaceholderFunctionSymbol() { }

    public override string Name => "???";
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override bool IsPlaceholder => true;

    public override ImmutableArray<ParameterSymbol> Parameters => [];
    public override TypeSymbol ReturnType => PlaceholderTypeSymbol.Instance;
    public override FrozenDictionary<string, ParameterSymbol> ParameterMap =>
        FrozenDictionary<string, ParameterSymbol>.Empty;
}
