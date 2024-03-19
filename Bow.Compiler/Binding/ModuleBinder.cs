using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class ModuleBinder(ModuleSymbol module)
    : Binder((Binder?)module.Previous?.Binder ?? module.Compilation.Binder)
{
    private readonly ModuleSymbol _module = module;

    public override Symbol? LookupMember(string name)
    {
        return _module.MembersMap.GetValueOrDefault(name);
    }

    public override Symbol? Lookup(string name)
    {
        return _module.MembersMap.TryGetValue(name, out var symbol) ? symbol : Parent.Lookup(name);
    }

    public override Symbol BindName(NameSyntax syntax, DiagnosticBag diagnostics)
    {
        Symbol? symbol = syntax switch
        {
            SimpleNameSyntax s => BindSimpleName(s, diagnostics),
            QualifiedNameSyntax s => BindQualifiedName(s, diagnostics),
            _ => throw new UnreachableException()
        };

        return symbol ?? new MissingSymbol(syntax);
    }

    private Symbol? BindSimpleName(SimpleNameSyntax syntax, DiagnosticBag diagnostics)
    {
        var name = syntax.Identifier.IdentifierText;
        var symbol = Lookup(name);
        if (symbol != null)
        {
            return symbol;
        }

        diagnostics.AddError(syntax, DiagnosticMessages.NameNotFound, name);
        return new MissingSymbol(syntax);
    }

    private ModuleSymbol? BindQualifiedName(QualifiedNameSyntax syntax, DiagnosticBag diagnostics)
    {
        var name = syntax.Parts[0].IdentifierText;
        var symbol = Lookup(name);
        if (symbol == null)
        {
            diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameNotFound, name);
            return null;
        }

        if (symbol is not ModuleSymbol module)
        {
            diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameIsNotAModule, name);
            return null;
        }

        for (var i = 1; i < syntax.Parts.Count; i++)
        {
            name = syntax.Parts[i].IdentifierText;
            var subModule = module.SubModules.FindByName(name);
            if (subModule != null)
            {
                module = subModule;
                continue;
            }

            var member = module.Binder.LookupMember(name);
            if (member != null)
            {
                diagnostics.AddError(syntax.Parts[i], DiagnosticMessages.NameIsNotAModule, name);
                return null;
            }

            diagnostics.AddError(syntax.Parts[i], DiagnosticMessages.NameNotFound, name);
            return null;
        }

        return module;
    }
}
