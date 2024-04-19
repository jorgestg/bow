namespace Bow.Compiler.Symbols;

internal record struct BoundLabel(int Id)
{
    public readonly bool IsDefault => Id == 0;
}

internal struct BoundLabelGenerator()
{
    private int _nextLabelId = 1;

    public BoundLabel GenerateLabel()
    {
        return new BoundLabel(_nextLabelId++);
    }
}
