using Bow.Compiler.Binding;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Tests.Binding;

public class BoundOperatorTests
{
    public static readonly object[][] UnaryOperatorTestCases =
    [
        [
            // -s32 => s32
            SyntaxKind.MinusToken,
            BuiltInPackage.Signed32Type,
            true,
            BoundOperatorKind.Negation,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type
        ],
        [
            // -f32 => f32
            SyntaxKind.MinusToken,
            BuiltInPackage.Float32Type,
            true,
            BoundOperatorKind.Negation,
            BuiltInPackage.Float32Type,
            BuiltInPackage.Float32Type
        ],
        [
            // -unit => ???
            SyntaxKind.MinusToken,
            BuiltInPackage.UnitType,
            false,
            BoundOperatorKind.Negation,
            PlaceholderTypeSymbol.Instance,
            PlaceholderTypeSymbol.Instance
        ],
        [
            // not bool => bool
            SyntaxKind.NotKeyword,
            BuiltInPackage.BoolType,
            true,
            BoundOperatorKind.LogicalNegation,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType
        ],
        [
            // not s32 => ???
            SyntaxKind.NotKeyword,
            BuiltInPackage.Signed32Type,
            false,
            BoundOperatorKind.LogicalNegation,
            PlaceholderTypeSymbol.Instance,
            PlaceholderTypeSymbol.Instance
        ],
    ];

    [Theory]
    [MemberData(nameof(UnaryOperatorTestCases))]
    internal void UnaryOperatorFor_ReturnsCorrectTypes(
        SyntaxKind syntaxKind,
        TypeSymbol operandType,
        bool expectedResult,
        BoundOperatorKind expectedOperatorKind,
        TypeSymbol expectedOperandType,
        TypeSymbol expectedResultType
    )
    {
        var result = BoundOperator.TryBindUnaryOperator(syntaxKind, operandType, out var @operator);

        Assert.Equal(expectedResult, result);
        if (result)
        {
            Assert.Equal(expectedOperatorKind, @operator.Kind);
            Assert.Equal(expectedOperandType, @operator.OperandType);
            Assert.Equal(expectedResultType, @operator.ResultType);
        }
    }

    public static readonly object[][] BinaryOperatorTestCases =
    [
        [
            // s16 * s32 => s32
            SyntaxKind.StarToken,
            BuiltInPackage.Signed16Type,
            BuiltInPackage.Signed32Type,
            true,
            BoundOperatorKind.Multiplication,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type
        ],
        [
            // s32 > s32 => bool
            SyntaxKind.GreaterThanToken,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type,
            true,
            BoundOperatorKind.Greater,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.BoolType
        ],
        [
            // unit == never => ???
            SyntaxKind.EqualEqualToken,
            BuiltInPackage.UnitType,
            BuiltInPackage.NeverType,
            false,
            BoundOperatorKind.Equals,
            PlaceholderTypeSymbol.Instance,
            PlaceholderTypeSymbol.Instance
        ],
        [
            // s32 & s16 => s32
            SyntaxKind.AmpersandToken,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed16Type,
            true,
            BoundOperatorKind.BitwiseAnd,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type,
        ],
        [
            // u32 & s32 => ???
            SyntaxKind.AmpersandToken,
            BuiltInPackage.Unsigned32Type,
            BuiltInPackage.Signed32Type,
            false,
            BoundOperatorKind.BitwiseAnd,
            PlaceholderTypeSymbol.Instance,
            PlaceholderTypeSymbol.Instance
        ],
        [
            // bool and bool => bool
            SyntaxKind.AndKeyword,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType,
            true,
            BoundOperatorKind.LogicalAnd,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType
        ],
        [
            // bool and unit => ???
            SyntaxKind.AndKeyword,
            BuiltInPackage.BoolType,
            BuiltInPackage.UnitType,
            false,
            BoundOperatorKind.LogicalAnd,
            PlaceholderTypeSymbol.Instance,
            PlaceholderTypeSymbol.Instance
        ],
    ];

    [Theory]
    [MemberData(nameof(BinaryOperatorTestCases))]
    internal void TryBindBinaryOperator_ReturnsCorrectTypes(
        SyntaxKind syntaxKind,
        TypeSymbol leftType,
        TypeSymbol rightType,
        bool expectedResult,
        BoundOperatorKind expectedOperatorKind,
        TypeSymbol expectedOperandType,
        TypeSymbol expectedResultType
    )
    {
        var result = BoundOperator.TryBindBinaryOperator(syntaxKind, leftType, rightType, out var @operator);

        Assert.Equal(expectedResult, result);
        if (result)
        {
            Assert.Equal(expectedOperatorKind, @operator.Kind);
            Assert.Equal(expectedOperandType, @operator.OperandType);
            Assert.Equal(expectedResultType, @operator.ResultType);
        }
    }
}
