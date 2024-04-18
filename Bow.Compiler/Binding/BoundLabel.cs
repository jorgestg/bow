namespace Bow.Compiler.Symbols;

internal record struct BoundLabel(int Id)
{
    public readonly bool IsDefault => Id == 0;
}

internal sealed class BoundLabelFactory
{
    private static readonly BoundLabelFactory Instance = new();

    public static BoundLabel GenerateLabel()
    {
        return Instance.GenerateLabelInternal();
    }

    private int _nextLabelId = 1;

    private BoundLabelFactory() { }

    private BoundLabel GenerateLabelInternal()
    {
        return new BoundLabel(_nextLabelId++);
    }
}
