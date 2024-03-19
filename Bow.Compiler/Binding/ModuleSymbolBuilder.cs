using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

public sealed class ModuleSymbolBuilder(string name)
{
    private CompilationUnitSyntax? _root;

    private ImmutableArray<CompilationUnitSyntax>.Builder? _lazyRoots;

    public string Name { get; } = name;

    private List<ModuleSymbolBuilder>? _lazySubModuleBuilders;
    public List<ModuleSymbolBuilder> SubModuleBuilders => _lazySubModuleBuilders ??= [];

    public void AddRoot(CompilationUnitSyntax root)
    {
        if (root == _root)
        {
            return;
        }

        if (_root == null)
        {
            _root = root;
            return;
        }

        if (_lazyRoots == null)
        {
            _lazyRoots = ImmutableArray.CreateBuilder<CompilationUnitSyntax>();
            _lazyRoots.Add(_root);
        }

        _lazyRoots.Add(root);
    }

    public ModuleSymbol ToModuleSymbol(Compilation compilation)
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

        return new ModuleSymbol(compilation, Name, roots, null, _lazySubModuleBuilders);
    }

    public ModuleSymbol ToModuleSymbol(ModuleSymbol container)
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

        return new ModuleSymbol(
            container.Compilation,
            Name,
            roots,
            container,
            _lazySubModuleBuilders
        );
    }
}
