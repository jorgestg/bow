using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class ModuleSymbol : Symbol
{
    public ModuleSymbol(
        Compilation compilation,
        string name,
        ImmutableArray<CompilationUnitSyntax> roots,
        ModuleSymbol? container,
        IEnumerable<ModuleSymbolBuilder>? subModules
    )
    {
        Name = name;
        Compilation = compilation;
        Roots = roots;
        Container = container;
        SubModules = subModules == null ? [] : CreateSubModules(this, subModules);
    }

    public override string Name { get; }
    public override CompilationUnitSyntax Syntax => Roots.First();

    private ModuleBinder? _lazyBinder;
    internal override ModuleBinder Binder => _lazyBinder ??= new(this);

    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public Compilation Compilation { get; }
    public ImmutableArray<CompilationUnitSyntax> Roots { get; }
    public ModuleSymbol? Container { get; }
    public ImmutableArray<ModuleSymbol> SubModules { get; }

    private ImmutableArray<TypeSymbol>? _lazyTypes;
    public ImmutableArray<TypeSymbol> Types => _lazyTypes ??= CreateTypes();

    private ImmutableArray<FunctionItemSymbol>? _lazyFunctions;
    public ImmutableArray<FunctionItemSymbol> Functions => _lazyFunctions ??= CreateFunctions();

    private ModuleSymbol? _lazyRoot;
    public ModuleSymbol RootModule => _lazyRoot ??= GetRootModule();

    private static ImmutableArray<ModuleSymbol> CreateSubModules(
        ModuleSymbol self,
        IEnumerable<ModuleSymbolBuilder> subModules
    )
    {
        var builder = ImmutableArray.CreateBuilder<ModuleSymbol>(subModules.Count());
        foreach (var subModule in subModules)
        {
            var symbol = subModule.ToModuleSymbol(self);
            builder.Add(symbol);
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<TypeSymbol> CreateTypes()
    {
        var capacity = Syntax.Items.CountBy(item => item is not FunctionDefinitionSyntax);
        var builder = ImmutableArray.CreateBuilder<TypeSymbol>(capacity);
        foreach (var item in Syntax.Items)
        {
            if (item is FunctionDefinitionSyntax)
            {
                continue;
            }

            TypeSymbol type = item switch
            {
                StructDefinitionSyntax s => new StructSymbol(this, s),
                EnumDefinitionSyntax s => new EnumSymbol(this, s),
                _ => throw new UnreachableException()
            };

            builder.Add(type);
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<FunctionItemSymbol> CreateFunctions()
    {
        var capacity = Syntax.Items.CountBy(item => item is FunctionDefinitionSyntax);
        var builder = ImmutableArray.CreateBuilder<FunctionItemSymbol>(capacity);
        foreach (var item in Syntax.Items)
        {
            if (item is not FunctionDefinitionSyntax syntax)
            {
                continue;
            }

            FunctionItemSymbol function = new(this, syntax);
            builder.Add(function);
        }

        return builder.MoveToImmutable();
    }

    private ModuleSymbol GetRootModule()
    {
        if (Container == null)
        {
            return this;
        }

        ModuleSymbol root = Container;
        while (root.Container != null)
        {
            root = root.Container;
        }

        return root;
    }
}
