using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal enum BoundOperatorKind
{
    // Unary
    Negation,
    LogicalNegation,

    // Binary
    Multiplication,
    Division,
    Modulo,
    Addition,
    Subtraction,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    Equals,
    NotEquals,
    BitwiseAnd,
    BitwiseOr,
    LogicalAnd,
    LogicalOr
}

internal readonly struct BoundOperator
{
    private BoundOperator(BoundOperatorKind kind, TypeSymbol operandType, TypeSymbol resultType)
    {
        Kind = kind;
        OperandType = operandType;
        ResultType = resultType;
    }

    public BoundOperatorKind Kind { get; }
    public TypeSymbol OperandType { get; }
    public TypeSymbol ResultType { get; }

    public static BoundOperator CreateErrorUnaryOperator(SyntaxKind operatorSyntaxKind)
    {
        var operatorKind = operatorSyntaxKind switch
        {
            SyntaxKind.NotKeyword => BoundOperatorKind.LogicalNegation,
            SyntaxKind.MinusToken => BoundOperatorKind.Negation,
            _ => throw new UnreachableException()
        };

        return new BoundOperator(operatorKind, PlaceholderTypeSymbol.Instance, PlaceholderTypeSymbol.Instance);
    }

    public static BoundOperator CreateErrorBinaryOperator(SyntaxKind operatorSyntaxKind)
    {
        var operatorKind = operatorSyntaxKind switch
        {
            SyntaxKind.StarToken => BoundOperatorKind.Multiplication,
            SyntaxKind.SlashToken => BoundOperatorKind.Division,
            SyntaxKind.PercentToken => BoundOperatorKind.Modulo,
            SyntaxKind.PlusToken => BoundOperatorKind.Addition,
            SyntaxKind.MinusToken => BoundOperatorKind.Subtraction,
            SyntaxKind.GreaterThanToken => BoundOperatorKind.Greater,
            SyntaxKind.GreaterThanEqualToken => BoundOperatorKind.GreaterOrEqual,
            SyntaxKind.LessThanToken => BoundOperatorKind.Less,
            SyntaxKind.LessThanEqualToken => BoundOperatorKind.LessOrEqual,
            SyntaxKind.EqualEqualToken => BoundOperatorKind.Equals,
            SyntaxKind.DiamondToken => BoundOperatorKind.NotEquals,
            SyntaxKind.AmpersandToken => BoundOperatorKind.BitwiseAnd,
            SyntaxKind.PipeToken => BoundOperatorKind.BitwiseOr,
            SyntaxKind.AndKeyword => BoundOperatorKind.LogicalAnd,
            SyntaxKind.OrKeyword => BoundOperatorKind.LogicalOr,
            _ => throw new UnreachableException()
        };

        return new BoundOperator(operatorKind, PlaceholderTypeSymbol.Instance, PlaceholderTypeSymbol.Instance);
    }

    public static bool TryBindUnaryOperator(
        SyntaxKind operatorSyntaxKind,
        TypeSymbol operandType,
        out BoundOperator @operator
    )
    {
        if (operatorSyntaxKind == SyntaxKind.NotKeyword && operandType == BuiltInPackage.BoolType)
        {
            @operator = new BoundOperator(
                BoundOperatorKind.LogicalNegation,
                operandType: BuiltInPackage.BoolType,
                resultType: BuiltInPackage.BoolType
            );

            return true;
        }

        if (
            operatorSyntaxKind == SyntaxKind.MinusToken
            && operandType.IsNumericType()
            && !((PrimitiveTypeSymbol)operandType).IsUnsigned()
        )
        {
            @operator = new BoundOperator(BoundOperatorKind.Negation, operandType, resultType: operandType);
            return true;
        }

        @operator = default;
        return false;
    }

    public static bool TryBindBinaryOperator(
        SyntaxKind operatorSyntaxKind,
        TypeSymbol leftType,
        TypeSymbol rightType,
        out BoundOperator @operator
    )
    {
        var operatorKind = operatorSyntaxKind switch
        {
            SyntaxKind.StarToken => BoundOperatorKind.Multiplication,
            SyntaxKind.SlashToken => BoundOperatorKind.Division,
            SyntaxKind.PercentToken => BoundOperatorKind.Modulo,
            SyntaxKind.PlusToken => BoundOperatorKind.Addition,
            SyntaxKind.MinusToken => BoundOperatorKind.Subtraction,
            SyntaxKind.GreaterThanToken => BoundOperatorKind.Greater,
            SyntaxKind.GreaterThanEqualToken => BoundOperatorKind.GreaterOrEqual,
            SyntaxKind.LessThanToken => BoundOperatorKind.Less,
            SyntaxKind.LessThanEqualToken => BoundOperatorKind.LessOrEqual,
            SyntaxKind.EqualEqualToken => BoundOperatorKind.Equals,
            SyntaxKind.DiamondToken => BoundOperatorKind.NotEquals,
            SyntaxKind.AmpersandToken => BoundOperatorKind.BitwiseAnd,
            SyntaxKind.PipeToken => BoundOperatorKind.BitwiseOr,
            SyntaxKind.AndKeyword => BoundOperatorKind.LogicalAnd,
            SyntaxKind.OrKeyword => BoundOperatorKind.LogicalOr,
            _ => throw new UnreachableException()
        };

        switch (operatorKind)
        {
            case BoundOperatorKind.Multiplication:
            case BoundOperatorKind.Division:
            case BoundOperatorKind.Modulo:
            case BoundOperatorKind.Addition:
            case BoundOperatorKind.Subtraction:
            {
                if (!leftType.TryUnify(rightType, out var unifiedType) || !unifiedType.IsNumericType())
                {
                    break;
                }

                @operator = new BoundOperator(operatorKind, unifiedType, unifiedType);
                return true;
            }

            case BoundOperatorKind.Greater:
            case BoundOperatorKind.GreaterOrEqual:
            case BoundOperatorKind.Less:
            case BoundOperatorKind.LessOrEqual:
            {
                if (!leftType.TryUnify(rightType, out var unifiedType) || !unifiedType.IsNumericType())
                {
                    break;
                }

                @operator = new BoundOperator(operatorKind, unifiedType, BuiltInPackage.BoolType);
                return true;
            }

            case BoundOperatorKind.Equals:
            case BoundOperatorKind.NotEquals:
            {
                if (!leftType.TryUnify(rightType, out var unifiedType))
                {
                    break;
                }

                @operator = new BoundOperator(operatorKind, unifiedType, BuiltInPackage.BoolType);
                return true;
            }

            // `&` and `|` operators are defined for the s32, u32, s64 and u64 types.
            // If any of the operands are of other integral types (s8, u8, s16, u16),
            // their values are promoted to s32 or u32, which is also the result type of an operation.
            case BoundOperatorKind.BitwiseAnd:
            case BoundOperatorKind.BitwiseOr:
            {
                if (!leftType.TryUnify(rightType, out var unifiedType))
                {
                    break;
                }

                unifiedType = unifiedType.PrimitiveTypeKind switch
                {
                    PrimitiveTypeKind.Signed32
                    or PrimitiveTypeKind.Unsigned32
                    or PrimitiveTypeKind.Signed64
                    or PrimitiveTypeKind.Unsigned64
                        => unifiedType,

                    PrimitiveTypeKind.Signed8 or PrimitiveTypeKind.Signed16 => BuiltInPackage.Signed32Type,
                    PrimitiveTypeKind.Unsigned8 or PrimitiveTypeKind.Unsigned16 => BuiltInPackage.Unsigned32Type,
                    _ => PlaceholderTypeSymbol.Instance
                };

                @operator = new BoundOperator(operatorKind, unifiedType, unifiedType);
                return true;
            }

            case BoundOperatorKind.LogicalAnd:
            case BoundOperatorKind.LogicalOr:
            {
                if (leftType != BuiltInPackage.BoolType || rightType != BuiltInPackage.BoolType)
                {
                    break;
                }

                @operator = new BoundOperator(operatorKind, BuiltInPackage.BoolType, BuiltInPackage.BoolType);
                return true;
            }

            default:
                throw new UnreachableException();
        }

        @operator = default;
        return false;
    }
}
