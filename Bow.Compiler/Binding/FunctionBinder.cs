using System.Collections.Frozen;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class FunctionItemBinder(FunctionItemSymbol function)
    : Binder(GetFileBinder(function))
{
    private readonly FunctionItemSymbol _function = function;

    private FrozenDictionary<string, ParameterSymbol>? _lazyParameters;
    private FrozenDictionary<string, ParameterSymbol> Parameters =>
        _lazyParameters ??= BindParameters();

    public override Symbol? Lookup(string name)
    {
        return Parameters.TryGetValue(name, out var symbol) ? symbol : Parent.Lookup(name);
    }

    private FrozenDictionary<string, ParameterSymbol> BindParameters()
    {
        Dictionary<string, ParameterSymbol> parameters = [];
        foreach (var parameter in _function.Parameters)
        {
            if (parameters.TryAdd(parameter.Name, parameter))
            {
                continue;
            }

            Diagnostics.AddError(
                parameter.Syntax,
                DiagnosticMessages.NameIsAlreadyDefined,
                parameter.Name
            );
        }

        return parameters.ToFrozenDictionary();
    }
}
