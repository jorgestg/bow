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

    public static BoundOperator UnaryOperatorFor(SyntaxKind operatorSyntaxKind, TypeSymbol operandType)
    {
        BoundOperatorKind kind;
        TypeSymbol resultType;
        if (operatorSyntaxKind == SyntaxKind.NotKeyword)
        {
            kind = BoundOperatorKind.LogicalNegation;
            resultType =
                operandType == BuiltInPackage.BoolType ? BuiltInPackage.BoolType : PlaceholderTypeSymbol.UnknownType;
        }
        else
        {
            Debug.Assert(operatorSyntaxKind == SyntaxKind.MinusToken);

            kind = BoundOperatorKind.Negation;
            resultType =
                operandType.IsNumericType() && !((PrimitiveTypeSymbol)operandType).IsUnsigned()
                    ? operandType
                    : PlaceholderTypeSymbol.UnknownType;
        }

        return new BoundOperator(kind, operandType: resultType, resultType);
    }

    public static BoundOperator BinaryOperatorFor(
        SyntaxKind operatorSyntaxKind,
        TypeSymbol leftType,
        TypeSymbol rightType
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

        TypeSymbol operandType = PlaceholderTypeSymbol.UnknownType;
        TypeSymbol resultType = PlaceholderTypeSymbol.UnknownType;
        switch (operatorKind)
        {
            case BoundOperatorKind.Multiplication:
            case BoundOperatorKind.Division:
            case BoundOperatorKind.Modulo:
            case BoundOperatorKind.Addition:
            case BoundOperatorKind.Subtraction:
            {
                if (leftType.TryUnify(rightType, out var unifiedType) && unifiedType.IsNumericType())
                {
                    operandType = unifiedType;
                    resultType = unifiedType;
                }

                break;
            }

            case BoundOperatorKind.Greater:
            case BoundOperatorKind.GreaterOrEqual:
            case BoundOperatorKind.Less:
            case BoundOperatorKind.LessOrEqual:
            {
                if (leftType.TryUnify(rightType, out var unifiedType) && unifiedType.IsNumericType())
                {
                    operandType = unifiedType;
                    resultType = BuiltInPackage.BoolType;
                }

                break;
            }

            case BoundOperatorKind.Equals:
            case BoundOperatorKind.NotEquals:
            {
                if (leftType.TryUnify(rightType, out var unifiedType))
                {
                    operandType = unifiedType;
                    resultType = BuiltInPackage.BoolType;
                }

                break;
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
                    _ => PlaceholderTypeSymbol.UnknownType
                };

                operandType = unifiedType;
                resultType = unifiedType;
                break;
            }

            case BoundOperatorKind.LogicalAnd:
            case BoundOperatorKind.LogicalOr:
            {
                if (leftType == BuiltInPackage.BoolType && rightType == BuiltInPackage.BoolType)
                {
                    operandType = BuiltInPackage.BoolType;
                    resultType = BuiltInPackage.BoolType;
                }

                break;
            }

            default:
                throw new UnreachableException();
        }

        return new BoundOperator(operatorKind, operandType, resultType);
    }
}
