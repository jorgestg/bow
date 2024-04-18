namespace Bow.Compiler.Binding;

internal abstract class BoundTreeRewriter
{
    protected virtual BoundStatement RewriteStatement(BoundStatement node)
    {
        return node.Kind switch
        {
            BoundNodeKind.LocalDeclaration => RewriteLocalDeclaration((BoundLocalDeclaration)node),
            BoundNodeKind.BlockStatement => RewriteBlockStatement((BoundBlockStatement)node),
            BoundNodeKind.IfStatement => RewriteIfStatement((BoundIfStatement)node),
            BoundNodeKind.WhileStatement => RewriteWhileStatement((BoundWhileStatement)node),
            BoundNodeKind.ReturnStatement => RewriteReturnStatement((BoundReturnStatement)node),
            BoundNodeKind.AssignmentStatement => RewriteAssignmentStatement((BoundAssignmentStatement)node),
            BoundNodeKind.ExpressionStatement => RewriteExpressionStatement((BoundExpressionStatement)node),

            BoundNodeKind.LabelDeclarationStatement
                => RewriteLabelDeclarationStatement((BoundLabelDeclarationStatement)node),

            BoundNodeKind.GotoStatement => RewriteGotoStatement((BoundGotoStatement)node),

            BoundNodeKind.ConditionalGotoStatement
                => RewriteConditionalGotoStatement((BoundConditionalGotoStatement)node),

            _ => throw new UnreachableException()
        };
    }

    protected virtual BoundStatement RewriteLocalDeclaration(BoundLocalDeclaration node)
    {
        if (node.Initializer == null)
        {
            return node;
        }

        var initializer = RewriteExpression(node.Initializer);
        if (initializer == node.Initializer)
        {
            return node;
        }

        return new BoundLocalDeclaration(node.Syntax, node.Local, initializer);
    }

    protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
    {
        ImmutableArray<BoundStatement>.Builder? newStatements = null;
        for (var i = 0; i < node.Statements.Length; i++)
        {
            var statement = node.Statements[i];
            var newStatement = RewriteStatement(statement);
            if (statement == newStatement)
            {
                newStatements?.Add(statement);
                continue;
            }

            if (newStatements == null)
            {
                newStatements = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);
                for (var j = 0; j < i; j++)
                {
                    newStatements.Add(node.Statements[j]);
                }
            }

            newStatements.Add(newStatement);
        }

        if (newStatements == null)
        {
            return node;
        }

