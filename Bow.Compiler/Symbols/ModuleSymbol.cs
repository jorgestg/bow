using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class ModuleSymbol : Symbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public ModuleSymbol(
        Compilation compilation,
        string name,
        ImmutableArray<CompilationUnitSyntax> roots,
        ModuleSymbol? previous,
        IEnumerable<ModuleSymbolBuilder>? subModules
    )
    {
        Name = name;
        Compilation = compilation;
        Roots = roots;
        Previous = previous;
        SubModules = subModules == null ? [] : CreateSubModules(this, subModules);
    }

    public override string Name { get; }
    public override CompilationUnitSyntax Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => this;

    private ModuleBinder? _lazyBinder;
    internal override ModuleBinder Binder => _lazyBinder ??= new(this);

    private ImmutableArray<FileBinder> _lazyFileBinders;
    internal ImmutableArray<FileBinder> FileBinders =>
        _lazyFileBinders.IsDefault ? _lazyFileBinders = CreateFileBinders() : _lazyFileBinders;

    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public Compilation Compilation { get; }
    public ImmutableArray<CompilationUnitSyntax> Roots { get; }

    private ModuleSymbol? _lazyRoot;
    public ModuleSymbol RootModule => _lazyRoot ??= GetRootModule();

    public ModuleSymbol? Previous { get; }
    public ImmutableArray<ModuleSymbol> SubModules { get; }

    private ImmutableArray<TypeSymbol> _lazyTypes;
    public ImmutableArray<TypeSymbol> Types =>
        _lazyTypes.IsDefault ? _lazyTypes = CreateTypes() : _lazyTypes;

    private ImmutableArray<FunctionItemSymbol> _lazyFunctions;
    public ImmutableArray<FunctionItemSymbol> Functions =>
        _lazyFunctions.IsDefault ? _lazyFunctions = CreateFunctions() : _lazyFunctions;

    private FrozenDictionary<string, Symbol>? _lazyMembers;
    public FrozenDictionary<string, Symbol> MembersMap => _lazyMembers ??= CreateMembersMap();

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _lazyDiagnostics.IsDefault
            ? _lazyDiagnostics = _diagnosticBag.ToImmutableArray()
            : _lazyDiagnostics;

    internal FileBinder GetFileBinder(SyntaxTree syntaxTree)
    {
        foreach (var fileBinder in FileBinders)
        {
            if (fileBinder.SyntaxTree == syntaxTree)
            {
                return fileBinder;
            }
        }

        throw new UnreachableException();
    }

    private ImmutableArray<FileBinder> CreateFileBinders()
    {
        var fileBinders = ImmutableArray.CreateBuilder<FileBinder>(Roots.Length);
        foreach (var root in Roots)
        {
            fileBinders.Add(new FileBinder(this, root.SyntaxTree, _diagnosticBag));
        }

        return fileBinders.MoveToImmutable();
    }

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
        var builder = ImmutableArray.CreateBuilder<TypeSymbol>();
        foreach (var root in Roots)
        {
            foreach (var item in root.Items)
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
        }

        return builder.DrainToImmutable();
    }

    private ImmutableArray<FunctionItemSymbol> CreateFunctions()
    {
        var builder = ImmutableArray.CreateBuilder<FunctionItemSymbol>();
        foreach (var root in Roots)
        {
            foreach (var item in root.Items)
            {
                if (item is not FunctionDefinitionSyntax syntax)
                {
                    continue;
                }

                FunctionItemSymbol function = new(this, syntax);
                builder.Add(function);
            }
        }

        return builder.DrainToImmutable();
    }

    private FrozenDictionary<string, Symbol> CreateMembersMap()
    {
        Dictionary<string, Symbol> members = [];
        foreach (var type in Types)
        {
            if (members.TryAdd(type.Name, type))
            {
                continue;
            }

            var identifier = ((ItemSyntax)type.Syntax).Identifier;
            _diagnosticBag.AddError(identifier, DiagnosticMessages.NameIsAlreadyDefined, type.Name);
        }

        foreach (var function in Functions)
        {
            if (members.TryAdd(function.Name, function))
            {
                continue;
            }

            _diagnosticBag.AddError(
                function.Syntax.Identifier,
                DiagnosticMessages.NameIsAlreadyDefined,
                function.Name
            );
        }

        return members.ToFrozenDictionary();
    }

    private ModuleSymbol GetRootModule()
    {
        if (Previous == null)
        {
            return this;
        }

        ModuleSymbol root = Previous;
        while (root.Previous != null)
        {
            root = root.Previous;
        }

        return root;
    }
}
