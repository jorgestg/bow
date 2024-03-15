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
            _nodes = new TNode[4];
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
        if (_count == nodes.Length)
        {
            return new SyntaxList<TNode>(SyntaxTree, nodes);
        }

        return new SyntaxList<TNode>(SyntaxTree, nodes[.._count]);
    }
}

public sealed class SyntaxList<TNode>(SyntaxTree syntaxTree, TNode[] nodes)
    : SyntaxNode(syntaxTree),
        IReadOnlyList<TNode>
    where TNode : SyntaxNode
{
    private readonly TNode[] _nodes = nodes;

    public TNode this[int index] => _nodes[index];

    public override Location Location
    {
        get
        {
            if (_nodes.Length == 0)
            {
                return new Location(0, 0);
            }

            return _nodes[0].Location.Combine(_nodes[Count - 1].Location);
        }
    }

    public int Count => _nodes.Length;

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
        return new ArraySegment<TNode>(_nodes).GetEnumerator();
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
