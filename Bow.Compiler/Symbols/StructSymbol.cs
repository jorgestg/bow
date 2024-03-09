using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class StructSymbol(ModuleSymbol module, StructDefinitionSyntax syntax) : TypeSymbol
{
    public override string Name => Syntax.Identifier.IdentifierText;

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, SymbolAccessibility.File);

    public override StructDefinitionSyntax Syntax { get; } = syntax;

    private StructBinder? _lazyBinder;
    internal override StructBinder Binder => _lazyBinder ??= new(this);

    public ModuleSymbol Module { get; } = module;
    public bool IsData => Syntax.Keyword.ContextualKeywordKind == ContextualKeywordKind.Data;

    private ImmutableArray<FieldSymbol>? _lazyFields;
    public ImmutableArray<FieldSymbol> Fields => _lazyFields ??= CreateFields();

    private ImmutableArray<MethodSymbol>? _lazyMethods;
    public ImmutableArray<MethodSymbol> Methods => _lazyMethods ??= CreateMethods();

    private ImmutableArray<FieldSymbol> CreateFields()
    {
        var builder = ImmutableArray.CreateBuilder<FieldSymbol>(Syntax.Fields.Count);
        foreach (var syntax in Syntax.Fields)
        {
            var type = Binder.BindType(syntax.Type);
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
            var returnType = Binder.BindType(syntax.ReturnType);
            MethodSymbol method = new(this, syntax, returnType);
            builder.Add(method);
        }

        return builder.MoveToImmutable();
    }
}

public sealed class FieldSymbol(
    StructSymbol @struct,
    FieldDeclarationSyntax syntax,
    TypeSymbol type
) : Symbol
{
    public override string Name => Syntax.Identifier.IdentifierText;

    public override SymbolAccessibility Accessibility =>
        SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier);

    public override FieldDeclarationSyntax Syntax { get; } = syntax;
    internal override Binder Binder => Struct.Binder;

    public StructSymbol Struct { get; } = @struct;
    public bool IsMutable => Syntax.MutKeyword != null;
    public TypeSymbol Type { get; } = type;
}
