using System.Text;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public sealed class FunctionTypeSymbol(FunctionSymbol function) : TypeSymbol
{
    private string? _lazyName;
    public override string Name
    {
        get
        {
            if (_lazyName != null)
            {
                return _lazyName;
            }

            StringBuilder builder = new();
            builder.Append("fun");
            builder.Append(' ');
            builder.Append('(');

            var first = true;
            foreach (var parameter in Function.Parameters)
            {
                if (!first)
                {
                    builder.Append(',');
                    builder.Append(' ');
                }

                builder.Append(parameter.Type.Name);
                first = false;
            }

            builder.Append(')');
            builder.Append(' ');
            builder.Append(Function.ReturnType.Name);

            return _lazyName = builder.ToString();
        }
    }

    public override SyntaxNode Syntax => Function.Syntax;
    public override ModuleSymbol Module => Function.Module;

    public FunctionSymbol Function { get; } = function;

    public override bool IsSameType(TypeSymbol other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is not FunctionTypeSymbol otherAsFunction)
        {
            return false;
        }

        if (!Function.ReturnType.IsSameType(otherAsFunction.Function.ReturnType))
        {
            return false;
        }

        if (Function.Parameters.Length != otherAsFunction.Function.Parameters.Length)
        {
            return false;
        }

        var thisParameters = Function.Parameters;
        var otherParameters = otherAsFunction.Function.Parameters;
        for (int i = 0; i < thisParameters.Length; i++)
        {
            if (!thisParameters[i].Type.IsSameType(otherParameters[i].Type))
            {
                return false;
            }
        }

        return true;
    }
}
