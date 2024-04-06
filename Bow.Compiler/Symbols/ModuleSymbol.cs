using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class ModuleSymbol(
    PackageSymbol package,
    string name,
    ImmutableArray<CompilationUnitSyntax> roots
) : Symbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public override string Name { get; } = name;
    public override CompilationUnitSyntax Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => this;

    private ModuleBinder? _lazyBinder;
    internal ModuleBinder Binder => _lazyBinder ??= new(this);

    private ImmutableArray<FileBinder> _lazyFileBinders;
    internal ImmutableArray<FileBinder> FileBinders =>
        _lazyFileBinders.IsDefault ? _lazyFileBinders = CreateFileBinders() : _lazyFileBinders;

    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public PackageSymbol Package { get; } = package;
    public ImmutableArray<CompilationUnitSyntax> Roots { get; } = roots;

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
            var fileBinder = FileBinder.CreateAndBindImports(this, root.SyntaxTree, _diagnosticBag);
            fileBinders.Add(fileBinder);
        }

        return fileBinders.MoveToImmutable();
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
}
