using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class StructSymbol(ModuleSymbol module, StructDefinitionSyntax syntax)
    : TypeSymbol,
        IItemSymbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public override string Name => Syntax.Identifier.IdentifierText;

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    public override StructDefinitionSyntax Syntax { get; } = syntax;
    public override ModuleSymbol Module { get; } = module;

    private StructBinder? _lazyBinder;
    internal override StructBinder Binder => _lazyBinder ??= new(this);

    public bool IsData => Syntax.Keyword.ContextualKeywordKind == ContextualKeywordKind.Data;

    private ImmutableArray<FieldSymbol> _lazyFields;
    public ImmutableArray<FieldSymbol> Fields =>
        _lazyFields.IsDefault ? _lazyFields = CreateFields() : _lazyFields;

    private ImmutableArray<MethodSymbol> _lazyMethods;
    public ImmutableArray<MethodSymbol> Methods =>
        _lazyMethods.IsDefault ? _lazyMethods = CreateMethods() : _lazyMethods;

    private FrozenDictionary<string, Symbol>? _lazyMembers;
    public FrozenDictionary<string, Symbol> MemberMap => _lazyMembers ??= CreateMemberMap();

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _lazyDiagnostics.IsDefault
            ? _lazyDiagnostics = _diagnosticBag.ToImmutableArray()
            : _lazyDiagnostics;

    ItemSyntax IItemSymbol.Syntax => Syntax;

    private ImmutableArray<FieldSymbol> CreateFields()
    {
        var builder = ImmutableArray.CreateBuilder<FieldSymbol>(Syntax.Fields.Count);
        foreach (var syntax in Syntax.Fields)
        {
            var type = Binder.BindType(syntax.Type, _diagnosticBag);
            FieldSymbol field = new(this, syntax, type);
            builder.Add(field);
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<MethodSymbol> CreateMethods()
    {
        var builder = ImmutableArray.CreateBuilder<MethodSymbol>(Syntax.Methods.Count);
        foreach (var syntax in Syntax.Methods)
        {
            var returnType =
                syntax.ReturnType == null
                    ? BuiltInModule.Unit
                    : Binder.BindType(syntax.ReturnType, _diagnosticBag);

            MethodSymbol method = new(this, syntax, returnType);
            builder.Add(method);
        }

        return builder.MoveToImmutable();
    }

    private IEnumerable<Symbol> GetOrderedMembers()
    {
        var slot = 0;
        foreach (var field in Fields)
        {
            if (field.Syntax.Slot == slot)
            {
                slot++;
                yield return field;
            }
        }

        foreach (var method in Methods)
        {
            if (method.Syntax.Slot == slot)
            {
                slot++;
                yield return method;
            }
        }
    }

    private FrozenDictionary<string, Symbol> CreateMemberMap()
    {
        Dictionary<string, Symbol> map = new(Fields.Length + Methods.Length);
        foreach (var member in GetOrderedMembers())
        {
            if (map.TryAdd(member.Name, member))
            {
                continue;
            }

            var identifier = member.Syntax switch
            {
                EnumCaseDeclarationSyntax s => s.Identifier,
                FunctionDefinitionSyntax s => s.Identifier,
                _ => throw new UnreachableException()
            };

            _diagnosticBag.AddError(
                identifier,
                DiagnosticMessages.NameIsAlreadyDefined,
                member.Name
            );
        }

        return map.ToFrozenDictionary();
    }
}
