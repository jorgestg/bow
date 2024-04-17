using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

public interface IMemberSymbol
{
    MemberDeclarationSyntax Syntax { get; }
    string Name { get; }
}
