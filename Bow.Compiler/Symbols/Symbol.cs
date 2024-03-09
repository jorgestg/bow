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
    internal abstract Binder Binder { get; }
    public virtual SymbolAccessibility Accessibility => SymbolAccessibility.Private;
    public virtual bool IsMissing => false;
}

public sealed class MissingSymbol(SyntaxNode syntax) : Symbol
{
    public override string Name => "???";
    internal override Binder Binder => throw new InvalidOperationException();
    public override SyntaxNode Syntax { get; } = syntax;
    public override bool IsMissing => true;
}
