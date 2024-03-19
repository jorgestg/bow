using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class FieldSymbol(
    StructSymbol @struct,
    FieldDeclarationSyntax syntax,
    TypeSymbol type
) : Symbol
{
    public override string Name => Syntax.Identifier.IdentifierText;

    public override SymbolAccessibility Accessibility
    {
        get
        {
            var defaultVisibility = Struct.IsData
                ? SymbolAccessibility.Public
                : SymbolAccessibility.Private;

            return SymbolFacts.GetAccessibilityFromToken(Syntax.AccessModifier, defaultVisibility);
        }
    }

    public override FieldDeclarationSyntax Syntax { get; } = syntax;
    public override ModuleSymbol Module => Struct.Module;
    internal override Binder Binder => Struct.Binder;

    public StructSymbol Struct { get; } = @struct;
    public bool IsMutable => Syntax.MutKeyword != null;
    public TypeSymbol Type { get; } = type;
}
