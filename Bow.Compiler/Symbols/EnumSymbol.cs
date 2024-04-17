using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class EnumSymbol(ModuleSymbol module, EnumDefinitionSyntax syntax) : TypeSymbol, IItemSymbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public override string Name => Syntax.Identifier.IdentifierText;
    public override EnumDefinitionSyntax Syntax { get; } = syntax;
    public override ModuleSymbol Module { get; } = module;

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    private ImmutableArray<EnumCaseSymbol> _lazyCases;
    public ImmutableArray<EnumCaseSymbol> Cases => _lazyCases.IsDefault ? _lazyCases = CreateCases() : _lazyCases;

    private ImmutableArray<MethodSymbol> _lazyMethods;
    public ImmutableArray<MethodSymbol> Methods =>
        _lazyMethods.IsDefault ? _lazyMethods = CreateMethods() : _lazyMethods;

    private FrozenDictionary<string, IMemberSymbol>? _lazyMembers;
    public FrozenDictionary<string, IMemberSymbol> MemberMap => _lazyMembers ??= CreateMemberMap();

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _lazyDiagnostics.IsDefault ? _lazyDiagnostics = _diagnosticBag.ToImmutableArray() : _lazyDiagnostics;

    ItemSyntax IItemSymbol.Syntax => Syntax;

    private ImmutableArray<EnumCaseSymbol> CreateCases()
    {
        var cases = Syntax.Members.OfType<EnumCaseDeclarationSyntax>();
        var builder = ImmutableArray.CreateBuilder<EnumCaseSymbol>(cases.Count);
        var binder = Module.GetFileBinder(Syntax.SyntaxTree);
        foreach (var caseSyntax in cases)
        {
            var argumentType =
                caseSyntax.Argument == null ? null : binder.BindType(caseSyntax.Argument.TypeReference, _diagnosticBag);

            EnumCaseSymbol @case = new(this, caseSyntax, argumentType);
            builder.Add(@case);
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
        Dictionary<string, IMemberSymbol> map = new(Cases.Length + Methods.Length);
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
                SyntaxKind.EnumCaseDeclaration => Cases.FindBySyntax(member)!,
                SyntaxKind.MethodDefinition => Methods.FindBySyntax(member)!,
                _ => throw new UnreachableException()
            };
        }
    }
}
