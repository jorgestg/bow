using System.Collections.Frozen;
using Bow.Compiler.Binding;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class FunctionItemSymbol(ModuleSymbol module, FunctionDefinitionSyntax syntax)
    : FunctionSymbol,
        IItemSymbol
{
    private readonly DiagnosticBag _diagnosticBag = new();

    public override string Name => Syntax.Identifier.IdentifierText;
    public override FunctionDefinitionSyntax Syntax { get; } = syntax;
    public override ModuleSymbol Module { get; } = module;

    private FunctionBinder? _lazyBinder;
    internal FunctionBinder Binder => _lazyBinder ??= new(this);

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    private ImmutableArray<ParameterSymbol> _lazyParameters;
    public override ImmutableArray<ParameterSymbol> Parameters =>
        _lazyParameters.IsDefault ? _lazyParameters = CreateParameters() : _lazyParameters;

    private TypeSymbol? _lazyReturnType;
    public override TypeSymbol ReturnType => _lazyReturnType ??= BindReturnType();

    // public ImmutableArray<LocalSymbol> Locals { get; }

    private FrozenDictionary<string, ParameterSymbol>? _lazyParameterMap;
    public override FrozenDictionary<string, ParameterSymbol> ParameterMap =>
        _lazyParameterMap ??= CreateParameterMap();

    private ImmutableArray<Diagnostic> _lazyDiagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _lazyDiagnostics.IsDefault ? _lazyDiagnostics = _diagnosticBag.ToImmutableArray() : _lazyDiagnostics;

    ItemSyntax IItemSymbol.Syntax => Syntax;

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
                    Debug.Assert(self.Type != null);
                    Debug.Assert(self.Star == null);
                    var type = Binder.BindType(self.Type, _diagnosticBag);
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
        return Syntax.ReturnType == null ? BuiltInPackage.UnitType : Binder.BindType(Syntax.ReturnType, _diagnosticBag);
    }

    private FrozenDictionary<string, ParameterSymbol> CreateParameterMap()
    {
        Dictionary<string, ParameterSymbol> map = [];
        foreach (var parameter in Parameters)
        {
            if (map.TryAdd(parameter.Name, parameter))
            {
                continue;
            }

            _diagnosticBag.AddError(parameter.Syntax, DiagnosticMessages.NameIsAlreadyDefined, parameter.Name);
        }

        return map.ToFrozenDictionary();
    }
}
