using System.Collections;

namespace Bow.Compiler.Syntax;

public sealed class SyntaxList<TNode>(SyntaxTree syntaxTree, IList<TNode> nodes)
    : SyntaxNode(syntaxTree),
        IReadOnlyList<TNode>
    where TNode : SyntaxNode
{
    private readonly TNode[] _nodes = nodes is TNode[] array ? array : [.. nodes];

    public TNode this[int index] => _nodes[index];

    public override Location Location
    {
        get
        {
            if (nodes.Count == 0)
            {
                return new Location(0, 0);
            }

            return _nodes[0].Location.Combine(_nodes[^1].Location);
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
