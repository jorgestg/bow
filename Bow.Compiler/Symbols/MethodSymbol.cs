using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class MethodSymbol(TypeSymbol container, MethodDefinitionSyntax syntax, TypeSymbol returnType)
    : FunctionSymbol,
        IMemberSymbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public override string Name => Syntax.Identifier.IdentifierText;
    public override MethodDefinitionSyntax Syntax { get; } = syntax;
    public override ModuleSymbol Module => Container.Module;

    private FunctionBinder? _lazyBinder;
    internal FunctionBinder Binder => _lazyBinder ??= new(this);

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

    private ImmutableArray<ParameterSymbol> _lazyParameters;
    public override ImmutableArray<ParameterSymbol> Parameters =>
        _lazyParameters.IsDefault ? _lazyParameters = CreateParameters() : _lazyParameters;

    public override TypeSymbol ReturnType { get; } = returnType;

    // public ImmutableArray<LocalSymbol> Locals { get; }

    private FrozenDictionary<string, ParameterSymbol>? _lazyParameterMap;
    public override FrozenDictionary<string, ParameterSymbol> ParameterMap =>
        _lazyParameterMap ??= CreateParameterMap();

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _lazyDiagnostics.IsDefault ? _lazyDiagnostics = _diagnosticBag.ToImmutableArray() : _lazyDiagnostics;

    MemberDeclarationSyntax IMemberSymbol.Syntax => Syntax;

    private ImmutableArray<ParameterSymbol> CreateParameters()
    {
        var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(Syntax.Parameters.Count);
        foreach (var syntax in Syntax.Parameters)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.SimpleParameterDeclaration:
                {
                    var parameterSyntax = (SimpleParameterDeclarationSyntax)syntax;
                    var type = Binder.BindType(parameterSyntax.Type, _diagnosticBag);
                    var parameter = new SimpleParameterSymbol(this, parameterSyntax, type);
                    builder.Add(parameter);
                    break;
                }

                case SyntaxKind.SelfParameterDeclaration:
                {
                    var self = (SelfParameterDeclarationSyntax)syntax;
                    var type = self.Star == null ? Container : new PointerTypeSymbol(self, Container);

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

    private FrozenDictionary<string, ParameterSymbol> CreateParameterMap()
    {
        Dictionary<string, ParameterSymbol> map = [];
        foreach (var parameter in Parameters)
        {
            if (parameter is SelfParameterSymbol self && self.Syntax.Type != null)
            {
                _diagnosticBag.AddError(self.Syntax.Type, DiagnosticMessages.SelfParameterCannotHaveAType);

                continue;
            }

            if (map.TryAdd(parameter.Name, parameter))
            {
                continue;
            }

            _diagnosticBag.AddError(parameter.Syntax, DiagnosticMessages.NameIsAlreadyDefined, parameter.Name);
        }

        return map.ToFrozenDictionary();
    }
}
