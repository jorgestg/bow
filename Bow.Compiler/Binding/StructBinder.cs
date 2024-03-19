using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class StructBinder(StructSymbol @struct) : Binder(GetFileBinder(@struct))
{
    private readonly StructSymbol _struct = @struct;

    public override Symbol? LookupMember(string name)
    {
        return _struct.MemberMap.GetValueOrDefault(name);
    }
}
