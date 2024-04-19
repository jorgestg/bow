using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class ModuleBinder(ModuleSymbol module) : Binder(module.Package.Binder)
{
    private readonly ModuleSymbol _module = module;

    public override Symbol? LookupMember(string name)
    {
        return (Symbol?)_module.MembersMap.GetValueOrDefault(name);
    }

    public override Symbol? Lookup(string name)
    {
        return _module.MembersMap.TryGetValue(name, out var symbol) ? (Symbol)symbol : Parent.Lookup(name);
    }

    public override Symbol BindName(NameSyntax syntax, DiagnosticBag diagnostics)
    {
        Symbol? symbol = syntax.Kind switch
        {
            SyntaxKind.SimpleName => BindSimpleName((SimpleNameSyntax)syntax, diagnostics),
            SyntaxKind.QualifiedName => BindQualifiedName((QualifiedNameSyntax)syntax, diagnostics),
            _ => throw new UnreachableException()
        };

        return symbol ?? PlaceholderSymbol.Instance;
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
        return PlaceholderSymbol.Instance;
    }

    private ModuleSymbol? BindQualifiedName(QualifiedNameSyntax syntax, DiagnosticBag diagnostics)
    {
        var name = syntax.Parts[0].IdentifierText;
        var left = Lookup(name);
        if (left == null)
        {
            diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameNotFound, name);
            return null;
        }

        if (left is not PackageSymbol package)
        {
            diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameIsNotAPackage, name);
            return null;
        }

        var right = package.Modules.FindByName(syntax.Parts[1].IdentifierText);
        if (syntax.Parts.Count == 2)
        {
            return right;
        }

        diagnostics.AddError(syntax.Parts[2], DiagnosticMessages.NameIsNotAPackage, syntax.Parts[2].IdentifierText);

        return null;
    }
}
