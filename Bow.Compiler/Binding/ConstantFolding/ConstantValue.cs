namespace Bow.Compiler.Binding;

internal sealed class ConstantValue(object value)
{
    public object Value { get; } = value;
}
