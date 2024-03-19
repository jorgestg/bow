using System.Collections.Immutable;
using Bow.Compiler;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Tests;

public class CompilationTests
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

        var m = actual[0];
        Assert.Equal("m", m.Name);
        Assert.Equal(2, m.Roots.Length);
        Assert.Single(m.SubModules);

        var a = m.SubModules[0];
        Assert.Equal("a", a.Name);
        Assert.Equal(2, m.SubModules[0].Roots.Length);
    }

    [Fact]
    public void BindModules_WhenNestedModules_AssignsCorrectRoots()
    {
        Compilation compilation =
            new(
                [
                    SyntaxTree.Create("/test/mod1.bow", "mod a.b"),
                    SyntaxTree.Create("/test/mod2.bow", "mod a.b.c")
                ]
            );

        ImmutableArray<ModuleSymbol> actual = compilation.Modules;

        Assert.Single(actual);

        var a = actual[0];
        Assert.Equal("a", a.Name);
        Assert.Empty(a.Roots);
        Assert.Single(a.SubModules);

        var b = a.SubModules[0];
        Assert.Equal("b", b.Name);
        Assert.Single(b.Roots);
        Assert.Single(b.SubModules);

        var c = b.SubModules[0];
        Assert.Equal("c", c.Name);
        Assert.Single(c.Roots);
        Assert.Empty(c.SubModules);
    }
}
