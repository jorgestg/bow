namespace Bow.Compiler.Binding.ControlFlow;

internal enum BasicBlockKind
{
    Intermediate,
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

            return _lazyBasicBlock = new BasicBlock(Kind, Statements, incoming, outgoing);
        }
    }
}
