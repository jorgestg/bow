using Bow.Compiler.Syntax;

namespace Bow.Tests.Syntax;

public class ScannerTests
{
    [Fact]
    public void NextToken_GivenMultipleNewLinesWithWhitespace_ReturnsSingleNewlineToken()
    {
        const string input = "\n   \n x";
        var tree = SyntaxTree.Create("test.bow", input);
        var scanner = tree.Parser.Scanner;

        var newLine = scanner.NextToken();
        var identifier = scanner.NextToken();

        Assert.Equal(SyntaxKind.NewLineToken, newLine.Kind);
        Assert.Equal(4, newLine.Location.Start);
        Assert.Equal(SyntaxKind.IdentifierToken, identifier.Kind);
    }

    [Fact]
    public void NextToken_GivenUnderscoreSeparatedNumber_ReturnsNumberToken()
    {
        const string input = "1_000_000_";
        var tree = SyntaxTree.Create("test.bow", input);
        var scanner = tree.Parser.Scanner;

        var number = scanner.NextToken();

        Assert.Equal(SyntaxKind.IntegerLiteral, number.Kind);
        Assert.Equal("1_000_000", number.ToString());
    }
}
