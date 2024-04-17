using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public interface IItemSymbol
{
    ItemSyntax Syntax { get; }
    string Name { get; }
    ModuleSymbol Module { get; }
}
