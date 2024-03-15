using System.Collections.Frozen;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class ModuleBinder : Binder
{
    private readonly ModuleSymbol _module;

    public ModuleBinder(ModuleSymbol module)
        : base((Binder?)module.Container?.Binder ?? module.Compilation.Binder)
    {
        _module = module;
    }

    private ImmutableArray<FileBinder>? _lazyFileBinders;
    private ImmutableArray<FileBinder> FileBinders => _lazyFileBinders ??= CreateFileBinders();

    private FrozenDictionary<string, Symbol>? _lazyMembers;
    private FrozenDictionary<string, Symbol> MembersMap => _lazyMembers ??= CreateMembersMap();

    public override Symbol? LookupMember(string name)
    {
        return MembersMap.GetValueOrDefault(name);
    }

    public override Symbol? Lookup(string name)
    {
        return MembersMap.TryGetValue(name, out var symbol) ? symbol : Parent.Lookup(name);
    }

    public FileBinder GetFileBinder(SyntaxTree syntaxTree)
    {
        foreach (var fileBinder in FileBinders)
        {
            if (fileBinder.SyntaxTree == syntaxTree)
            {
                return fileBinder;
            }
        }

        throw new UnreachableException();
    }

    private ImmutableArray<FileBinder> CreateFileBinders()
    {
        var fileBinders = ImmutableArray.CreateBuilder<FileBinder>(_module.Roots.Length);
        foreach (var root in _module.Roots)
        {
            fileBinders.Add(new FileBinder(_module, root.SyntaxTree));
        }

        return fileBinders.MoveToImmutable();
    }

    private FrozenDictionary<string, Symbol> CreateMembersMap()
    {
        Dictionary<string, Symbol> members = [];
        foreach (var type in _module.Types)
        {
            // File-scoped symbols are bound in FileBinder
            if (type.Accessibility == SymbolAccessibility.File || members.TryAdd(type.Name, type))
            {
                continue;
            }

            var identifier = ((ItemSyntax)type.Syntax).Identifier;
            Diagnostics.AddError(identifier, DiagnosticMessages.NameIsAlreadyDefined, type.Name);
        }

        foreach (var function in _module.Functions)
        {
            if (
                // File-scoped symbols are bound in FileBinder
                function.Accessibility == SymbolAccessibility.File
                || members.TryAdd(function.Name, function)
            )
            {
                continue;
            }

            Diagnostics.AddError(
                function.Syntax.Identifier,
                DiagnosticMessages.NameIsAlreadyDefined,
                function.Name
            );
        }

        return members.ToFrozenDictionary();
    }
}
