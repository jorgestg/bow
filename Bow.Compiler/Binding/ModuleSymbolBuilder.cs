using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

public sealed class ModuleSymbolBuilder(string name, CompilationUnitSyntax root)
{
    private readonly CompilationUnitSyntax _root = root;

    private ImmutableArray<CompilationUnitSyntax>.Builder? _lazyRoots;

    public string Name { get; } = name;

    public void AddRoot(CompilationUnitSyntax root)
    {
        if (root == _root)
        {
            return;
        }

        if (_lazyRoots == null)
        {
            _lazyRoots = ImmutableArray.CreateBuilder<CompilationUnitSyntax>();
            _lazyRoots.Add(_root);
        }

        _lazyRoots.Add(root);
    }

    public ModuleSymbol ToModuleSymbol(PackageSymbol package)
    {
        ImmutableArray<CompilationUnitSyntax> roots;
        if (_lazyRoots != null)
        {
            roots = _lazyRoots.DrainToImmutable();
        }
        else if (_root != null)
        {
            roots = [_root];
        }
        else
        {
            roots = [];
        }

        return new ModuleSymbol(package, Name, roots);
    }
}
