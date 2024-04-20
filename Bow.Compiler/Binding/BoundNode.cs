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

    // Lowering-only
    LabelDeclarationStatement,
    GotoStatement,
    ConditionalGotoStatement,

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
