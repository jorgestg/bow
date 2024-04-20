using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal abstract class BoundExpression : BoundNode
{
    public abstract override ExpressionSyntax Syntax { get; }
    public abstract TypeSymbol Type { get; }

    public virtual ConstantValue? ConstantValue => null;
}

internal sealed class BoundMissingExpression(MissingExpressionSyntax syntax) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.MissingExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type => PlaceholderTypeSymbol.Instance;
}

internal sealed class BoundLiteralExpression(LiteralExpressionSyntax syntax, TypeSymbol type, object value)
    : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
    public override LiteralExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type => type;
    public override ConstantValue ConstantValue { get; } = new ConstantValue(value);

    public object Value => ConstantValue.Value;
}

internal sealed class BoundIdentifierExpression(ExpressionSyntax syntax, Symbol referencedSymbol) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.IdentifierExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type
    {
        get
        {
            return ReferencedSymbol switch
            {
                PlaceholderSymbol => PlaceholderTypeSymbol.Instance,
                TypeSymbol => PlaceholderTypeSymbol.MetaType,
                EnumCaseSymbol e => e.Enum,
                FieldSymbol f => f.Type,
                FunctionSymbol f => f.Type,
                ModuleSymbol => PlaceholderTypeSymbol.ModuleType,
                PackageSymbol => PlaceholderTypeSymbol.PackageType,
                ParameterSymbol p => p.Type,
                LocalSymbol l => l.Type,
                _ => throw new UnreachableException()
            };
        }
    }

    public Symbol ReferencedSymbol { get; } = referencedSymbol;
}

internal sealed class BoundCallExpression(
    ExpressionSyntax syntax,
    FunctionSymbol function,
    ImmutableArray<BoundExpression> arguments
) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type => Function.ReturnType;

    public FunctionSymbol Function { get; } = function;
    public ImmutableArray<BoundExpression> Arguments { get; } = arguments;
}

internal sealed class BoundCastExpression(ExpressionSyntax syntax, BoundExpression expression, TypeSymbol type)
    : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.CastExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type { get; } = type;

    public BoundExpression Expression { get; } = expression;
}

internal sealed class BoundUnaryExpression(ExpressionSyntax syntax, BoundOperator op, BoundExpression operand)
    : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type => Operator.ResultType;

    public BoundOperator Operator { get; } = op;
    public BoundExpression Operand { get; } = operand;

    public override ConstantValue? ConstantValue { get; } = ConstantValueResolver.Resolve(op, operand);
}

internal sealed class BoundBinaryExpression(
    ExpressionSyntax syntax,
    BoundExpression left,
    BoundOperator op,
    BoundExpression right
) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type => Operator.ResultType;
    public override ConstantValue? ConstantValue { get; } = ConstantValueResolver.Resolve(left, op, right);

    public BoundExpression Left { get; } = left;
    public BoundOperator Operator { get; } = op;
    public BoundExpression Right { get; } = right;
}

internal sealed class BoundStructCreationExpression(
    ExpressionSyntax syntax,
    TypeSymbol type,
    ImmutableArray<BoundFieldInitializer> fieldInitializers
) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.StructCreationExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type { get; } = type;

    public ImmutableArray<BoundFieldInitializer> FieldInitializers { get; } = fieldInitializers;
}

internal readonly struct BoundFieldInitializer(FieldSymbol field, BoundExpression expression)
{
    public FieldSymbol Field { get; } = field;
    public BoundExpression Expression { get; } = expression;
}
