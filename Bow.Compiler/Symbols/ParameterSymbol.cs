using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

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
