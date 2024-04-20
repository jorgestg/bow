using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding.ControlFlow;

internal readonly struct ControlFlowGraph
{
    private ControlFlowGraph(
        BasicBlock start,
        BasicBlock end,
        ImmutableArray<BasicBlock> blocks,
        ImmutableArray<BasicBlockBranch> branches
    )
    {
        Start = start;
        End = end;
        Blocks = blocks;
        Branches = branches;
    }

    public BasicBlock Start { get; }
    public BasicBlock End { get; }
    public ImmutableArray<BasicBlock> Blocks { get; }
    public ImmutableArray<BasicBlockBranch> Branches { get; }

    private static List<BasicBlockBuilder> CreateBlockBuilders(BoundBlockStatement block)
    {
        List<BasicBlockBuilder> blocks = [];
        if (block.Statements.Length == 0)
        {
            return blocks;
        }

        List<BoundStatement> statements = [];

        foreach (var statement in block.Statements)
        {
            switch (statement.Kind)
            {
                case BoundNodeKind.LabelDeclarationStatement:
                    StartBlock(blocks, statements);
                    statements.Add(statement);
                    break;
                case BoundNodeKind.GotoStatement:
                case BoundNodeKind.ConditionalGotoStatement:
                case BoundNodeKind.ReturnStatement:
                    statements.Add(statement);
                    StartBlock(blocks, statements);
                    break;
                case BoundNodeKind.LocalDeclaration:
                case BoundNodeKind.AssignmentStatement:
                case BoundNodeKind.ExpressionStatement:
                    statements.Add(statement);
                    break;
                default:
                    throw new UnreachableException();
            }
        }

        StartBlock(blocks, statements);
        return blocks;

        static void StartBlock(List<BasicBlockBuilder> blocks, List<BoundStatement> statements)
        {
            if (statements.Count == 0)
            {
                return;
            }

            BasicBlockBuilder blockBuilder = new(BasicBlockKind.Intermediate, [.. statements]);
            blocks.Add(blockBuilder);
            statements.Clear();
        }
    }

    public static ControlFlowGraph Create(BoundBlockStatement block)
    {
        var blockBuilders = CreateBlockBuilders(block);
        if (blockBuilders.Count == 0)
        {
            BasicBlock start = new(BasicBlockKind.Start, statements: [], incoming: [], outgoing: []);
            BasicBlock end = new(BasicBlockKind.End, statements: [], incoming: [], outgoing: []);
            return new ControlFlowGraph(start, end, blocks: [start, end], branches: []);
        }

        BasicBlockBuilder startBuilder = new(BasicBlockKind.Start, statements: []);
        BasicBlockBuilder endBuilder = new(BasicBlockKind.End, statements: []);

        Dictionary<BoundLabel, BasicBlockBuilder>? blockFromLabel = null;
        var branchCount = 0;
        foreach (var blockBuilder in blockBuilders)
        {
            foreach (var statement in blockBuilder.Statements)
            {
                switch (statement.Kind)
                {
                    case BoundNodeKind.LabelDeclarationStatement:
                    {
                        blockFromLabel ??= [];

                        var labelDeclaration = (BoundLabelDeclarationStatement)statement;
                        blockFromLabel[labelDeclaration.Label] = blockBuilder;
                        break;
                    }

                    case BoundNodeKind.GotoStatement:
                    case BoundNodeKind.ConditionalGotoStatement:
                    case BoundNodeKind.ReturnStatement:
                        branchCount++;
                        break;
                }
            }
        }

        List<BasicBlockBranchBuilder>? branchBuilders = branchCount > 0 ? new(capacity: branchCount) : null;

        foreach (var blockBuilder in blockBuilders)
        {
            foreach (var statement in blockBuilder.Statements)
            {
                switch (statement.Kind)
                {
                    case BoundNodeKind.LabelDeclarationStatement:
                    {
                        Debug.Assert(blockFromLabel != null);

                        var labelDeclaration = (BoundLabelDeclarationStatement)statement;
                        blockFromLabel[labelDeclaration.Label] = blockBuilder;
                        break;
                    }
                    case BoundNodeKind.GotoStatement:
                    {
                        Debug.Assert(blockFromLabel != null);
                        Debug.Assert(branchBuilders != null);

                        var @goto = (BoundGotoStatement)statement;
                        var targetBlock = blockFromLabel[@goto.Label];
                        var branch = Connect(blockBuilder, targetBlock);
                        branchBuilders.Add(branch);
                        break;
                    }
                    case BoundNodeKind.ConditionalGotoStatement:
                    {
                        Debug.Assert(blockFromLabel != null);
                        Debug.Assert(branchBuilders != null);

                        var conditionalGoto = (BoundConditionalGotoStatement)statement;

                        var targetBlock = blockFromLabel[conditionalGoto.Label];

                        var currentBlockIndex = blockBuilders.IndexOf(blockBuilder);
                        var fallthroughBlock =
                            currentBlockIndex == blockBuilders.Count - 1
                                ? endBuilder
                                : blockBuilders[currentBlockIndex + 1];

                        var negatedCondition = NegateCondition(conditionalGoto.Condition);
                        var targetCondition = conditionalGoto.JumpIfFalse
                            ? negatedCondition
                            : conditionalGoto.Condition;

                        var fallthroughCondition = conditionalGoto.JumpIfFalse
                            ? conditionalGoto.Condition
                            : negatedCondition;

                        branchBuilders.Add(Connect(blockBuilder, targetBlock, targetCondition));
                        branchBuilders.Add(Connect(blockBuilder, fallthroughBlock, fallthroughCondition));
                        break;
                    }
                    case BoundNodeKind.ReturnStatement:
                    {
                        Debug.Assert(branchBuilders != null);

                        var branch = Connect(blockBuilder, endBuilder);
                        branchBuilders.Add(branch);
                        break;
                    }
                    case BoundNodeKind.LocalDeclaration:
                    case BoundNodeKind.AssignmentStatement:
                    case BoundNodeKind.ExpressionStatement:
                        break;
                    default:
                        throw new UnreachableException();
                }
            }
        }

        var blocks = ImmutableArray.CreateBuilder<BasicBlock>(blockBuilders.Count + 2);
        blocks.Add(startBuilder.BasicBlock);
        foreach (var blockBuilder in blockBuilders)
        {
            blocks.Add(blockBuilder.BasicBlock);
        }

        blocks.Add(endBuilder.BasicBlock);

        ImmutableArray<BasicBlockBranch> branches;
        if (branchBuilders?.Count > 0)
        {
            var branchesBuilder = ImmutableArray.CreateBuilder<BasicBlockBranch>(branchCount);
            foreach (var branchBuilder in branchBuilders)
                branchesBuilder.Add(branchBuilder.BasicBlockBranch);

            branches = branchesBuilder.MoveToImmutable();
        }
        else
        {
            branches = [];
        }

        return new ControlFlowGraph(startBuilder.BasicBlock, endBuilder.BasicBlock, blocks.MoveToImmutable(), branches);

        static BasicBlockBranchBuilder Connect(
            BasicBlockBuilder from,
            BasicBlockBuilder to,
            BoundExpression? condition = null
        )
        {
            BasicBlockBranchBuilder branch = new(from, to, condition);
            from.Outgoing.Add(branch);
            to.Incoming.Add(branch);
            return branch;
        }

        static BoundExpression NegateCondition(BoundExpression condition)
        {
            var @operator = BoundOperator.TryBindUnaryOperator(SyntaxKind.NotKeyword, condition.Type, out var op)
                ? op
                : BoundOperator.CreateErrorUnaryOperator(SyntaxKind.NotKeyword);

            return new BoundUnaryExpression(condition.Syntax, @operator, condition);
        }
    }
}
