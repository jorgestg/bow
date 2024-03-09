using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public abstract class FunctionSymbol : Symbol
{
    public abstract ImmutableArray<LocalSymbol> Parameters { get; }
    public abstract TypeSymbol ReturnType { get; }
}

public sealed class FunctionItemSymbol(ModuleSymbol module, FunctionDefinitionSyntax syntax)
    : FunctionSymbol
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override FunctionDefinitionSyntax Syntax { get; } = syntax;
    internal override Binder Binder => throw new NotImplementedException();

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    public ModuleSymbol Module { get; } = module;

    private ImmutableArray<LocalSymbol>? _lazyParameters;
    public override ImmutableArray<LocalSymbol> Parameters =>
        _lazyParameters ??= CreateParameters();

    private TypeSymbol? _lazyReturnType;
    public override TypeSymbol ReturnType =>
        _lazyReturnType ??= Module.Binder.BindType(Syntax.ReturnType);

    // public ImmutableArray<LocalSymbol> Locals { get; }

    private ImmutableArray<LocalSymbol> CreateParameters()
    {
        var builder = ImmutableArray.CreateBuilder<LocalSymbol>(Syntax.Parameters.Count);
        foreach (var syntax in Syntax.Parameters)
        {
            Debug.Assert(syntax.Type != null);
            var type = Module.Binder.BindType(syntax.Type);
            LocalSymbol field = new(this, syntax, type);
            builder.Add(field);
        }

        return builder.MoveToImmutable();
    }
}

public sealed class MethodSymbol(
    TypeSymbol container,
    FunctionDefinitionSyntax syntax,
    TypeSymbol returnType
) : FunctionSymbol
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override FunctionDefinitionSyntax Syntax { get; } = syntax;
    internal override Binder Binder => throw new NotImplementedException();

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier);

    public TypeSymbol Container { get; } = container;

    private ImmutableArray<LocalSymbol>? _lazyParameters;
    public override ImmutableArray<LocalSymbol> Parameters =>
        _lazyParameters ??= CreateParameters();

    public override TypeSymbol ReturnType { get; } = returnType;

    // public ImmutableArray<LocalSymbol> Locals { get; }

    private ImmutableArray<LocalSymbol> CreateParameters()
    {
        var builder = ImmutableArray.CreateBuilder<LocalSymbol>(Syntax.Parameters.Count);
        foreach (var syntax in Syntax.Parameters)
        {
            Debug.Assert(
                syntax.Type != null
                    || syntax.Identifier.ContextualKeywordKind == ContextualKeywordKind.Self
            );

            var type = syntax.Type == null ? Container : Container.Binder.BindType(syntax.Type);
            LocalSymbol parameter = new(this, syntax, type);
            builder.Add(parameter);
        }

        return builder.MoveToImmutable();
    }
}

public sealed class LocalSymbol(
    FunctionSymbol function,
    ParameterDeclarationSyntax syntax,
    TypeSymbol type
) : Symbol
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override ParameterDeclarationSyntax Syntax { get; } = syntax;
    internal override Binder Binder => Function.Binder;

    public FunctionSymbol Function { get; } = function;
    public bool IsMutable => Syntax.MutKeyword != null;
    public bool IsByReference => Syntax.Ampersand != null;
    public bool IsSelf => Syntax.Identifier.ContextualKeywordKind == ContextualKeywordKind.Self;
    public TypeSymbol Type { get; } = type;
}
