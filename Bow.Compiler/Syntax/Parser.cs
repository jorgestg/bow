using Bow.Compiler.Diagnostics;

namespace Bow.Compiler.Syntax;

internal sealed class Parser
{
    private readonly SyntaxFactory _syntaxFactory;
    private readonly DiagnosticBag _diagnostics;

    private readonly Scanner _scanner;
    private Token _previous;
    private Token _current;
    private Token _next;

    public Parser(SyntaxFactory syntaxFactory)
    {
        _syntaxFactory = syntaxFactory;
        _scanner = new Scanner(syntaxFactory);
        _diagnostics = new DiagnosticBag();

        _previous = null!;
        _current = _scanner.NextToken();
        _next = _scanner.NextToken();
    }

    public DiagnosticBagView Diagnostics => _diagnostics.AsView();

    // compilation-unit = mod-clause? use-clause* item+
    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var modClause = _current.Kind == TokenKind.Mod ? ParseModClause() : null;
        var useClauses = ParseUseClauses();
        return _syntaxFactory.CompilationUnit(modClause, useClauses, ParseItems());
    }

    // mod-clause = 'mod' name NL
    private ModClauseSyntax ParseModClause()
    {
        var modKeyword = Match(TokenKind.Mod);
        var name = ParseName();
        MatchNewLine();
        return _syntaxFactory.ModClause(modKeyword, name);
    }

    private SyntaxList<UseClauseSyntax> ParseUseClauses()
    {
        var useClauses = _syntaxFactory.SyntaxListBuilder<UseClauseSyntax>();
        while (_current.Kind == TokenKind.Use)
        {
            var useClause = ParseUseClause();
            useClauses.Add(useClause);
        }

        return useClauses.ToSyntaxList();
    }

    // use-clause = 'use' name NL
    private UseClauseSyntax ParseUseClause()
    {
        var useKeyword = Match(TokenKind.Use);
        var name = ParseName();
        MatchNewLine();
        return _syntaxFactory.UseClause(useKeyword, name);
    }

    // name = ID | qualified-name
    // qualified-name = ID ('.' ID)+
    private NameSyntax ParseName()
    {
        var identifierToken = MatchIdentifier();
        if (_current.Kind != TokenKind.Dot)
        {
            return _syntaxFactory.SimpleName(identifierToken);
        }

        var parts = _syntaxFactory.SyntaxListBuilder<IdentifierToken>();
        parts.Add(identifierToken);

        while (_current.Kind == TokenKind.Dot)
        {
            Advance(); // Consume the dot
            identifierToken = MatchIdentifier();
            parts.Add(identifierToken);
        }

        return _syntaxFactory.QualifiedName(parts.ToSyntaxList());
    }

    // type-reference = name | keyword-type-reference | pointer-type-reference
    // keyword-type-reference = 'f32' | 'f64' | 'never' | 's8' | 's16' | 's32' | 's64' | 'u8' | 'u16' | 'u32' | 'u64' | 'unit'
    // pointer-type-reference = '*' type-reference
    private TypeReferenceSyntax ParseTypeReference()
    {
        switch (_current.Kind)
        {
            case TokenKind.F32:
            case TokenKind.F64:
            case TokenKind.Never:
            case TokenKind.S8:
            case TokenKind.S16:
            case TokenKind.S32:
            case TokenKind.S64:
            case TokenKind.U8:
            case TokenKind.U16:
            case TokenKind.U32:
            case TokenKind.U64:
            case TokenKind.Unit:
                return _syntaxFactory.KeywordTypeReference(Advance());

            case TokenKind.Identifier:
                var name = ParseName();
                return _syntaxFactory.NamedTypeReference(name);

            case TokenKind.Star:
                var starToken = Advance();
                var elementType = ParseTypeReference();
                return _syntaxFactory.PointerTypeReference(starToken, elementType);

            default:
                _diagnostics.AddError(_current, DiagnosticMessages.TypeNameExpected);
                return _syntaxFactory.MissingTypeReference(_current);
        }
    }

    // item-access-modifier = 'pub' | 'mod'
    // item = struct-definition | enum-definition | function-definition
    private SyntaxList<ItemSyntax> ParseItems()
    {
        var items = _syntaxFactory.SyntaxListBuilder<ItemSyntax>();
        Token? accessModifier = null;

        while (true)
        {
            switch (_current.Kind)
            {
                case TokenKind.NewLine:
                    Advance();
                    break;

                case TokenKind.EndOfFile:
                    return items.ToSyntaxList();

                case TokenKind.Pub:
                case TokenKind.Mod:
                    accessModifier = Advance();
                    break;

                case TokenKind.Struct:
                    items.Add(ParseStructDefinition(accessModifier));
                    MatchNewLine();
                    break;

                default:
                    _diagnostics.AddError(_current, DiagnosticMessages.ItemExpected);

                    Advance();
                    continue;
            }
        }
    }

    // struct-definition = item-access-modifier? struct-keyword ID '{' member-declarations? '}'
    // struct-keyword = 'struct' | 'data'
    // member-declarations = NL member-declaration (NL member-declaration)* NL
    private StructDefinitionSyntax ParseStructDefinition(Token? accessModifier)
    {
        var structKeyword = Match(TokenKind.Struct);
        var identifier = MatchIdentifier();
        var openBrace = Match(TokenKind.OpenBrace);
        MatchNewLine();

        var (fields, methods) = ParseMemberDeclarations();
        var closeBrace = Match(TokenKind.CloseBrace);
        return _syntaxFactory.StructDefinition(
            accessModifier,
            structKeyword,
            identifier,
            openBrace,
            fields,
            methods,
            closeBrace
        );
    }

    // member-access-modifier = 'pub' | 'file' | 'mod'
    // member-declaration = field-declaration | method-definition
    private (
        SyntaxList<FieldDeclarationSyntax>,
        SyntaxList<FunctionDefinitionSyntax>
    ) ParseMemberDeclarations()
    {
        var fields = _syntaxFactory.SyntaxListBuilder<FieldDeclarationSyntax>();
        var methods = _syntaxFactory.SyntaxListBuilder<FunctionDefinitionSyntax>();

        Token? accessModifier = null;
        while (true)
        {
            switch (_current.Kind)
            {
                case TokenKind.EndOfFile:
                case TokenKind.CloseBrace:
                    break;

                case TokenKind.NewLine:
                    Advance();
                    continue;

                case TokenKind.Pub:
                case TokenKind.Mod:
                case TokenKind.Identifier
                    when _current.ContextualKeywordKind == ContextualKeywordKind.File:
                    accessModifier = Advance();
                    continue;

                case TokenKind.Mut:
                case TokenKind.Identifier:
                    fields.Add(ParseFieldDeclaration(accessModifier));
                    MatchNewLine();
                    continue;

                case TokenKind.Fun:
                    methods.Add(ParseFunctionDefinition(accessModifier));
                    MatchNewLine();
                    continue;

                default:
                    _diagnostics.AddError(
                        _current,
                        DiagnosticMessages.MemberExpected,
                        _current.ToString()
                    );

                    Advance();
                    continue;
            }

            return (fields.ToSyntaxList(), methods.ToSyntaxList());
        }
    }

    // field-declaration = member-access-modifier? 'mut'? ID type-reference
    private FieldDeclarationSyntax ParseFieldDeclaration(Token? accessModifier)
    {
        var mutableKeyword = _current.Kind == TokenKind.Mut ? Advance() : null;
        var identifier = MatchIdentifier();
        var type = ParseTypeReference();
        return _syntaxFactory.FieldDeclaration(accessModifier, mutableKeyword, identifier, type);
    }

    // enum-definition = item-access-modifier 'enum' ID '{' enum-member-declarations '}'
    // enum-member-declarations = NL enum-member-declaration (NL enum-member-declaration)* NL
    private EnumDefinitionSyntax ParseEnumDefinition(Token? accessModifier)
    {
        var enumKeyword = Match(TokenKind.Enum);
        var identifier = MatchIdentifier();
        var openBrace = Match(TokenKind.OpenBrace);
        var (cases, methods) = ParseEnumMemberDeclarations();
        var closeBrace = Match(TokenKind.CloseBrace);
        return _syntaxFactory.EnumDefinition(
            accessModifier,
            enumKeyword,
            identifier,
            openBrace,
            cases,
            methods,
            closeBrace
        );
    }

    // enum-member-declaration = enum-case-declaration | method-definition
    private (
        SyntaxList<EnumCaseDeclarationSyntax>,
        SyntaxList<FunctionDefinitionSyntax>
    ) ParseEnumMemberDeclarations()
    {
        var cases = _syntaxFactory.SyntaxListBuilder<EnumCaseDeclarationSyntax>();
        var methods = _syntaxFactory.SyntaxListBuilder<FunctionDefinitionSyntax>();

        while (true)
        {
            switch (_current.Kind)
            {
                case TokenKind.EndOfFile:
                case TokenKind.CloseBrace:
                    break;

                case TokenKind.NewLine:
                    Advance();
                    continue;

                case TokenKind.Pub:
                case TokenKind.Mod:
                case TokenKind.Fun:
                case TokenKind.Identifier
                    when _current.ContextualKeywordKind == ContextualKeywordKind.File:
                    methods.Add(ParseFunctionDefinition(null));
                    MatchNewLine();
                    continue;

                case TokenKind.Identifier:
                    cases.Add(ParseEnumCaseDeclaration());
                    MatchNewLine();
                    continue;

                default:
                    _diagnostics.AddError(
                        _current,
                        DiagnosticMessages.MemberExpected,
                        _current.ToString()
                    );

                    Advance();
                    continue;
            }

            return (cases.ToSyntaxList(), methods.ToSyntaxList());
        }
    }

    // enum-case-declaration = ID ('(' type-reference ')')?
    private EnumCaseDeclarationSyntax ParseEnumCaseDeclaration()
    {
        var identifier = MatchIdentifier();
        if (_current.Kind != TokenKind.OpenParenthesis)
        {
            return _syntaxFactory.EnumCaseDeclaration(identifier, null);
        }

        var openParenthesis = Match(TokenKind.OpenParenthesis);
        var type = ParseTypeReference();
        var closeParenthesis = Match(TokenKind.CloseParenthesis);
        return _syntaxFactory.EnumCaseDeclaration(
            identifier,
            _syntaxFactory.EnumCaseArgument(openParenthesis, type, closeParenthesis)
        );
    }

    // method-definition = member-access-modifier? function-definition
    // function-item-definition = item-access-modifier? function-declaration
    // function-definition = 'fun' ID '(' parameter-declarations? ')' type-reference? block
    private FunctionDefinitionSyntax ParseFunctionDefinition(Token? accessModifier)
    {
        var funKeyword = Match(TokenKind.Fun);
        var identifier = MatchIdentifier();
        var openParenthesis = Match(TokenKind.OpenParenthesis);
        var parameters = ParseParameterDeclarations();
        var closeParenthesis = Match(TokenKind.CloseParenthesis);
        var returnType = _current.Kind != TokenKind.OpenBrace ? ParseTypeReference() : null;
        var body = ParseBlock();
        return _syntaxFactory.FunctionDefinition(
            accessModifier,
            funKeyword,
            identifier,
            openParenthesis,
            parameters,
            closeParenthesis,
            returnType,
            body
        );
    }

    // parameter-declarations = parameter-declaration ((',' parameter-declaration)+ ','?)?
    private SyntaxList<ParameterDeclarationSyntax> ParseParameterDeclarations()
    {
        var parameters = _syntaxFactory.SyntaxListBuilder<ParameterDeclarationSyntax>();
        // create loop, handle getting stuck and resyncing on a }, ) or EOF

        return parameters.ToSyntaxList();
    }

    // parameter-declaration = 'mut'? (ID type-reference | '*'? 'self')
    private ParameterDeclarationSyntax ParseParameterDeclaration()
    {
        var mutKeyword = _current.Kind == TokenKind.Mut ? Advance() : null;
        if (_current.Kind == TokenKind.Star)
        {
            var starToken = Advance();
            var selfKeyword = Match(TokenKind.Self);
            var optType = _current.Kind is TokenKind.Comma or TokenKind.CloseParenthesis
                ? null
                : ParseTypeReference();

            return _syntaxFactory.SelfParameterDeclaration(
                mutKeyword,
                starToken,
                selfKeyword,
                optType
            );
        }

        var identifier = MatchIdentifier();
        var type = ParseTypeReference();
        return _syntaxFactory.SimpleParameterDeclaration(mutKeyword, identifier, type);
    }

    private Token Advance()
    {
        _previous = _current;
        _current = _next;
        _next = _scanner.NextToken();
        return _previous;
    }

    private Token Match(TokenKind kind)
    {
        if (_current.Kind == kind)
        {
            return Advance();
        }

        _diagnostics.AddError(
            _current,
            DiagnosticMessages.TokenMismatch,
            SyntaxFacts.GetKindDisplayText(kind),
            _current.ToString()
        );

        return _syntaxFactory.Token(kind, _current.Location.Start, _current.Location.Length);
    }

    private IdentifierToken MatchIdentifier()
    {
        if (_current.Kind == TokenKind.Identifier)
        {
            return (IdentifierToken)Advance();
        }

        _diagnostics.AddError(
            _current,
            DiagnosticMessages.TokenMismatch,
            SyntaxFacts.GetKindDisplayText(TokenKind.Identifier),
            _current.ToString()
        );

        return _syntaxFactory.Identifier(_current.Location.Start, _current.Location.Length);
    }

    private Token MatchNewLine()
    {
        if (_current.Kind is TokenKind.NewLine or TokenKind.EndOfFile)
        {
            return Advance();
        }

        _diagnostics.AddError(
            _current,
            DiagnosticMessages.TokenMismatch,
            SyntaxFacts.GetKindDisplayText(TokenKind.NewLine),
            _current.ToString()
        );

        return _syntaxFactory.Token(
            TokenKind.NewLine,
            _current.Location.Start,
            _current.Location.Length
        );
    }
}
