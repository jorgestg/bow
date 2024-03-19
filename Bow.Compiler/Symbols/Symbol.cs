using Bow.Compiler.Binding;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public enum SymbolAccessibility
{
    Private,
    File,
    Module,
    Public
}

public abstract class Symbol
{
    public abstract string Name { get; }
    public abstract SyntaxNode Syntax { get; }
    public abstract ModuleSymbol Module { get; }
    internal abstract Binder Binder { get; }
    public virtual SymbolAccessibility Accessibility => SymbolAccessibility.Private;
    public virtual bool IsMissing => false;
}
