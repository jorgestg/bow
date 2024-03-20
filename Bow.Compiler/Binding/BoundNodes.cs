using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal abstract class BoundNode
{
    public abstract SyntaxNode Syntax { get; }
}

internal abstract class BoundStatement : BoundNode;

internal sealed class BoundBlock(BlockSyntax syntax, ImmutableArray<BoundStatement> statements)
    : BoundStatement
{
    public override BlockSyntax Syntax { get; } = syntax;
    public ImmutableArray<BoundStatement> Statements { get; } = statements;
}

internal sealed class BoundReturnStatement(
    ReturnStatementSyntax syntax,
    BoundExpression? expression
) : BoundStatement
{
    public override ReturnStatementSyntax Syntax { get; } = syntax;
    public BoundExpression? Expression { get; } = expression;
}

internal sealed class BoundExpressionStatement(
    ExpressionStatementSyntax syntax,
    BoundExpression expression
) : BoundStatement
{
    public override ExpressionStatementSyntax Syntax { get; } = syntax;
    public BoundExpression Expression { get; } = expression;
}

internal abstract class BoundExpression : BoundNode
{
    public abstract TypeSymbol Type { get; }
}

internal sealed class BoundLiteralExpression(
    LiteralExpressionSyntax syntax,
    TypeSymbol type,
    object value
) : BoundExpression
{
    public override LiteralExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type { get; } = type;
    public object Value { get; } = value;
}
