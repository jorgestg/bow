using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public abstract class ParameterSymbol(FunctionSymbol function, TypeSymbol type) : Symbol
{
    public abstract override ParameterDeclarationSyntax Syntax { get; }
    public override ModuleSymbol Module => Function.Module;
    public override bool IsMutable => Syntax.MutKeyword != null;

    public FunctionSymbol Function { get; } = function;
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

public sealed class SelfParameterSymbol(FunctionSymbol function, SelfParameterDeclarationSyntax syntax, TypeSymbol type)
    : ParameterSymbol(function, type)
{
    public override string Name => "self";
    public override SelfParameterDeclarationSyntax Syntax { get; } = syntax;
}
