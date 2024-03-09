using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal abstract class Binder(Binder parent)
{
    public Binder Parent { get; } = parent;

    public abstract DiagnosticBag Diagnostics { get; }

    public virtual Symbol? Lookup(string name)
    {
        return Parent.Lookup(name);
    }

    public virtual Symbol? LookupMember(string name)
    {
        return null;
    }

    public virtual Symbol BindName(NameSyntax syntax)
    {
        return Parent.BindName(syntax);
    }

    public virtual TypeSymbol BindType(TypeReferenceSyntax syntax)
    {
        return Parent.BindType(syntax);
    }
}
