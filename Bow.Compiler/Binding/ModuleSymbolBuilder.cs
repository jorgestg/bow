using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

public sealed class ModuleSymbolBuilder(string name, CompilationUnitSyntax root)
{
    private readonly CompilationUnitSyntax _root = root;

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

        if (_lazyRoots == null)
        {
            _lazyRoots = ImmutableArray.CreateBuilder<CompilationUnitSyntax>();
            _lazyRoots.Add(_root);
        }

        _lazyRoots.Add(root);
    }

    public ModuleSymbol ToModuleSymbol(Compilation compilation)
    {
        var roots = _lazyRoots?.DrainToImmutable() ?? [_root];
        return new ModuleSymbol(compilation, Name, roots, null, _lazySubModuleBuilders);
    }

    public ModuleSymbol ToModuleSymbol(ModuleSymbol container)
    {
        var roots = _lazyRoots?.DrainToImmutable() ?? [_root];
        return new ModuleSymbol(
            container.Compilation,
            Name,
            roots,
            container,
            _lazySubModuleBuilders
        );
    }
}
