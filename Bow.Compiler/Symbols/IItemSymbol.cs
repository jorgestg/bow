using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

internal interface IItemSymbol
{
    ItemSyntax Syntax { get; }
    ModuleSymbol Module { get; }
}
