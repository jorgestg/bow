using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class FunctionBinder(FunctionSymbol function) : Binder(GetParentBinder(function))
{
    private readonly FunctionSymbol _function = function;

    public override Symbol? Lookup(string name)
    {
        return _function.ParameterMap.TryGetValue(name, out var symbol)
            ? symbol
            : Parent.Lookup(name);
    }

    private static Binder GetParentBinder(FunctionSymbol function)
    {
        return function switch
        {
            FunctionItemSymbol i => GetFileBinder(i),
            MethodSymbol m => m.Container.Binder,
            _ => throw new UnreachableException()
        };
    }
}
