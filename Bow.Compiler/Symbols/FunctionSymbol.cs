using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public abstract class FunctionSymbol : Symbol
{
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }
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

    private ImmutableArray<ParameterSymbol>? _lazyParameters;
    public override ImmutableArray<ParameterSymbol> Parameters =>
        _lazyParameters ??= CreateParameters();

    private TypeSymbol? _lazyReturnType;
    public override TypeSymbol ReturnType => _lazyReturnType ??= BindReturnType();

    // public ImmutableArray<LocalSymbol> Locals { get; }

    private ImmutableArray<ParameterSymbol> CreateParameters()
    {
        var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(Syntax.Parameters.Count);
        foreach (var syntax in Syntax.Parameters)
        {
            switch (syntax)
            {
                case SimpleParameterDeclarationSyntax simple:
                {
                    var type = Binder.BindType(simple.Type);
                    var parameter = new SimpleParameterSymbol(this, simple, type);
                    builder.Add(parameter);
                    break;
                }

                case SelfParameterDeclarationSyntax self:
                {
                    Debug.Assert(self.Type != null);
                    var type = Binder.BindType(self.Type);
                    var parameter = new SelfParameterSymbol(this, self, type);
                    builder.Add(parameter);
                    break;
                }

                default:
                    throw new UnreachableException();
            }
        }

        return builder.MoveToImmutable();
    }

    private TypeSymbol BindReturnType()
    {
        return Syntax.ReturnType == null ? BuiltInModule.Unit : Binder.BindType(Syntax.ReturnType);
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

    private ImmutableArray<ParameterSymbol>? _lazyParameters;
    public override ImmutableArray<ParameterSymbol> Parameters =>
        _lazyParameters ??= CreateParameters();

    public override TypeSymbol ReturnType { get; } = returnType;

    // public ImmutableArray<LocalSymbol> Locals { get; }

    private ImmutableArray<ParameterSymbol> CreateParameters()
    {
        var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(Syntax.Parameters.Count);
        foreach (var syntax in Syntax.Parameters)
        {
            switch (syntax)
            {
                case SimpleParameterDeclarationSyntax simple:
                {
                    var type = Container.Binder.BindType(simple.Type);
                    var parameter = new SimpleParameterSymbol(this, simple, type);
                    builder.Add(parameter);
                    break;
                }

                case SelfParameterDeclarationSyntax self:
                {
                    var type =
                        self.Star == null ? Container : new PointerTypeSymbol(self, Container);

                    var parameter = new SelfParameterSymbol(this, self, type);
                    builder.Add(parameter);
                    break;
                }

                default:
                    throw new UnreachableException();
            }
        }

        return builder.MoveToImmutable();
    }
}

public abstract class ParameterSymbol(FunctionSymbol function, TypeSymbol type) : Symbol
{
    public abstract override ParameterDeclarationSyntax Syntax { get; }
    internal override Binder Binder => Function.Binder;

    public FunctionSymbol Function { get; } = function;
    public bool IsMutable => Syntax.MutKeyword != null;
    public TypeSymbol Type { get; } = type;
}

public sealed class SimpleParameterSymbol(
    FunctionSymbol function,
    SimpleParameterDeclarationSyntax syntax,
    TypeSymbol type
) : ParameterSymbol(function, type)
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override SimpleParameterDeclarationSyntax Syntax { get; } = syntax;
}

public sealed class SelfParameterSymbol(
    FunctionSymbol function,
    SelfParameterDeclarationSyntax syntax,
    TypeSymbol type
) : ParameterSymbol(function, type)
{
    public override string Name => "self";
    public override SelfParameterDeclarationSyntax Syntax { get; } = syntax;
}