        return new BoundBlockStatement(node.Syntax, newStatements.MoveToImmutable());
    }

    protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
    {
        var condition = RewriteExpression(node.Condition);
        var then = RewriteStatement(node.Then);
        var @else = node.Else == null ? null : RewriteStatement(node.Else);
        if (condition == node.Condition && then == node.Then && @else == node.Else)
        {
            return node;
        }

        return new BoundIfStatement(node.Syntax, condition, then, @else);
    }

    protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
    {
        var condition = RewriteExpression(node.Condition);
        var body = RewriteStatement(node.Body);
        if (condition == node.Condition && body == node.Body)
        {
            return node;
        }

        return new BoundWhileStatement(node.Syntax, condition, body);
    }

    protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
    {
        if (node.Expression == null)
        {
            return node;
        }

        var expression = RewriteExpression(node.Expression);
        if (expression == node.Expression)
        {
            return node;
        }

        return new BoundReturnStatement(node.Syntax, expression);
    }

    protected virtual BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
    {
        var expression = RewriteExpression(node.Expression);
        if (expression == node.Expression)
        {
            return node;
        }

        return new BoundAssignmentStatement(node.Syntax, node.Assignee, expression);
    }

    protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
    {
        var expression = RewriteExpression(node.Expression);
        if (expression == node.Expression)
        {
            return node;
        }

        return new BoundExpressionStatement(node.Syntax, expression);
    }

    protected virtual BoundStatement RewriteLabelDeclarationStatement(BoundLabelDeclarationStatement node)
    {
        return node;
    }

    protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
    {
        return node;
    }

    protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
    {
        var condition = RewriteExpression(node.Condition);
        if (condition == node.Condition)
        {
            return node;
        }

        return new BoundConditionalGotoStatement(node.Syntax, node.Label, condition, node.JumpIfFalse);
    }

    protected virtual BoundExpression RewriteExpression(BoundExpression node)
    {
        return node.Kind switch
        {
            BoundNodeKind.MissingExpression => RewriteMissingExpression((BoundMissingExpression)node),
            BoundNodeKind.LiteralExpression => RewriteLiteralExpression((BoundLiteralExpression)node),
            BoundNodeKind.IdentifierExpression => RewriteIdentifierExpression((BoundIdentifierExpression)node),
            BoundNodeKind.CallExpression => RewriteCallExpression((BoundCallExpression)node),
            BoundNodeKind.CastExpression => RewriteCastExpression((BoundCastExpression)node),
            BoundNodeKind.UnaryExpression => RewriteUnaryExpression((BoundUnaryExpression)node),
            BoundNodeKind.BinaryExpression => RewriteBinaryExpression((BoundBinaryExpression)node),

            BoundNodeKind.StructCreationExpression
                => RewriteStructCreationExpression((BoundStructCreationExpression)node),

            _ => throw new UnreachableException()
        };
    }

    protected virtual BoundExpression RewriteMissingExpression(BoundMissingExpression node)
    {
        return node;
    }

    protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
    {
        return node;
    }

    protected virtual BoundExpression RewriteIdentifierExpression(BoundIdentifierExpression node)
    {
        return node;
    }

    protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
    {
        ImmutableArray<BoundExpression>.Builder? newArguments = null;
        for (var i = 0; i < node.Arguments.Length; i++)
        {
            var argument = node.Arguments[i];
            var newArgument = RewriteExpression(argument);
            if (argument == newArgument)
            {
                newArguments?.Add(argument);
                continue;
            }

            if (newArguments == null)
            {
                newArguments = ImmutableArray.CreateBuilder<BoundExpression>(node.Arguments.Length);
                for (var j = 0; j < i; j++)
                {
                    newArguments.Add(node.Arguments[j]);
                }
            }

            newArguments.Add(newArgument);
        }

        if (newArguments == null)
        {
            return node;
        }

        return new BoundCallExpression(node.Syntax, node.Function, newArguments.MoveToImmutable());
    }

    protected virtual BoundExpression RewriteCastExpression(BoundCastExpression node)
    {
        var expression = RewriteExpression(node.Expression);
        if (expression == node.Expression)
        {
            return node;
        }

        return new BoundCastExpression(node.Syntax, expression, node.Type);
    }

    protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
    {
        var operand = RewriteExpression(node.Operand);
        if (operand == node.Operand)
        {
            return node;
        }

        return new BoundUnaryExpression(node.Syntax, node.Operator, operand);
    }

    protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
    {
        var left = RewriteExpression(node.Left);
        var right = RewriteExpression(node.Right);
        if (left == node.Left && right == node.Right)
        {
            return node;
        }

        return new BoundBinaryExpression(node.Syntax, left, node.Operator, right);
    }

    protected virtual BoundExpression RewriteStructCreationExpression(BoundStructCreationExpression node)
    {
        ImmutableArray<BoundFieldInitializer>.Builder? newInitializers = null;
        for (var i = 0; i < node.FieldInitializers.Length; i++)
        {
            var initializerExpression = node.FieldInitializers[i].Expression;
            var newInitializerExpression = RewriteExpression(initializerExpression);
            if (initializerExpression == newInitializerExpression)
            {
                newInitializers?.Add(node.FieldInitializers[i]);
                continue;
            }

            if (newInitializers == null)
            {
                newInitializers = ImmutableArray.CreateBuilder<BoundFieldInitializer>(node.FieldInitializers.Length);
                for (var j = 0; j < i; j++)
                {
                    newInitializers.Add(node.FieldInitializers[j]);
                }
            }

            BoundFieldInitializer newInitializer = new(node.FieldInitializers[i].Field, newInitializerExpression);
            newInitializers.Add(newInitializer);
        }

        if (newInitializers == null)
        {
            return node;
        }

        return new BoundStructCreationExpression(node.Syntax, node.Type, newInitializers.MoveToImmutable());
    }
}
