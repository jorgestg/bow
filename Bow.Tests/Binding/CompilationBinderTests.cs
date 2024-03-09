using System.Collections.Immutable;
using Bow.Compiler;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Tests.Binding;

public class CompilationBinderTests
{
    [Fact]
    public void BindModules_MergesRoots()
    {
        SyntaxTree root1 =
            new(
                new SourceText("/test/mod1.bow", "mod m"),
                f =>
                    f.CompilationUnit(
                        f.ModClause(f.Identifier(0, 3), f.SimpleName(f.Identifier(4, 1))),
                        f.SyntaxList<UseClauseSyntax>(),
                        f.SyntaxList<ItemSyntax>()
                    )
            );

        SyntaxTree root2 =
            new(
                new SourceText("/test/mod2.bow", "mod m"),
                f =>
                    f.CompilationUnit(
                        f.ModClause(f.Identifier(0, 3), f.SimpleName(f.Identifier(4, 1))),
                        f.SyntaxList<UseClauseSyntax>(),
                        f.SyntaxList<ItemSyntax>()
                    )
            );

        SyntaxTree root3 =
            new(
                new SourceText("/test/mod3.bow", "mod m.a"),
                f =>
                    f.CompilationUnit(
                        f.ModClause(
                            f.Identifier(0, 3),
                            f.QualifiedName(f.SyntaxList(f.Identifier(4, 1), f.Identifier(6, 1)))
                        ),
                        f.SyntaxList<UseClauseSyntax>(),
                        f.SyntaxList<ItemSyntax>()
                    )
            );

        SyntaxTree root4 =
            new(
                new SourceText("/test/mod4.bow", "mod m.a"),
                f =>
                    f.CompilationUnit(
                        f.ModClause(
                            f.Identifier(0, 3),
                            f.QualifiedName(f.SyntaxList(f.Identifier(4, 1), f.Identifier(6, 1)))
                        ),
                        f.SyntaxList<UseClauseSyntax>(),
                        f.SyntaxList<ItemSyntax>()
                    )
            );

        Compilation compilation = new([root1, root2, root3, root4]);

        ImmutableArray<ModuleSymbol> actual = compilation.Modules;

        Assert.Single(actual);
        Assert.Equal("m", actual[0].Name);
        Assert.Equal(2, actual[0].Roots.Length);
        Assert.Single(actual[0].SubModules);
        Assert.Equal("a", actual[0].SubModules[0].Name);
        Assert.Equal(2, actual[0].SubModules[0].Roots.Length);
    }
}
