using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class EnumBinder(EnumSymbol @enum) : Binder(GetFileBinder(@enum))
{
    private readonly EnumSymbol _enum = @enum;

    public override Symbol? LookupMember(string name)
    {
        return _enum.MemberMap.GetValueOrDefault(name);
    }
}
