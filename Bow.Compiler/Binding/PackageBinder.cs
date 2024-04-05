using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class PackageBinder(PackageSymbol package) : Binder(null!)
{
    private readonly PackageSymbol _package = package;

    public override Symbol? Lookup(string name)
    {
        if (name == _package.Name)
        {
            return _package;
        }

        return (Symbol?)_package.Modules.FindByName(name) ?? _package.References.FindByName(name);
    }
}
