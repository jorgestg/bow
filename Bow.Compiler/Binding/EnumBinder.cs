using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class EnumBinder : Binder
{
    private readonly EnumSymbol _enum;

    public EnumBinder(EnumSymbol @enum)
        : base(GetParentBinder(@enum))
    {
        _enum = @enum;
        Diagnostics = Parent.Diagnostics;
    }

    public override DiagnosticBag Diagnostics { get; }

    private Dictionary<string, Symbol>? _lazyMembers;
    private Dictionary<string, Symbol> MembersMap => _lazyMembers ??= CreateMembersMap();

    public override Symbol? LookupMember(string name)
    {
        return MembersMap.GetValueOrDefault(name);
    }

    private static FileBinder GetParentBinder(EnumSymbol @struct)
    {
        return @struct.Module.Binder.GetFileBinder(@struct.Syntax.SyntaxTree);
    }

    private Dictionary<string, Symbol> CreateMembersMap()
    {
        Dictionary<string, Symbol> members = [];
        foreach (var field in _enum.Cases)
        {
            if (
                // File-scoped symbols are bound in FileBinder
                field.Accessibility == SymbolAccessibility.File
                || members.TryAdd(field.Name, field)
            )
            {
                continue;
            }

            Diagnostics.AddError(
                field.Syntax.Identifier,
                DiagnosticMessages.NameIsAlreadyDefined,
                field.Name
            );
        }

        foreach (var method in _enum.Methods)
        {
            if (
                // File-scoped symbols are bound in FileBinder
                method.Accessibility == SymbolAccessibility.File
                || members.TryAdd(method.Name, method)
            )
            {
                continue;
            }

            Diagnostics.AddError(
                method.Syntax.Identifier,
                DiagnosticMessages.NameIsAlreadyDefined,
                method.Name
            );
        }

        return members;
    }
}
