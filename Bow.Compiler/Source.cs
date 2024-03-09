namespace Bow.Compiler;

public class SourceText(string fileName, string text)
{
    public string FileName { get; } = fileName;
    public string Text { get; } = text;

    public ReadOnlyMemory<char> GetTextRange(Location location)
    {
        return Text.AsMemory(location.Start, location.Length);
    }
}

public readonly struct Location(int start, int length)
{
    public int Start { get; } = start;
    public int Length { get; } = length;
    public int End => Start + Length;

    public Location Combine(Location other)
    {
        var start = Math.Min(Start, other.Start);
        var length = Math.Max(End, other.End) - start;
        return new Location(start, length);
    }
}
