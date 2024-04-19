using System.Collections.Frozen;

namespace Bow.Compiler.Symbols;

public abstract class FunctionSymbol : Symbol
{
    public bool IsStatic => Parameters.Length == 0 || Parameters[0] is not SelfParameterSymbol;
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }
    public abstract TypeSymbol ReturnType { get; }
    public abstract FrozenDictionary<string, ParameterSymbol> ParameterMap { get; }

    private TypeSymbol? _lazyType;
    public TypeSymbol Type => _lazyType ??= new FunctionTypeSymbol(this);
}
