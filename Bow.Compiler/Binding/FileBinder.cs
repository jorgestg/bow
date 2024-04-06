using System.Collections.Frozen;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class FileBinder : Binder
{
    private readonly ModuleSymbol _module;
    private readonly FrozenDictionary<string, Symbol> _symbols;

    private FileBinder(
        ModuleSymbol module,
        SyntaxTree syntaxTree,
        FrozenDictionary<string, Symbol> symbols
    )
        : base(module.Binder)
    {
        _module = module;
        _symbols = symbols;

        SyntaxTree = syntaxTree;
    }

    public SyntaxTree SyntaxTree { get; }

    public static FileBinder CreateAndBindImports(
        ModuleSymbol module,
        SyntaxTree syntaxTree,
        DiagnosticBag diagnostics
    )
    {
        Dictionary<string, Symbol> symbols = [];
        foreach (var useClause in syntaxTree.Root.UseClauses)
        {
            var importedSymbol = module.Binder.BindName(useClause.Name, diagnostics);
            if (importedSymbol.IsMissing)
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
                    SyntaxKind.F32Keyword => BuiltInModule.Float32,
                    SyntaxKind.F64Keyword => BuiltInModule.Float64,
                    SyntaxKind.NeverKeyword => BuiltInModule.Never,
                    SyntaxKind.S8Keyword => BuiltInModule.Signed8,
                    SyntaxKind.S16Keyword => BuiltInModule.Signed16,
                    SyntaxKind.S32Keyword => BuiltInModule.Signed32,
                    SyntaxKind.S64Keyword => BuiltInModule.Signed64,
                    SyntaxKind.UnitKeyword => BuiltInModule.Unit,
                    SyntaxKind.U8Keyword => BuiltInModule.Unsigned8,
                    SyntaxKind.U16Keyword => BuiltInModule.Unsigned16,
                    SyntaxKind.U32Keyword => BuiltInModule.Unsigned32,
                    SyntaxKind.U64Keyword => BuiltInModule.Unsigned64,
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

                diagnostics.AddError(
                    namedType.Name,
                    DiagnosticMessages.NameIsNotAType,
                    namedType.Name.ToString()
                );

                return new MissingTypeSymbol(namedType.Name);
            }

            case SyntaxKind.MissingTypeReference:
            {
                // Diagnostic already reported
                return new MissingTypeSymbol(syntax);
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
            return new MissingSymbol(syntax);
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
        return new MissingSymbol(syntax);
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
                return module.MembersMap.TryGetValue(name, out var member) ? member : null;
        }

        diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameIsNotAPackage, name);
        return null;
    }
}
