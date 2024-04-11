using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public abstract class LocalSymbol(FunctionSymbol function, LocalDeclarationSyntax syntax) : Symbol
{
    public override LocalDeclarationSyntax Syntax { get; } = syntax;
    public override string Name => Syntax.Identifier.IdentifierText;
    public override ModuleSymbol Module => Function.Module;
    public override bool IsMutable => Syntax.MutKeyword != null;

    public FunctionSymbol Function { get; } = function;
    public abstract TypeSymbol Type { get; }
    public abstract bool IsInitialized { get; }
    public abstract bool HasResolvedType { get; }
}

internal sealed class InitializedLocalSymbol : LocalSymbol
{
    public override TypeSymbol Type { get; }

    public override bool IsInitialized => true;
    public override bool HasResolvedType => true;

    public InitializedLocalSymbol(FunctionSymbol function, LocalDeclarationSyntax syntax, TypeSymbol type)
        : base(function, syntax)
    {
        Debug.Assert(syntax.Initializer != null);
        Type = type;
    }
}

internal sealed class LocalSymbolBuilder(FunctionSymbol function, LocalDeclarationSyntax syntax, TypeSymbol type)
{
    public LocalDeclarationSyntax Syntax { get; } = syntax;
    public string Name => Syntax.Identifier.IdentifierText;

    public FunctionSymbol Function { get; } = function;

    private TypeSymbol _type = type;
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

    private bool _isInitialized = false;
    public bool IsInitialized
    {
        get => _isInitialized;
        set
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("IsInitialized already set");
            }

            _isInitialized = value;
        }
    }
}

internal sealed class LateInitLocalSymbol(LocalSymbolBuilder builder) : LocalSymbol(builder.Function, builder.Syntax)
{
    public override TypeSymbol Type => Builder.Type;

    public override bool IsInitialized => Builder.IsInitialized;
    public override bool HasResolvedType => Builder.Type != PlaceholderTypeSymbol.ToBeInferred;

    public LocalSymbolBuilder Builder { get; } = builder;
}
