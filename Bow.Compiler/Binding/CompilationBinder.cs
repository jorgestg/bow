using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class CompilationBinder(Compilation compilation) : Binder(null!)
{
    private readonly Compilation _compilation = compilation;

    public override DiagnosticBag Diagnostics { get; } = new();

    public override Symbol? Lookup(string name)
    {
        for (var i = 0; i < _compilation.Modules.Length; i++)
        {
            var module = _compilation.Modules[i];
            if (module.Name == name)
            {
                return module;
            }
        }

        return null;
    }
}
