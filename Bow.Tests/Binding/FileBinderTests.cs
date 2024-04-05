using Bow.Compiler.Binding;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Tests.Binding;

public class FileBinderTests
{
    [Fact]
    public void BindType_WhenInaccessibleSymbol_ReportsDiagnostic()
    {
        // Arrange
        var file1 = SyntaxTree.Create(
            "mod1.bow",
            """
            mod points

            struct Point {
                x s32
                y s32
            }
            """
        );

        var file2 = SyntaxTree.Create(
            "mod2.bow",
            """
            mod points

            fun foo() Point {
            }
            """
        );

        PackageSymbol package = new("main", [file1, file2]);

        // Act
        ModuleSymbol points = package.Modules.FindByName("points")!;
        TypeSymbol point = points.Types.FindByName("Point")!;
        FunctionItemSymbol foo = points.Functions.FindByName("foo")!;

        // Assert
        Assert.Same(point, foo.ReturnType);
        Assert.Single(foo.Diagnostics);
    }

    [Fact]
    public void BindType_WhenUsingModule_BindsType()
    {
        // Arrange
        var file1 = SyntaxTree.Create(
            "points.bow",
            """
            mod points

            pub struct Point {
                x s32
                y s32
            }
            """
        );

        var file2 = SyntaxTree.Create(
            "main.bow",
            """
            use points

            fun foo() points.Point {
            }
            """
        );

        PackageSymbol package = new("main", [file1, file2]);

        // Act
        ModuleSymbol main = package.Modules.FindByName("main")!;
        FunctionItemSymbol foo = main.Functions.FindByName("foo")!;

        ModuleSymbol points = package.Modules.FindByName("points")!;
        TypeSymbol point = points.Types.FindByName("Point")!;

        // Assert
        Assert.Same(point, foo.ReturnType);
    }
}
