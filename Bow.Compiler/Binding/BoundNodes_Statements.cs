using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal abstract class BoundStatement(StatementSyntax syntax) : BoundNode
{
    public override StatementSyntax Syntax { get; } = syntax;
}

internal sealed class BoundLocalDeclaration(StatementSyntax syntax, LocalSymbol local, BoundExpression? initializer)
    : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.LocalDeclaration;

    public LocalSymbol Local { get; } = local;
    public BoundExpression? Initializer { get; } = initializer;
}

internal sealed class BoundBlockStatement(StatementSyntax syntax, ImmutableArray<BoundStatement> statements)
    : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;

    public ImmutableArray<BoundStatement> Statements { get; } = statements;
}

internal sealed class BoundIfStatement(
    StatementSyntax syntax,
    BoundExpression condition,
    BoundStatement then,
    BoundStatement? @else
) : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;

    public BoundExpression Condition { get; } = condition;
    public BoundStatement Then { get; } = then;
    public BoundStatement? Else { get; } = @else;
}

internal sealed class BoundWhileStatement(
    StatementSyntax syntax,
    BoundExpression condition,
    BoundStatement body,
    BoundLabel breakLabel,
    BoundLabel continueLabel
) : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;

    public BoundExpression Condition { get; } = condition;
    public BoundStatement Body { get; } = body;
    public BoundLabel BreakLabel { get; } = breakLabel;
    public BoundLabel ContinueLabel { get; } = continueLabel;
}

internal sealed class BoundReturnStatement(StatementSyntax syntax, BoundExpression? expression) : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;

    public BoundExpression? Expression { get; } = expression;
}

internal sealed class BoundAssignmentStatement(StatementSyntax syntax, Symbol assignee, BoundExpression expression)
    : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;

    public Symbol Assignee { get; } = assignee;
    public BoundExpression Expression { get; } = expression;
}

internal sealed class BoundExpressionStatement(StatementSyntax syntax, BoundExpression expression)
    : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

    public BoundExpression Expression { get; } = expression;
}

internal sealed class BoundLabelDeclarationStatement(StatementSyntax syntax, BoundLabel label) : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.LabelDeclarationStatement;

    public BoundLabel Label { get; } = label;
}

internal sealed class BoundGotoStatement(StatementSyntax syntax, BoundLabel label) : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

    public BoundLabel Label { get; } = label;
}

internal sealed class BoundConditionalGotoStatement(
    StatementSyntax syntax,
    BoundLabel label,
    BoundExpression condition,
    bool jumpIfFalse
) : BoundStatement(syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

    public BoundLabel Label { get; } = label;
    public BoundExpression Condition { get; } = condition;
    public bool JumpIfFalse { get; } = jumpIfFalse;
}
