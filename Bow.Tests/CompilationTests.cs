using System.Collections.Immutable;
using Bow.Compiler;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Tests.Binding;

public class CompilationBinderTests
{
    [Fact]
    public void BindModules_WhenMultipleRoots_CreatesSingleModule()
    {
        Compilation compilation =
            new(
                [
                    SyntaxTree.Create("/test/mod1.bow", "mod m"),
                    SyntaxTree.Create("/test/mod2.bow", "mod m"),
                    SyntaxTree.Create("/test/mod3.bow", "mod m.a"),
                    SyntaxTree.Create("/test/mod4.bow", "mod m.a")
                ]
            );

        ImmutableArray<ModuleSymbol> actual = compilation.Modules;

        Assert.Single(actual);
        Assert.Equal("m", actual[0].Name);
        Assert.Equal(2, actual[0].Roots.Length);
        Assert.Single(actual[0].SubModules);
        Assert.Equal("a", actual[0].SubModules[0].Name);
        Assert.Equal(2, actual[0].SubModules[0].Roots.Length);
    }
}
