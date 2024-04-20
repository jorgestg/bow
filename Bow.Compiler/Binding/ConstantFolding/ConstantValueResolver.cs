using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal static class ConstantValueResolver
{
    public static ConstantValue? Resolve(BoundOperator @operator, BoundExpression operand)
    {
        if (operand.ConstantValue == null)
        {
            return null;
        }

        switch (@operator.Kind)
        {
            case BoundOperatorKind.Negation:
                object value = @operator.OperandType.PrimitiveTypeKind switch
                {
                    PrimitiveTypeKind.Float32 => -(float)operand.ConstantValue.Value,
                    PrimitiveTypeKind.Float64 => -(double)operand.ConstantValue.Value,
                    PrimitiveTypeKind.Signed8 => -(sbyte)operand.ConstantValue.Value,
                    PrimitiveTypeKind.Signed16 => -(short)operand.ConstantValue.Value,
                    PrimitiveTypeKind.Signed32 => -(int)operand.ConstantValue.Value,
                    PrimitiveTypeKind.Signed64 => -(long)operand.ConstantValue.Value,
                    _ => throw new UnreachableException()
                };

                return new ConstantValue(value);

            case BoundOperatorKind.LogicalNegation:
                return new ConstantValue(!(bool)operand.ConstantValue.Value);

            default:
                throw new UnreachableException();
        }
    }

    public static ConstantValue? Resolve(BoundExpression left, BoundOperator @operator, BoundExpression right)
    {
        // 'and' and 'or' do not need both operands to be constants.
        // For 'and', if any of the operands is false, the result is false.
        if (
            @operator.Kind == BoundOperatorKind.LogicalAnd
            && ((bool?)left.ConstantValue?.Value == false || (bool?)right.ConstantValue?.Value == false)
        )
        {
            return new ConstantValue(false);
        }

        // For 'or', if any of the operands is true, the result is true.
        if (
            @operator.Kind == BoundOperatorKind.LogicalOr
            && ((bool?)left.ConstantValue?.Value == true || (bool?)right.ConstantValue?.Value == true)
        )
        {
            return new ConstantValue(true);
        }

        // For every other operator, both operands need to be constants.
        if (left.ConstantValue == null || right.ConstantValue == null)
        {
            return null;
        }
    }
}
