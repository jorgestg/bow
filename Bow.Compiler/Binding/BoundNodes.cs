using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal enum BoundNodeKind
{
    // Statements
    LocalDeclaration,
    BlockStatement,
    IfStatement,
    WhileStatement,
    ReturnStatement,
    AssignmentStatement,
    ExpressionStatement,

    // Expressions
    MissingExpression,
    LiteralExpression,
    IdentifierExpression,
    CallExpression,
    CastExpression,
    UnaryExpression,
    BinaryExpression,
    StructCreationExpression
}

internal abstract class BoundNode
{
    public abstract BoundNodeKind Kind { get; }
    public abstract SyntaxNode Syntax { get; }
}

internal abstract class BoundStatement : BoundNode;

internal sealed class BoundLocalDeclaration(
    LocalDeclarationSyntax syntax,
    LocalSymbol local,
    BoundExpression? initializer
) : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.LocalDeclaration;
    public override LocalDeclarationSyntax Syntax { get; } = syntax;

    public LocalSymbol Local { get; } = local;
    public BoundExpression? Initializer { get; } = initializer;
}

internal sealed class BoundBlockStatement(BlockStatementSyntax syntax, ImmutableArray<BoundStatement> statements)
    : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;
    public override BlockStatementSyntax Syntax { get; } = syntax;

    public ImmutableArray<BoundStatement> Statements { get; } = statements;
}

internal sealed class BoundIfStatement(
    IfStatementSyntax syntax,
    BoundExpression condition,
    BoundBlockStatement then,
    ImmutableArray<BoundElseIfClause> elseIfs,
    BoundBlockStatement? @else
) : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
    public override IfStatementSyntax Syntax { get; } = syntax;

    public BoundExpression Condition { get; } = condition;
    public BoundBlockStatement Then { get; } = then;
    public ImmutableArray<BoundElseIfClause> ElseIfs { get; } = elseIfs;
    public BoundBlockStatement? Else { get; } = @else;
}

internal readonly struct BoundElseIfClause(BoundExpression condition, BoundBlockStatement block)
{
    public BoundExpression Condition { get; } = condition;
    public BoundBlockStatement Block { get; } = block;
}

internal sealed class BoundWhileStatement(
    WhileStatementSyntax syntax,
    BoundExpression condition,
    BoundBlockStatement body
) : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
    public override WhileStatementSyntax Syntax { get; } = syntax;

    public BoundExpression Condition { get; } = condition;
    public BoundBlockStatement Body { get; } = body;
}

internal sealed class BoundReturnStatement(ReturnStatementSyntax syntax, BoundExpression? expression) : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
    public override ReturnStatementSyntax Syntax { get; } = syntax;

    public BoundExpression? Expression { get; } = expression;
}

internal sealed class BoundAssignmentStatement(StatementSyntax syntax, Symbol assignee, BoundExpression expression)
    : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;
    public override StatementSyntax Syntax { get; } = syntax;

    public Symbol Assignee { get; } = assignee;
    public BoundExpression Expression { get; } = expression;
}

internal sealed class BoundExpressionStatement(ExpressionStatementSyntax syntax, BoundExpression expression)
    : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;
    public override ExpressionStatementSyntax Syntax { get; } = syntax;

    public BoundExpression Expression { get; } = expression;
}

internal abstract class BoundExpression : BoundNode
{
    public abstract override ExpressionSyntax Syntax { get; }
    public abstract TypeSymbol Type { get; }
}

internal sealed class BoundMissingExpression(MissingExpressionSyntax syntax) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.MissingExpression;
    public override ExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type => PlaceholderTypeSymbol.UnknownType;
}

internal sealed class BoundLiteralExpression(LiteralExpressionSyntax syntax, TypeSymbol type, object value)
    : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
    public override LiteralExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type { get; } = type;

    public object Value { get; } = value;
}

internal sealed class BoundIdentifierExpression(IdentifierExpressionSyntax syntax, Symbol referencedSymbol)
    : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.IdentifierExpression;
    public override IdentifierExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type
    {
        get
        {
            return ReferencedSymbol switch
            {
                MissingSymbol => PlaceholderTypeSymbol.UnknownType,
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
    CallExpressionSyntax syntax,
    FunctionSymbol function,
    ImmutableArray<BoundExpression> arguments
) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
    public override CallExpressionSyntax Syntax { get; } = syntax;
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

internal sealed class BoundUnaryExpression(UnaryExpressionSyntax syntax, BoundOperator op, BoundExpression operand)
    : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
    public override UnaryExpressionSyntax Syntax { get; } = syntax;
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
    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
    public override BinaryExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type => Operator.ResultType;

    public BoundExpression Left { get; } = left;
    public BoundOperator Operator { get; } = op;
    public BoundExpression Right { get; } = right;
}

internal sealed class BoundStructCreationExpression(
    StructCreationExpressionSyntax syntax,
    TypeSymbol type,
    ImmutableArray<BoundFieldInitializer> fieldInitializers
) : BoundExpression
{
    public override BoundNodeKind Kind => BoundNodeKind.StructCreationExpression;
    public override StructCreationExpressionSyntax Syntax { get; } = syntax;
    public override TypeSymbol Type { get; } = type;

    public ImmutableArray<BoundFieldInitializer> FieldInitializers { get; } = fieldInitializers;
}

internal readonly struct BoundFieldInitializer(FieldSymbol field, BoundExpression expression)
{
    public FieldSymbol Field { get; } = field;
    public BoundExpression Expression { get; } = expression;
}
