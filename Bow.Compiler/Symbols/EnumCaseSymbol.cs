using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class EnumCaseSymbol(
    EnumSymbol @enum,
    EnumCaseDeclarationSyntax syntax,
    TypeSymbol? argumentType
) : Symbol
{
    public override string Name => Syntax.Identifier.IdentifierText;
    public override EnumCaseDeclarationSyntax Syntax { get; } = syntax;
    public override ModuleSymbol Module => Enum.Module;
    public override SymbolAccessibility Accessibility => SymbolAccessibility.Public;

    public EnumSymbol Enum { get; } = @enum;
    public TypeSymbol? ArgumentType { get; } = argumentType;
}
