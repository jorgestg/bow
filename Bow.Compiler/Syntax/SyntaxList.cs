using System.Collections;

namespace Bow.Compiler.Syntax;

public struct SyntaxListBuilder<TNode>(SyntaxTree syntaxTree)
    where TNode : SyntaxNode
{
    private TNode[]? _nodes;
    private int _count;

    public SyntaxTree SyntaxTree { get; } = syntaxTree;

    public void Add(TNode node)
    {
        ResizeIfNecessary();
        _nodes![_count++] = node;
    }

    private void ResizeIfNecessary()
    {
        if (_nodes == null)
        {
            _nodes = new TNode[2];
            return;
        }

        if (_count < _nodes.Length)
        {
            return;
        }

        Array.Resize(ref _nodes, _count * 2);
    }

    public readonly SyntaxList<TNode> ToSyntaxList()
    {
        var nodes = _nodes ?? [];
        return new SyntaxList<TNode>(SyntaxTree, nodes, _count);
    }
}

public sealed class SyntaxList<TNode> : SyntaxNode, IReadOnlyList<TNode>
    where TNode : SyntaxNode
{
    private readonly ArraySegment<TNode> _nodes;

    public SyntaxList(SyntaxTree syntaxTree, TNode[] nodes)
        : base(syntaxTree)
    {
        _nodes = nodes;
    }

    internal SyntaxList(SyntaxTree syntaxTree, TNode[] nodes, int count)
        : base(syntaxTree)
    {
        _nodes = new ArraySegment<TNode>(nodes, 0, count);
    }

    public TNode this[int index] => _nodes[index];

    public override Location Location
    {
        get
        {
            if (_nodes.Count == 0)
            {
                return new Location(0, 0);
            }

            return _nodes[0].Location.Combine(_nodes[Count - 1].Location);
        }
    }

    public int Count => _nodes.Count;

    public int CountBy(Predicate<TNode> predicate)
    {
        var count = 0;
        foreach (var node in _nodes)
        {
            if (predicate(node))
            {
                count++;
            }
        }

        return count;
    }

    public ArraySegment<TNode>.Enumerator GetEnumerator()
    {
        return _nodes.GetEnumerator();
    }

    IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
