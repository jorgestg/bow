using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public abstract class FunctionSymbol : Symbol
{
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }
    public abstract TypeSymbol ReturnType { get; }
}

public sealed class FunctionItemSymbol(ModuleSymbol module, FunctionDefinitionSyntax syntax)
    : FunctionSymbol,
        IItemSymbol
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override FunctionDefinitionSyntax Syntax { get; } = syntax;

    private FunctionItemBinder? _lazyBinder;
    internal override FunctionItemBinder Binder => _lazyBinder ??= new(this);

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    public ModuleSymbol Module { get; } = module;

    private ImmutableArray<ParameterSymbol>? _lazyParameters;
    public override ImmutableArray<ParameterSymbol> Parameters =>
        _lazyParameters ??= CreateParameters();

    private TypeSymbol? _lazyReturnType;
    public override TypeSymbol ReturnType => _lazyReturnType ??= BindReturnType();

    // public ImmutableArray<LocalSymbol> Locals { get; }

    ItemSyntax IItemSymbol.Syntax => Syntax;

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
                    Debug.Assert(self.Star == null);
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

    public override SymbolAccessibility Accessibility
    {
        get
        {
            var defaultVisibility = Container is StructSymbol { IsData: true }
                ? SymbolAccessibility.Public
                : SymbolAccessibility.Private;

            return SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, defaultVisibility);
        }
    }

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
                    Debug.Assert(self.Type == null);
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
