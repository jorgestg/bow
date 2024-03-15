using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;

namespace Bow.Compiler.Binding;

internal sealed class StructBinder(StructSymbol @struct) : Binder(GetFileBinder(@struct))
{
    private readonly StructSymbol _struct = @struct;

    private Dictionary<string, Symbol>? _lazyMembers;
    private Dictionary<string, Symbol> Members => _lazyMembers ??= BindMembers();

    public override Symbol? LookupMember(string name)
    {
        return Members.GetValueOrDefault(name);
    }

    private Dictionary<string, Symbol> BindMembers()
    {
        Dictionary<string, Symbol> members = [];
        foreach (var field in _struct.Fields)
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

        foreach (var method in _struct.Methods)
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
