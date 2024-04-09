using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal enum BoundNodeKind
{
    // Statements
    BlockStatement,
    ExpressionStatement,
    ReturnStatement,
    IfStatement,

    // Expressions
    MissingExpression,
    LiteralExpression,
    IdentifierExpression,
    CallExpression,
    CastExpression,
    UnaryExpression,
    BinaryExpression,
}

internal abstract class BoundNode
{
    public abstract SyntaxNode Syntax { get; }
    public abstract BoundNodeKind Kind { get; }
}

internal abstract class BoundStatement : BoundNode;

internal sealed class BoundBlockStatement(
    BlockStatementSyntax syntax,
    ImmutableArray<BoundStatement> statements
) : BoundStatement
{
    public override BlockStatementSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;

    public ImmutableArray<BoundStatement> Statements { get; } = statements;
}

internal sealed class BoundReturnStatement(
    ReturnStatementSyntax syntax,
    BoundExpression? expression
) : BoundStatement
{
    public override ReturnStatementSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;

    public BoundExpression? Expression { get; } = expression;
}

internal sealed class BoundExpressionStatement(
    ExpressionStatementSyntax syntax,
    BoundExpression expression
) : BoundStatement
{
    public override ExpressionStatementSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

    public BoundExpression Expression { get; } = expression;
}

internal sealed class BoundIfStatement(
    IfStatementSyntax syntax,
    BoundExpression condition,
    BoundBlockStatement then,
    ImmutableArray<BoundIfElseClause> elseIfs,
    BoundBlockStatement? @else
) : BoundStatement
{
    public override IfStatementSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;

    public BoundExpression Condition { get; } = condition;
    public BoundBlockStatement Then { get; } = then;
    public ImmutableArray<BoundIfElseClause> ElseIfs { get; } = elseIfs;
    public BoundBlockStatement? Else { get; } = @else;
}

internal readonly struct BoundIfElseClause(BoundExpression condition, BoundBlockStatement block)
{
    public BoundExpression Condition { get; } = condition;
    public BoundBlockStatement Block { get; } = block;
}

internal abstract class BoundExpression : BoundNode
{
    public abstract override ExpressionSyntax Syntax { get; }
    public abstract TypeSymbol Type { get; }
}

internal sealed class BoundMissingExpression(MissingExpressionSyntax syntax) : BoundExpression
{
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.MissingExpression;
    public override TypeSymbol Type => PlaceholderTypeSymbol.UnknownType;
}

internal sealed class BoundLiteralExpression(
    LiteralExpressionSyntax syntax,
    TypeSymbol type,
    object value
) : BoundExpression
{
    public override LiteralExpressionSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
    public override TypeSymbol Type { get; } = type;

    public object Value { get; } = value;
}

internal sealed class BoundIdentifierExpression(
    IdentifierExpressionSyntax syntax,
    Symbol referencedSymbol
) : BoundExpression
{
    public override IdentifierExpressionSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.IdentifierExpression;
    public override TypeSymbol Type
    {
        get
        {
            return ReferencedSymbol switch
            {
                TypeSymbol => PlaceholderTypeSymbol.MetaType,
                EnumCaseSymbol e => e.Enum,
                FieldSymbol f => f.Type,
                FunctionSymbol f => f.Type,
                ModuleSymbol => PlaceholderTypeSymbol.ModuleType,
                PackageSymbol => PlaceholderTypeSymbol.PackageType,
                ParameterSymbol p => p.Type,
                _ => throw new UnreachableException()
            };
        }
    }

    public Symbol ReferencedSymbol { get; } = referencedSymbol;
}

internal sealed class BoundCallExpression(
    CallExpressionSyntax syntax,
    FunctionSymbol function,
    ImmutableArray<BoundExpression> arguments
) : BoundExpression
{
    public override CallExpressionSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
    public override TypeSymbol Type => Function.ReturnType;

    public FunctionSymbol Function { get; } = function;
    public ImmutableArray<BoundExpression> Arguments { get; } = arguments;
}

internal sealed class BoundCastExpression(
    ExpressionSyntax syntax,
    BoundExpression expression,
    TypeSymbol type
) : BoundExpression
{
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.CastExpression;
    public override TypeSymbol Type { get; } = type;

    public BoundExpression Expression { get; } = expression;
}

internal sealed class BoundUnaryExpression(
    UnaryExpressionSyntax syntax,
    BoundOperator op,
    BoundExpression operand
) : BoundExpression
{
    public override UnaryExpressionSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
    public override TypeSymbol Type => Operator.ResultType;

    public BoundOperator Operator { get; } = op;
    public BoundExpression Operand { get; } = operand;
}

internal sealed class BoundBinaryExpression(
    BinaryExpressionSyntax syntax,
    BoundExpression left,
    BoundOperator op,
    BoundExpression right
) : BoundExpression
{
    public override BinaryExpressionSyntax Syntax { get; } = syntax;
    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
    public override TypeSymbol Type => Operator.ResultType;

    public BoundExpression Left { get; } = left;
    public BoundOperator Operator { get; } = op;
    public BoundExpression Right { get; } = right;
}
