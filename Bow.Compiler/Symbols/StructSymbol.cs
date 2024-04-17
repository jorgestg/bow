using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class StructSymbol(ModuleSymbol module, StructDefinitionSyntax syntax) : TypeSymbol, IItemSymbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public override string Name => Syntax.Identifier.IdentifierText;

    public override StructDefinitionSyntax Syntax { get; } = syntax;
    public override ModuleSymbol Module { get; } = module;

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    public bool IsData => Syntax.Keyword.ContextualKeywordKind == ContextualKeywordKind.Data;

    private ImmutableArray<FieldSymbol> _lazyFields;
    public ImmutableArray<FieldSymbol> Fields => _lazyFields.IsDefault ? _lazyFields = CreateFields() : _lazyFields;

    private ImmutableArray<MethodSymbol> _lazyMethods;
    public ImmutableArray<MethodSymbol> Methods =>
        _lazyMethods.IsDefault ? _lazyMethods = CreateMethods() : _lazyMethods;

    private FrozenDictionary<string, IMemberSymbol>? _lazyMembers;
    public FrozenDictionary<string, IMemberSymbol> MemberMap => _lazyMembers ??= CreateMemberMap();

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _lazyDiagnostics.IsDefault ? _lazyDiagnostics = _diagnosticBag.ToImmutableArray() : _lazyDiagnostics;

    ItemSyntax IItemSymbol.Syntax => Syntax;

    private ImmutableArray<FieldSymbol> CreateFields()
    {
        var fields = Syntax.Members.OfType<FieldDeclarationSyntax>();
        var builder = ImmutableArray.CreateBuilder<FieldSymbol>(fields.Count);
        var binder = Module.GetFileBinder(Syntax.SyntaxTree);
        foreach (var fieldSyntax in fields)
        {
            var type = binder.BindType(fieldSyntax.Type, _diagnosticBag);
            FieldSymbol field = new(this, fieldSyntax, type);
            builder.Add(field);
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<MethodSymbol> CreateMethods()
    {
        var methods = Syntax.Members.OfType<MethodDefinitionSyntax>();
        var builder = ImmutableArray.CreateBuilder<MethodSymbol>(methods.Count);
        var binder = Module.GetFileBinder(Syntax.SyntaxTree);
        foreach (var methodSyntax in methods)
        {
            var returnType =
                methodSyntax.ReturnType == null
                    ? BuiltInPackage.UnitType
                    : binder.BindType(methodSyntax.ReturnType, _diagnosticBag);

            MethodSymbol method = new(this, methodSyntax, returnType);
            builder.Add(method);
        }

        return builder.MoveToImmutable();
    }

    private FrozenDictionary<string, IMemberSymbol> CreateMemberMap()
    {
        Dictionary<string, IMemberSymbol> map = new(Fields.Length + Methods.Length);
        foreach (var member in GetOrderedMembers())
        {
            if (map.TryAdd(member.Name, member))
            {
                continue;
            }

            var identifier = member.Syntax.Identifier;
            _diagnosticBag.AddError(identifier, DiagnosticMessages.NameIsAlreadyDefined, member.Name);
        }

        return map.ToFrozenDictionary();
    }

    private IEnumerable<IMemberSymbol> GetOrderedMembers()
    {
        foreach (var member in Syntax.Members)
        {
            yield return member.Kind switch
            {
                SyntaxKind.FieldDeclaration => Fields.FindBySyntax(member)!,
                SyntaxKind.MethodDefinition => Methods.FindBySyntax(member)!,
                _ => throw new UnreachableException()
            };
        }
    }
}
