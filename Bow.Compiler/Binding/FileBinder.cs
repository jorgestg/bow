using System.Collections.Frozen;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class FileBinder(
    ModuleSymbol module,
    SyntaxTree syntaxTree,
    DiagnosticBag diagnostics
) : Binder(module.Binder)
{
    private readonly ModuleSymbol _module = module;
    private readonly DiagnosticBag _diagnostics = diagnostics;

    public SyntaxTree SyntaxTree { get; } = syntaxTree;

    private FrozenDictionary<string, Symbol>? _lazyScope;
    private FrozenDictionary<string, Symbol> Scope => _lazyScope ??= CreateScope();

    public override Symbol? Lookup(string name)
    {
        return Scope.TryGetValue(name, out var symbol) ? symbol : Parent.Lookup(name);
    }

    public override TypeSymbol BindType(TypeReferenceSyntax syntax, DiagnosticBag diagnostics)
    {
        switch (syntax)
        {
            case KeywordTypeReferenceSyntax keywordType:
            {
                return keywordType.Keyword.Kind switch
                {
                    TokenKind.F32 => BuiltInModule.Float32,
                    TokenKind.F64 => BuiltInModule.Float64,
                    TokenKind.Never => BuiltInModule.Never,
                    TokenKind.S8 => BuiltInModule.Signed8,
                    TokenKind.S16 => BuiltInModule.Signed16,
                    TokenKind.S32 => BuiltInModule.Signed32,
                    TokenKind.S64 => BuiltInModule.Signed64,
                    TokenKind.Unit => BuiltInModule.Unit,
                    TokenKind.U8 => BuiltInModule.Unsigned8,
                    TokenKind.U16 => BuiltInModule.Unsigned16,
                    TokenKind.U32 => BuiltInModule.Unsigned32,
                    TokenKind.U64 => BuiltInModule.Unsigned64,
                    _ => throw new UnreachableException()
                };
            }

            case PointerTypeReferenceSyntax pointerType:
            {
                var innerType = BindType(pointerType.Type, diagnostics);
                return new PointerTypeSymbol(pointerType, innerType);
            }

            case NamedTypeReferenceSyntax namedType:
            {
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

            case MissingTypeReferenceSyntax missingType:
            {
                // Diagnostic already reported
                return new MissingTypeSymbol(missingType);
            }
        }

        throw new UnreachableException();
    }

    public override Symbol BindName(NameSyntax syntax, DiagnosticBag diagnostics)
    {
        Symbol? symbol = syntax switch
        {
            SimpleNameSyntax s => BindSimpleName(s, diagnostics),
            QualifiedNameSyntax s => BindQualifiedName(s, diagnostics),
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
            SymbolAccessibility.Module => _module.RootModule == symbol.Module.RootModule,
            SymbolAccessibility.File => SyntaxTree == symbol.Syntax.SyntaxTree,
            _ => false
        };
    }

    private FrozenDictionary<string, Symbol> CreateScope()
    {
        Dictionary<string, Symbol> symbols = [];
        foreach (var useClause in SyntaxTree.Root.UseClauses)
        {
            var symbol = base.BindName(useClause.Name, _diagnostics);
            if (symbol.IsMissing)
            {
                continue;
            }

            var module = (ModuleSymbol)symbol;
            symbols.TryAdd(module.Name, module);
        }

        return symbols.ToFrozenDictionary();
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

        if (symbol is not ModuleSymbol module)
        {
            diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameIsNotAModule, name);
            return null;
        }

        for (var i = 1; i < syntax.Parts.Count - 1; i++)
        {
            name = syntax.Parts[i].IdentifierText;
            var subModule = module.SubModules.FindByName(name);
            if (subModule != null)
            {
                module = subModule;
                continue;
            }

            var member = module.Binder.LookupMember(name);
            if (member != null)
            {
                diagnostics.AddError(syntax.Parts[i], DiagnosticMessages.NameIsNotAModule, name);
                return null;
            }

            diagnostics.AddError(syntax.Parts[i], DiagnosticMessages.NameNotFound, name);
            return null;
        }

        name = syntax.Parts[^1].IdentifierText;
        return module.Binder.LookupMember(name);
    }
}
