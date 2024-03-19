using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal abstract class Binder(Binder parent)
{
    public Binder Parent { get; } = parent;

    public static FileBinder GetFileBinder(IItemSymbol item)
    {
        return item.Module.GetFileBinder(item.Syntax.SyntaxTree);
    }

    public virtual Symbol? Lookup(string name)
    {
        return Parent.Lookup(name);
    }

    public virtual Symbol? LookupMember(string name)
    {
        return null;
    }

    public virtual Symbol BindName(NameSyntax syntax, DiagnosticBag diagnostics)
    {
        return Parent.BindName(syntax, diagnostics);
    }

    public virtual TypeSymbol BindType(TypeReferenceSyntax syntax, DiagnosticBag diagnostics)
    {
        return Parent.BindType(syntax, diagnostics);
    }
}
