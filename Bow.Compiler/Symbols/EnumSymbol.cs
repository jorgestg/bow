using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class EnumSymbol(ModuleSymbol module, EnumDefinitionSyntax syntax) : TypeSymbol
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override EnumDefinitionSyntax Syntax { get; } = syntax;

    private EnumBinder? _lazyBinder;
    internal override EnumBinder Binder => _lazyBinder ??= new(this);

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    public ModuleSymbol Module { get; } = module;

    private ImmutableArray<EnumCaseSymbol>? _lazyCases;
    public ImmutableArray<EnumCaseSymbol> Cases => _lazyCases ??= CreateCases();

    private ImmutableArray<MethodSymbol>? _lazyMethods;
    public ImmutableArray<MethodSymbol> Methods => _lazyMethods ??= CreateMethods();

    private ImmutableArray<EnumCaseSymbol> CreateCases()
    {
        var builder = ImmutableArray.CreateBuilder<EnumCaseSymbol>(Syntax.Cases.Count);
        foreach (var syntax in Syntax.Cases)
        {
            var associatedValueType =
                syntax.AssociatedValue == null
                    ? null
                    : Binder.BindType(syntax.AssociatedValue.TypeReference);

            EnumCaseSymbol @case = new(this, syntax, associatedValueType);
            builder.Add(@case);
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<MethodSymbol> CreateMethods()
    {
        var builder = ImmutableArray.CreateBuilder<MethodSymbol>(Syntax.Methods.Count);
        foreach (var syntax in Syntax.Methods)
        {
            var returnType = Binder.BindType(syntax.ReturnType);
            MethodSymbol method = new(this, syntax, returnType);
            builder.Add(method);
        }

        return builder.MoveToImmutable();
    }
}

public sealed class EnumCaseSymbol(
    EnumSymbol @enum,
    EnumCaseDeclarationSyntax syntax,
    TypeSymbol? associatedValueType
) : Symbol
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override EnumCaseDeclarationSyntax Syntax { get; } = syntax;
    internal override Binder Binder => Enum.Binder;
    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public EnumSymbol Enum { get; } = @enum;
    public TypeSymbol? AssociatedValueType { get; } = associatedValueType;
}
