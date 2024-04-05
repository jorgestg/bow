using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class PackageSymbol : Symbol
{
    public PackageSymbol(string name, ImmutableArray<SyntaxTree> syntaxTrees)
    {
        Name = name;
        SyntaxTrees = syntaxTrees;
        Binder = new PackageBinder(this);
        Modules = CreateModules();
    }

    public override string Name { get; }
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();

    public ImmutableArray<PackageSymbol> References { get; }
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public ImmutableArray<ModuleSymbol> Modules { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    internal PackageBinder Binder { get; }

    public FunctionSymbol? GetEntryPoint()
    {
        var mainModule = Modules.FindByName("main");
        var mainFunction = mainModule?.Functions.FindByName("main");
        if (mainFunction != null)
        {
            return mainFunction;
        }

        foreach (var module in Modules)
        {
            mainFunction = module.Functions.FindByName("main");
            if (mainFunction != null)
            {
                return mainFunction;
            }
        }

        return null;
    }

    private ImmutableArray<ModuleSymbol> CreateModules()
    {
        List<ModuleSymbolBuilder> builders = [];
        foreach (var syntaxTree in SyntaxTrees)
        {
            var root = syntaxTree.Root;
            var name = root.ModClause?.Name.IdentifierText ?? SymbolFacts.DefaultModuleName;
            ModuleSymbolBuilder builder = FindOrCreateModuleBuilder(builders, name, root);
            builder.AddRoot(root);
        }

        var modules = ImmutableArray.CreateBuilder<ModuleSymbol>(builders.Count);
        foreach (var moduleBuilder in builders)
        {
            var module = moduleBuilder.ToModuleSymbol(this);
            modules.Add(module);
        }

        return modules.MoveToImmutable();

        static ModuleSymbolBuilder FindOrCreateModuleBuilder(
            List<ModuleSymbolBuilder> builders,
            string name,
            CompilationUnitSyntax root
        )
        {
            foreach (var builder in builders)
            {
                if (builder.Name == name)
                {
                    return builder;
                }
            }

            ModuleSymbolBuilder newBuilder = new(name, root);
            builders.Add(newBuilder);
            return newBuilder;
        }
    }
}
