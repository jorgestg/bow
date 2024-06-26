using System.Collections.Frozen;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class FileBinder : Binder
{
    private readonly ModuleSymbol _module;
    private readonly FrozenDictionary<string, Symbol> _symbols;

    private FileBinder(ModuleSymbol module, SyntaxTree syntaxTree, FrozenDictionary<string, Symbol> symbols)
        : base(module.Binder)
    {
        _module = module;
        _symbols = symbols;

        SyntaxTree = syntaxTree;
    }

    public SyntaxTree SyntaxTree { get; }

    public static FileBinder CreateAndBindImports(ModuleSymbol module, SyntaxTree syntaxTree, DiagnosticBag diagnostics)
    {
        Dictionary<string, Symbol> symbols = [];
        foreach (var useClause in syntaxTree.Root.UseClauses)
        {
            var importedSymbol = module.Binder.BindName(useClause.Name, diagnostics);
            if (importedSymbol.IsPlaceholder)
            {
                continue;
            }

            symbols.TryAdd(importedSymbol.Name, importedSymbol);
        }

        return new FileBinder(module, syntaxTree, symbols.ToFrozenDictionary());
    }

    public override Symbol? Lookup(string name)
    {
        return _symbols.TryGetValue(name, out var symbol) ? symbol : Parent.Lookup(name);
    }

    public override TypeSymbol BindType(TypeReferenceSyntax syntax, DiagnosticBag diagnostics)
    {
        switch (syntax.Kind)
        {
            case SyntaxKind.KeywordTypeReference:
            {
                return ((KeywordTypeReferenceSyntax)syntax).Keyword.Kind switch
                {
                    SyntaxKind.F32Keyword => BuiltInPackage.Float32Type,
                    SyntaxKind.F64Keyword => BuiltInPackage.Float64Type,
                    SyntaxKind.NeverKeyword => BuiltInPackage.NeverType,
                    SyntaxKind.S8Keyword => BuiltInPackage.Signed8Type,
                    SyntaxKind.S16Keyword => BuiltInPackage.Signed16Type,
                    SyntaxKind.S32Keyword => BuiltInPackage.Signed32Type,
                    SyntaxKind.S64Keyword => BuiltInPackage.Signed64Type,
                    SyntaxKind.UnitKeyword => BuiltInPackage.UnitType,
                    SyntaxKind.U8Keyword => BuiltInPackage.Unsigned8Type,
                    SyntaxKind.U16Keyword => BuiltInPackage.Unsigned16Type,
                    SyntaxKind.U32Keyword => BuiltInPackage.Unsigned32Type,
                    SyntaxKind.U64Keyword => BuiltInPackage.Unsigned64Type,
                    _ => throw new UnreachableException()
                };
            }

            case SyntaxKind.PointerTypeReference:
            {
                var pointerType = (PointerTypeReferenceSyntax)syntax;
                var innerType = BindType(pointerType.Type, diagnostics);
                return new PointerTypeSymbol(pointerType, innerType);
            }

            case SyntaxKind.NamedTypeReference:
            {
                var namedType = (NamedTypeReferenceSyntax)syntax;
                var symbol = BindName(namedType.Name, diagnostics);
                if (symbol is TypeSymbol typeSymbol)
                {
                    return typeSymbol;
                }

                diagnostics.AddError(namedType.Name, DiagnosticMessages.NameIsNotAType, namedType.Name.ToString());
                return PlaceholderTypeSymbol.Instance;
            }

            case SyntaxKind.MissingTypeReference:
            {
                // Diagnostic already reported
                return PlaceholderTypeSymbol.Instance;
            }
        }

        throw new UnreachableException();
    }

    public override Symbol BindName(NameSyntax syntax, DiagnosticBag diagnostics)
    {
        Symbol? symbol = syntax.Kind switch
        {
            SyntaxKind.SimpleName => BindSimpleName((SimpleNameSyntax)syntax, diagnostics),
            SyntaxKind.QualifiedName => BindQualifiedName((QualifiedNameSyntax)syntax, diagnostics),
            _ => throw new UnreachableException()
        };

        if (symbol == null)
        {
            return PlaceholderSymbol.Instance;
        }

        if (!IsAccessible(symbol))
        {
            diagnostics.AddError(syntax, DiagnosticMessages.SymbolIsNotAccessible, symbol.Name);
        }

        return symbol;
    }

    private bool IsAccessible(Symbol symbol)
    {
        return symbol.Accessibility switch
        {
            SymbolAccessibility.Public => true,
            SymbolAccessibility.Module => _module == symbol.Module,
            SymbolAccessibility.File => SyntaxTree == symbol.Syntax.SyntaxTree,
            _ => false
        };
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

    private Symbol? BindQualifiedName(QualifiedNameSyntax syntax, DiagnosticBag diagnostics)
    {
        var name = syntax.Parts[0].IdentifierText;
        var symbol = Lookup(name);
        if (symbol == null)
        {
            diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameNotFound, name);
            return null;
        }

        name = syntax.Parts[1].IdentifierText;
        switch (symbol)
        {
            case PackageSymbol package:
                return package.Modules.FindByName(name);
            case ModuleSymbol module:
                return module.MembersMap.TryGetValue(name, out var member) ? (Symbol)member : null;
        }

        diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameIsNotAPackage, name);
        return null;
    }
}
