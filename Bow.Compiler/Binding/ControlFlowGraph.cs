using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal enum BasicBlockKind
{
    Normal,
    Start,
    End
}

internal sealed class BasicBlock(
    BasicBlockKind kind,
    ImmutableArray<BoundStatement> statements,
    ImmutableArray<BasicBlockBranch> incoming,
    ImmutableArray<BasicBlockBranch> outgoing
)
{
    public BasicBlockKind Kind { get; } = kind;
    public ImmutableArray<BoundStatement> Statements { get; } = statements;
    public ImmutableArray<BasicBlockBranch> Incoming { get; } = incoming;
    public ImmutableArray<BasicBlockBranch> Outgoing { get; } = outgoing;
}

internal sealed class BasicBlockBuilder(BasicBlockKind kind, ImmutableArray<BoundStatement> statements)
{
    public BasicBlockKind Kind { get; } = kind;
    public ImmutableArray<BoundStatement> Statements { get; } = statements;

    private List<BasicBlockBranchBuilder>? _lazyIncoming;
    public List<BasicBlockBranchBuilder> Incoming => _lazyIncoming ??= [];

    private List<BasicBlockBranchBuilder>? _lazyOutgoing;
    public List<BasicBlockBranchBuilder> Outgoing => _lazyOutgoing ??= [];

    private BasicBlock? _lazyBasicBlock;
    public BasicBlock BasicBlock
    {
        get
        {
            if (_lazyBasicBlock != null)
                return _lazyBasicBlock;

            ImmutableArray<BasicBlockBranch> incoming;
            if (_lazyIncoming?.Count > 0)
            {
                var builder = ImmutableArray.CreateBuilder<BasicBlockBranch>(_lazyIncoming.Count);
                foreach (var branchBuilder in _lazyIncoming)
                    builder.Add(branchBuilder.BasicBlockBranch);

                incoming = builder.MoveToImmutable();
            }
            else
            {
                incoming = [];
            }

            ImmutableArray<BasicBlockBranch> outgoing;
            if (_lazyOutgoing?.Count > 0)
            {
                var builder = ImmutableArray.CreateBuilder<BasicBlockBranch>(_lazyOutgoing.Count);
                foreach (var branchBuilder in _lazyOutgoing)
                    builder.Add(branchBuilder.BasicBlockBranch);

                outgoing = builder.MoveToImmutable();
            }
            else
            {
                outgoing = [];
            }

            return _lazyBasicBlock ??= new BasicBlock(Kind, Statements, incoming, outgoing);
        }
    }
}

internal sealed class BasicBlockBranch(BasicBlockBranchBuilder builder)
{
    private readonly BasicBlockBranchBuilder _builder = builder;

    private BasicBlock? _lazyFrom;
    public BasicBlock From => _lazyFrom ??= _builder.From.BasicBlock;

    private BasicBlock? _lazyTo;
    public BasicBlock To => _lazyTo ??= _builder.To.BasicBlock;

    public BoundExpression? Condition => _builder.Condition;
}

internal sealed class BasicBlockBranchBuilder(BasicBlockBuilder from, BasicBlockBuilder to, BoundExpression? condition)
{
    public BasicBlockBuilder From { get; } = from;
    public BasicBlockBuilder To { get; } = to;
    public BoundExpression? Condition { get; } = condition;

    private BasicBlockBranch? _lazyBasicBlockBranch;
    public BasicBlockBranch BasicBlockBranch => _lazyBasicBlockBranch ??= new BasicBlockBranch(this);
}

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

            BasicBlockBuilder blockBuilder = new(BasicBlockKind.Normal, [.. statements]);
            blocks.Add(blockBuilder);
            statements.Clear();
        }
    }

    private static ControlFlowGraph Build(List<BasicBlockBuilder> blockBuilders)
    {
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
#error Handle JumpIfFalse
                        var branch = Connect(blockBuilder, targetBlock, conditionalGoto.Condition);
                        branchBuilders.Add(branch);
                        break;
                    }
                    case BoundNodeKind.ReturnStatement:
                    {
                        Debug.Assert(blockFromLabel != null);
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
    }
}
