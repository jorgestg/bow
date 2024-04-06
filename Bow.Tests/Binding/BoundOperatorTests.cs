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
            BoundOperatorKind.Negation,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type
        ],
        [
            // -f32 => f32
            SyntaxKind.MinusToken,
            BoundOperatorKind.Negation,
            BuiltInPackage.Float32Type,
            BuiltInPackage.Float32Type,
            BuiltInPackage.Float32Type
        ],
        [
            // -unit => ???
            SyntaxKind.MinusToken,
            BoundOperatorKind.Negation,
            BuiltInPackage.UnitType,
            MissingTypeSymbol.Instance,
            MissingTypeSymbol.Instance,
        ],
        [
            // not bool => bool
            SyntaxKind.NotKeyword,
            BoundOperatorKind.LogicalNegation,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType
        ],
        [
            // not s32 => ???
            SyntaxKind.NotKeyword,
            BoundOperatorKind.LogicalNegation,
            BuiltInPackage.Signed32Type,
            MissingTypeSymbol.Instance,
            MissingTypeSymbol.Instance,
        ],
    ];

    [Theory]
    [MemberData(nameof(UnaryOperatorTestCases))]
    internal void UnaryOperatorFor_ReturnsCorrectTypes(
        SyntaxKind syntaxKind,
        BoundOperatorKind expectedOperatorKind,
        TypeSymbol operandType,
        TypeSymbol expectedOperandType,
        TypeSymbol expectedResultType
    )
    {
        var @operator = BoundOperator.UnaryOperatorFor(syntaxKind, operandType);

        Assert.Equal(expectedOperatorKind, @operator.Kind);
        Assert.Equal(expectedOperandType, @operator.OperandType);
        Assert.Equal(expectedResultType, @operator.ResultType);
    }

    public static readonly object[][] BinaryOperatorTestCases =
    [
        [
            // s16 * s32 => s32
            SyntaxKind.StarToken,
            BoundOperatorKind.Multiplication,
            BuiltInPackage.Signed16Type,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type
        ],
        [
            // s32 > s32 => bool
            SyntaxKind.GreaterThanToken,
            BoundOperatorKind.Greater,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.BoolType
        ],
        [
            // unit == never => ???
            SyntaxKind.EqualEqualToken,
            BoundOperatorKind.Equals,
            BuiltInPackage.UnitType,
            BuiltInPackage.NeverType,
            MissingTypeSymbol.Instance,
            MissingTypeSymbol.Instance
        ],
        [
            // s32 & s16 => s32
            SyntaxKind.AmpersandToken,
            BoundOperatorKind.BitwiseAnd,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed16Type,
            BuiltInPackage.Signed32Type,
            BuiltInPackage.Signed32Type,
        ],
        [
            // u32 & s32 => ???
            SyntaxKind.AmpersandToken,
            BoundOperatorKind.BitwiseAnd,
            BuiltInPackage.Unsigned32Type,
            BuiltInPackage.Signed32Type,
            MissingTypeSymbol.Instance,
            MissingTypeSymbol.Instance
        ],
        [
            // bool and bool => bool
            SyntaxKind.AndKeyword,
            BoundOperatorKind.LogicalAnd,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType,
            BuiltInPackage.BoolType
        ],
        [
            // bool and unit => ???
            SyntaxKind.AndKeyword,
            BoundOperatorKind.LogicalAnd,
            BuiltInPackage.BoolType,
            BuiltInPackage.UnitType,
            MissingTypeSymbol.Instance,
            MissingTypeSymbol.Instance
        ],
    ];

    [Theory]
    [MemberData(nameof(BinaryOperatorTestCases))]
    internal void BinaryOperatorFor_ReturnsCorrectTypes(
        SyntaxKind syntaxKind,
        BoundOperatorKind expectedOperatorKind,
        TypeSymbol leftType,
        TypeSymbol rightType,
        TypeSymbol expectedOperandType,
        TypeSymbol expectedResultType
    )
    {
        var @operator = BoundOperator.BinaryOperatorFor(syntaxKind, leftType, rightType);

        Assert.Equal(expectedOperatorKind, @operator.Kind);
        Assert.Equal(expectedOperandType, @operator.OperandType);
        Assert.Equal(expectedResultType, @operator.ResultType);
    }
}
