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

    private static FileBinder GetParentBinder(FunctionSymbol function)
    {
        switch (function)
        {
            case FunctionItemSymbol i:
                return GetFileBinder(i);

            case MethodSymbol m:
            {
                return m.Container switch
                {
                    EnumSymbol e => GetFileBinder(e),
                    StructSymbol s => GetFileBinder(s),
                    _ => throw new UnreachableException()
                };
            }
        }

        throw new UnreachableException();
    }
}
