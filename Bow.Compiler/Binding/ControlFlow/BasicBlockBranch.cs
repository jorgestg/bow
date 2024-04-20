namespace Bow.Compiler.Binding.ControlFlow;

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
