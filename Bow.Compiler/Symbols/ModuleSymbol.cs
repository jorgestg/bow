using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class ModuleSymbol(PackageSymbol package, string name, ImmutableArray<CompilationUnitSyntax> roots)
    : Symbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public override string Name { get; } = name;
    public override SyntaxNode Syntax => throw new InvalidOperationException();
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
    public ImmutableArray<TypeSymbol> Types => _lazyTypes.IsDefault ? _lazyTypes = CreateTypes() : _lazyTypes;

    private ImmutableArray<FunctionItemSymbol> _lazyFunctions;
    public ImmutableArray<FunctionItemSymbol> Functions =>
        _lazyFunctions.IsDefault ? _lazyFunctions = CreateFunctions() : _lazyFunctions;

    private FrozenDictionary<string, IItemSymbol>? _lazyMembers;
    public FrozenDictionary<string, IItemSymbol> MembersMap => _lazyMembers ??= CreateMembersMap();

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _lazyDiagnostics.IsDefault ? _lazyDiagnostics = _diagnosticBag.ToImmutableArray() : _lazyDiagnostics;

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
                if (item.Kind == SyntaxKind.FunctionDefinition)
                {
                    continue;
                }

                TypeSymbol type = item.Kind switch
                {
                    SyntaxKind.StructDefinition => new StructSymbol(this, (StructDefinitionSyntax)item),
                    SyntaxKind.EnumDefinition => new EnumSymbol(this, (EnumDefinitionSyntax)item),
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
            var functions = root.Items.OfType<FunctionDefinitionSyntax>();
            foreach (var functionSyntax in functions)
            {
                FunctionItemSymbol function = new(this, functionSyntax);
                builder.Add(function);
            }
        }

        return builder.DrainToImmutable();
    }

    private FrozenDictionary<string, IItemSymbol> CreateMembersMap()
    {
        Dictionary<string, IItemSymbol> members = [];
        foreach (var item in GetOrderedItems())
        {
            if (members.TryAdd(item.Name, item))
            {
                continue;
            }

            var identifier = item.Syntax.Identifier;
            _diagnosticBag.AddError(identifier, DiagnosticMessages.NameIsAlreadyDefined, item.Name);
        }

        return members.ToFrozenDictionary();
    }

    private IEnumerable<IItemSymbol> GetOrderedItems()
    {
        foreach (var root in Roots)
        {
            foreach (var itemSyntax in root.Items)
            {
                yield return itemSyntax.Kind switch
                {
                    SyntaxKind.EnumDefinition
                    or SyntaxKind.StructDefinition
                        => (IItemSymbol)Types.FindBySyntax(itemSyntax)!,

                    SyntaxKind.FunctionDefinition => Functions.FindBySyntax(itemSyntax)!,
                    _ => throw new UnreachableException()
                };
            }
        }
    }
}
