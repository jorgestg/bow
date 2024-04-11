using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public abstract class LocalSymbol(FunctionSymbol function, LocalDeclarationSyntax syntax) : Symbol
{
    public override LocalDeclarationSyntax Syntax { get; } = syntax;
    public override string Name => Syntax.Identifier.IdentifierText;
    public override ModuleSymbol Module => Function.Module;

    public FunctionSymbol Function { get; } = function;
    public bool IsMutable => Syntax.MutKeyword != null;
    public abstract TypeSymbol Type { get; }
}

public sealed class SimpleLocalSymbol(FunctionSymbol function, LocalDeclarationSyntax syntax, TypeSymbol type)
    : LocalSymbol(function, syntax)
{
    public override TypeSymbol Type { get; } = type;
}

internal sealed class LocalSymbolBuilder(FunctionSymbol function, LocalDeclarationSyntax syntax)
{
    public LocalDeclarationSyntax Syntax { get; } = syntax;
    public string Name => Syntax.Identifier.IdentifierText;

    public FunctionSymbol Function { get; } = function;
    public bool IsMutable => Syntax.MutKeyword != null;

    private TypeSymbol _type = PlaceholderTypeSymbol.ToBeInferred;
    public TypeSymbol Type
    {
        get => _type;
        set
        {
            if (_type != PlaceholderTypeSymbol.ToBeInferred)
            {
                throw new InvalidOperationException("Type already set");
            }

            _type = value;
        }
    }

    public bool IsResolved => Type != PlaceholderTypeSymbol.ToBeInferred;
}

internal sealed class LateBoundLocalSymbol(LocalSymbolBuilder builder) : LocalSymbol(builder.Function, builder.Syntax)
{
    public override TypeSymbol Type => Builder.Type;

    public LocalSymbolBuilder Builder { get; } = builder;
}
