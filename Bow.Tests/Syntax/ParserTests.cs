using Bow.Compiler.Syntax;

namespace Bow.Tests.Syntax;

public class ParserTests
{
    [Fact]
    public void ParseFunctionDeclaration_WhenEmptyBlock_OnlyParsesFirstNewline()
    {
        var syntaxTree = SyntaxTree.Create(
            "test.bow",
            """
            fun main() {
            }
            """
        );

        var root = syntaxTree.Root;

        Assert.Empty(syntaxTree.Diagnostics);
        Assert.IsType<FunctionDefinitionSyntax>(root.Items[0]);
    }

    [Theory]
    [InlineData(
        """
            fun main()
            {
            }
            """
    )]
    [InlineData(
        """
            fun main() {
                return true }
            """
    )]
    public void ParseFunctionDeclaration_WhenFunctionBody_DisallowAsymmetricBraces(string source)
    {
        var syntaxTree = SyntaxTree.Create("test.bow", source);

        var root = syntaxTree.Root;

        Assert.Single(syntaxTree.Diagnostics);
        Assert.IsType<FunctionDefinitionSyntax>(root.Items[0]);
    }
}
