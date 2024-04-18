using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class Lowerer : BoundTreeRewriter
{
    private Lowerer() { }

    public static BoundBlockStatement Lower(BoundStatement statement)
    {
        Lowerer lowerer = new();
        statement = lowerer.RewriteStatement(statement);
        return Flatten(statement);
    }

    private static BoundBlockStatement Flatten(BoundStatement statement)
    {
        if (statement.Kind != BoundNodeKind.BlockStatement)
        {
            return new BoundBlockStatement(statement.Syntax, [statement]);
        }

        var block = (BoundBlockStatement)statement;
        if (block.Statements.All(statement => statement.Kind != BoundNodeKind.BlockStatement))
        {
            return block;
        }

        var syntax = statement.Syntax;

        Stack<BoundStatement> stack = new();
        stack.Push(statement);

        var builder = ImmutableArray.CreateBuilder<BoundStatement>();
        while (stack.Count > 0)
        {
            statement = stack.Pop();
            if (statement.Kind != BoundNodeKind.BlockStatement)
            {
                builder.Add(statement);
                continue;
            }

            block = (BoundBlockStatement)statement;
            for (var i = block.Statements.Length - 1; i >= 0; i--)
            {
                stack.Push(block.Statements[i]);
            }
        }

        return new BoundBlockStatement(syntax, builder.DrainToImmutable());
    }

    protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
    {
        if (node.Else == null)
        {
            // jne <condition>, 1, @end
            // <then block>
            // @end:
            // ...
            var endLabel = BoundLabelFactory.GenerateLabel();
            BoundConditionalGotoStatement gotoEnd = new(node.Syntax, endLabel, node.Condition, jumpIfFalse: true);
            BoundLabelDeclarationStatement endLabelDeclaration = new(node.Syntax, endLabel);
            BoundBlockStatement block = new(node.Syntax.Then, [gotoEnd, node.Then, endLabelDeclaration]);
            return RewriteBlockStatement(block);
        }
        else
        {
            // jne <condition>, 1, @else
            // <then block>
            // jmp @end
            // @else:
            // <else block>
            // @end:
            // ...
            var elseLabel = BoundLabelFactory.GenerateLabel();
            var endLabel = BoundLabelFactory.GenerateLabel();
            BoundConditionalGotoStatement gotoElse = new(node.Syntax, elseLabel, node.Condition, jumpIfFalse: true);
            BoundGotoStatement gotoEnd = new(node.Syntax, endLabel);
            BoundLabelDeclarationStatement elseLabelDeclaration = new(node.Syntax, elseLabel);
            BoundLabelDeclarationStatement endLabelDeclaration = new(node.Syntax, endLabel);
            BoundBlockStatement block =
                new(
                    node.Syntax.Then,
                    [gotoElse, node.Then, gotoEnd, elseLabelDeclaration, node.Else, endLabelDeclaration]
                );

            return RewriteBlockStatement(block);
        }
    }

    protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
    {
        // goto @continue
        // @body:
        // <body>
        // @continue:
        // jeq <condition>, 1, @body
        // @break:
        var bodyLabel = BoundLabelFactory.GenerateLabel();
        BoundGotoStatement gotoContinue = new(node.Syntax, node.ContinueLabel);
        BoundLabelDeclarationStatement bodyLabelDeclaration = new(node.Syntax, bodyLabel);
        BoundLabelDeclarationStatement checkLabelDeclaration = new(node.Syntax, node.ContinueLabel);
        BoundConditionalGotoStatement gotoBody = new(node.Syntax, bodyLabel, node.Condition, jumpIfFalse: false);
        BoundLabelDeclarationStatement breakLabelDeclaration = new(node.Syntax, node.BreakLabel);
        BoundBlockStatement block =
            new(
                node.Syntax,
                [gotoContinue, bodyLabelDeclaration, node.Body, checkLabelDeclaration, gotoBody, breakLabelDeclaration]
            );

        return RewriteBlockStatement(block);
    }
}
