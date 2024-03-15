using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class FileBinder(ModuleSymbol module, SyntaxTree syntaxTree) : Binder(module.Binder)
{
    private readonly ModuleSymbol _module = module;

    public override DiagnosticBag Diagnostics => Parent.Diagnostics;
    public SyntaxTree SyntaxTree { get; } = syntaxTree;

    private Dictionary<string, Symbol>? _lazySymbols;
    private Dictionary<string, Symbol> SymbolMap => _lazySymbols ??= CreateSymbolMap();

    public override Symbol? Lookup(string name)
    {
        return SymbolMap.TryGetValue(name, out var symbol) ? symbol : Parent.Lookup(name);
    }

    public override TypeSymbol BindType(TypeReferenceSyntax syntax)
    {
        if (syntax is KeywordTypeReferenceSyntax keywordType)
        {
            return keywordType.Keyword.Kind switch
            {
                TokenKind.F32 => BuiltInModule.Float32,
                TokenKind.F64 => BuiltInModule.Float64,
                TokenKind.S8 => BuiltInModule.Signed8,
                TokenKind.S16 => BuiltInModule.Signed16,
                TokenKind.S32 => BuiltInModule.Signed32,
                TokenKind.S64 => BuiltInModule.Signed64,
                TokenKind.U8 => BuiltInModule.Unsigned8,
                TokenKind.U16 => BuiltInModule.Unsigned16,
                TokenKind.U32 => BuiltInModule.Unsigned32,
                TokenKind.U64 => BuiltInModule.Unsigned64,
                TokenKind.Unit => BuiltInModule.Unit,
                _ => throw new UnreachableException()
            };
        }

        var namedTypeSyntax = (NamedTypeReferenceSyntax)syntax;
        var symbol = BindName(namedTypeSyntax.Name);
        if (symbol is TypeSymbol typeSymbol)
        {
            return typeSymbol;
        }

        Diagnostics.AddError(
            namedTypeSyntax.Name,
            DiagnosticMessages.NameIsNotAType,
            namedTypeSyntax.Name.ToString()
        );

        return new MissingTypeSymbol(namedTypeSyntax.Name);
    }

    public override Symbol BindName(NameSyntax syntax)
    {
        Symbol? symbol = syntax switch
        {
            SimpleNameSyntax s => BindSimpleName(s),
            QualifiedNameSyntax s => BindQualifiedName(s),
            _ => throw new UnreachableException()
        };

        return symbol ?? new MissingSymbol(syntax);
    }

    private Dictionary<string, Symbol> CreateSymbolMap()
    {
        Dictionary<string, Symbol> symbols = [];

        // Bind file-scoped symbols
        foreach (var type in _module.Types)
        {
            var isAccessible =
                type.Syntax.SyntaxTree == SyntaxTree
                && type.Accessibility == SymbolAccessibility.File;

            if (isAccessible && symbols.TryAdd(type.Name, type))
            {
                continue;
            }

            var identifier = ((ItemSyntax)type.Syntax).Identifier;
            Diagnostics.AddError(identifier, DiagnosticMessages.NameIsAlreadyDefined, type.Name);
        }

        foreach (var function in _module.Functions)
        {
            var isAccessible =
                function.Syntax.SyntaxTree == SyntaxTree
                && function.Accessibility == SymbolAccessibility.File;

            if (isAccessible && symbols.TryAdd(function.Name, function))
            {
                continue;
            }

            Diagnostics.AddError(
                function.Syntax.Identifier,
                DiagnosticMessages.NameIsAlreadyDefined,
                function.Name
            );
        }

        // Import use'd mods
        foreach (var useClause in SyntaxTree.Root.UseClauses)
        {
            var symbol = BindName(useClause.Name);
            if (symbol.IsMissing)
            {
                continue;
            }

            var module = (ModuleSymbol)symbol;
            symbols.TryAdd(module.Name, module);
        }

        return symbols;
    }

    private Symbol? BindSimpleName(SimpleNameSyntax syntax)
    {
        var name = syntax.Identifier.IdentifierText;
        var symbol = Lookup(name);
        if (symbol != null)
        {
            return symbol;
        }

        Diagnostics.AddError(syntax, DiagnosticMessages.NameNotFound, name);
        return new MissingSymbol(syntax);
    }

    private Symbol? BindQualifiedName(QualifiedNameSyntax syntax)
    {
        var name = syntax.Parts[0].IdentifierText;
        var symbol = Lookup(name);
        if (symbol == null)
        {
            Diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameNotFound, name);
            return null;
        }

        if (symbol is not ModuleSymbol module)
        {
            Diagnostics.AddError(syntax.Parts[0], DiagnosticMessages.NameIsNotAModule, name);
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
                Diagnostics.AddError(syntax.Parts[i], DiagnosticMessages.NameIsNotAModule, name);
                return null;
            }

            Diagnostics.AddError(syntax.Parts[i], DiagnosticMessages.NameNotFound, name);
            return null;
        }

        return module.Binder.LookupMember(name);
    }
}
