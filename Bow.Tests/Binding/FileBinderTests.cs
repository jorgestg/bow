using Bow.Compiler;
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

            file struct Point {
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

        Compilation compilation = new([file1, file2]);

        // Act
        ModuleSymbol points = compilation.Modules.FindByName("points")!;
        TypeSymbol point = points.Types.FindByName("Point")!;
        FunctionItemSymbol doSomethingWithPoint = points.Functions.FindByName("foo")!;

        // Assert
        Assert.Same(point, doSomethingWithPoint.ReturnType);
        Assert.Single(doSomethingWithPoint.Diagnostics);
    }

    [Fact]
    public void BindType_WhenUsingModule_BindsType()
    {
        // Arrange
        var file1 = SyntaxTree.Create(
            "mod1.bow",
            """
            mod math.points

            pub struct Point {
                x s32
                y s32
            }
            """
        );

        var file2 = SyntaxTree.Create(
            "mod2.bow",
            """
            use math.points

            fun foo() points.Point {
            }
            """
        );

        Compilation compilation = new([file1, file2]);

        // Act
        ModuleSymbol main = compilation.Modules.FindByName("main")!;
        FunctionItemSymbol doSomethingWithPoint = main.Functions.FindByName("foo")!;

        ModuleSymbol math = compilation.Modules.FindByName("math")!;
        ModuleSymbol points = math.SubModules.FindByName("points")!;
        TypeSymbol point = points.Types.FindByName("Point")!;

        // Assert
        Assert.Same(point, doSomethingWithPoint.ReturnType);
    }
}
