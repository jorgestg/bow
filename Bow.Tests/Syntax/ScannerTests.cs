using Bow.Compiler.Syntax;

namespace Bow.Tests.Syntax;

public class ScannerTests
{
    [Fact]
    public void NextToken_ReturnsSingleNewlineToken_WhenGivenMultipleNewLinesWithWhitespace()
    {
        const string input = "\n   \n x";
        var tree = SyntaxTree.Create("test.bow", input);
        var scanner = tree.Parser.Scanner;

        var newLine = scanner.NextToken();
        var identifier = scanner.NextToken();

        Assert.Equal(TokenKind.NewLine, newLine.Kind);
        Assert.Equal(4, newLine.Location.Start);
        Assert.Equal(TokenKind.Identifier, identifier.Kind);
    }
}
