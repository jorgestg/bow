using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler;

public sealed class Compilation
{
    public Compilation(ImmutableArray<SyntaxTree> syntaxTrees)
    {
        SyntaxTrees = syntaxTrees;
        Binder = new CompilationBinder(this);
        Modules = CreateModules();
    }

    internal CompilationBinder Binder { get; }
    public DiagnosticBagView Diagnostics => Binder.Diagnostics.AsView();

    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public ImmutableArray<ModuleSymbol> Modules { get; }

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
            mainFunction = FindEntryPointInModule(module);
            if (mainFunction != null)
            {
                return mainFunction;
            }
        }

        return null;

        static FunctionItemSymbol? FindEntryPointInModule(ModuleSymbol module)
        {
            var mainFunction = module.Functions.FindByName("main");
            if (mainFunction != null)
            {
                return mainFunction;
            }

            foreach (var subModule in module.SubModules)
            {
                mainFunction = FindEntryPointInModule(subModule);
                if (mainFunction != null)
                {
                    return mainFunction;
                }
            }

            return null;
        }
    }

    private ImmutableArray<ModuleSymbol> CreateModules()
    {
        List<ModuleSymbolBuilder> builders = [];
        foreach (var syntaxTree in SyntaxTrees)
        {
            var root = syntaxTree.Root;
            switch (root.ModClause?.Name)
            {
                case SimpleNameSyntax simpleName:
                {
                    var name = simpleName.Identifier.IdentifierText;
                    var builder = FindOrCreateBuilder(builders, name, root);
                    builder.AddRoot(root);
                    continue;
                }
                case QualifiedNameSyntax qualifiedName:
                {
                    var name = qualifiedName.Parts[0].IdentifierText;
                    var builder = FindOrCreateBuilder(builders, name, root);
                    for (var i = 1; i < qualifiedName.Parts.Count; i++)
                    {
                        name = qualifiedName.Parts[i].IdentifierText;
                        builder = FindOrCreateBuilder(builder.SubModuleBuilders, name, root);
                    }

                    builder.AddRoot(root);
                    continue;
                }
                case null:
                {
                    var fileName = syntaxTree.SourceText.FileName;
                    var directoryName = Path.GetDirectoryName(fileName);
                    var moduleName = string.IsNullOrEmpty(directoryName) ? "main" : directoryName;
                    var builder = FindOrCreateBuilder(builders, moduleName, root);
                    builder.AddRoot(root);
                    continue;
                }
            }

            throw new UnreachableException();
        }

        var modules = ImmutableArray.CreateBuilder<ModuleSymbol>(builders.Count);
        foreach (var moduleBuilder in builders)
        {
            var module = moduleBuilder.ToModuleSymbol(this);
            modules.Add(module);
        }

        return modules.MoveToImmutable();

        static ModuleSymbolBuilder FindOrCreateBuilder(
            List<ModuleSymbolBuilder> builders,
            string name,
            CompilationUnitSyntax root
        )
        {
            foreach (var builder in builders)
            {
                if (builder.Name != name)
                {
                    continue;
                }

                return builder;
            }

            ModuleSymbolBuilder newBuilder = new(name, root);
            builders.Add(newBuilder);
            return newBuilder;
        }
    }
}
