using System.Collections.Immutable;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Tests.Symbols;

public class CompilationTests
{
    [Fact]
    public void BindModules_WhenMultipleRoots_CreatesSingleModule()
    {
        PackageSymbol package =
            new(
                "test",
                [
                    SyntaxTree.Create("/test/mod1.bow", "mod m"),
                    SyntaxTree.Create("/test/mod2.bow", "mod m"),
                ]
            );

        ImmutableArray<ModuleSymbol> actual = package.Modules;

        Assert.Single(actual);

        var m = actual[0];
        Assert.Equal("m", m.Name);
        Assert.Equal(2, m.Roots.Length);
    }
}
