using Bow.Compiler.Diagnostics;

namespace Bow.Compiler.Syntax;

internal sealed class Parser(SyntaxFactory syntaxFactory)
{
    private readonly SyntaxFactory _syntaxFactory = syntaxFactory;
    private readonly Scanner _scanner = new(syntaxFactory);
    private readonly DiagnosticBag _diagnostics = new();

    private Token _current = null!;

    public Scanner Scanner => _scanner;

    public ImmutableArray<Diagnostic> GetDiagnostics()
    {
        return _diagnostics.ToImmutableArray();
    }

    // compilation-unit = mod-clause? use-clause* item+
    public CompilationUnitSyntax ParseCompilationUnit()
    {
        _current = _scanner.NextToken();

        ModClauseSyntax? modClause = null;
        if (_current.Kind == SyntaxKind.ModKeyword)
        {
            var modKeyword = Advance();
            if (CurrentIsItemDeclarationStart())
            {
                return _syntaxFactory.CompilationUnit(
                    null,
                    // If we are already defining items, it's safe to skip use clauses.
                    new SyntaxList<UseClauseSyntax>(),
                    ParseItems(modKeyword)
                );
            }

            modClause = ParseModClause(modKeyword);
        }

        var useClauses = ParseUseClauses();
        var items = ParseItems();
        return _syntaxFactory.CompilationUnit(modClause, useClauses, items);
    }

    private bool CurrentIsItemDeclarationStart()
    {
        return _current.Kind is SyntaxKind.EnumKeyword or SyntaxKind.FunKeyword or SyntaxKind.StructKeyword
            || _current.ContextualKeywordKind == ContextualKeywordKind.Data;
    }

    // mod-clause = 'mod' ID NL
    private ModClauseSyntax ParseModClause(Token modKeyword)
    {
        var identifier = MatchIdentifier();
        MatchNewLine();
        return _syntaxFactory.ModClause(modKeyword, identifier);
    }

    private SyntaxList<UseClauseSyntax> ParseUseClauses()
    {
        SyntaxListBuilder<UseClauseSyntax> useClauses = new();
        while (_current.Kind == SyntaxKind.UseKeyword)
        {
            var useClause = ParseUseClause();
            useClauses.Add(useClause);
        }

        return useClauses.ToSyntaxList();
    }

    // use-clause = 'use' ID '.' ID NL
    private UseClauseSyntax ParseUseClause()
    {
        var useKeyword = Match(SyntaxKind.UseKeyword);
        var name = ParseName();
        MatchNewLine();
        return _syntaxFactory.UseClause(useKeyword, name);
    }

    // name = ID | qualified-name
    // qualified-name = ID ('.' ID)+
    private NameSyntax ParseName()
    {
        var identifierToken = MatchIdentifier();
        if (_current.Kind != SyntaxKind.DotToken)
        {
            return _syntaxFactory.SimpleName(identifierToken);
        }

        SyntaxListBuilder<IdentifierToken> parts = new();
        parts.Add(identifierToken);

        while (_current.Kind == SyntaxKind.DotToken)
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
            case SyntaxKind.F32Keyword:
            case SyntaxKind.F64Keyword:
            case SyntaxKind.NeverKeyword:
            case SyntaxKind.S8Keyword:
            case SyntaxKind.S16Keyword:
            case SyntaxKind.S32Keyword:
            case SyntaxKind.S64Keyword:
            case SyntaxKind.U8Keyword:
            case SyntaxKind.U16Keyword:
            case SyntaxKind.U32Keyword:
            case SyntaxKind.U64Keyword:
            case SyntaxKind.UnitKeyword:
                return _syntaxFactory.KeywordTypeReference(Advance());

            case SyntaxKind.IdentifierToken:
                var name = ParseName();
                return _syntaxFactory.NamedTypeReference(name);

            case SyntaxKind.StarToken:
                var starToken = Advance();
                var elementType = ParseTypeReference();
                return _syntaxFactory.PointerTypeReference(starToken, elementType);

            default:
                _diagnostics.AddError(_current, DiagnosticMessages.TypeNameExpected);
                return _syntaxFactory.MissingTypeReference(_current);
        }
    }

    private bool CurrentIsTypeReferenceStart()
    {
        return _current.Kind
            is SyntaxKind.F32Keyword
                or SyntaxKind.F64Keyword
                or SyntaxKind.NeverKeyword
                or SyntaxKind.S8Keyword
                or SyntaxKind.S16Keyword
                or SyntaxKind.S32Keyword
                or SyntaxKind.S64Keyword
                or SyntaxKind.U8Keyword
                or SyntaxKind.U16Keyword
                or SyntaxKind.U32Keyword
                or SyntaxKind.U64Keyword
                or SyntaxKind.UnitKeyword
                or SyntaxKind.IdentifierToken
                or SyntaxKind.StarToken;
    }

    // item-access-modifier = 'pub' | 'pkg' | 'mod'
    // item = struct-definition | enum-definition | function-definition
    private SyntaxList<ItemSyntax> ParseItems(Token? modKeyword = null)
    {
        SyntaxListBuilder<ItemSyntax> items = new();

        Token? accessModifier = modKeyword;
        while (true)
        {
            switch (_current.Kind)
            {
                case SyntaxKind.EndOfFileToken:
                    return items.ToSyntaxList();

                case SyntaxKind.PubKeyword:
                case SyntaxKind.PkgKeyword:
                case SyntaxKind.ModKeyword:
                {
                    if (accessModifier != null)
                    {
                        _diagnostics.AddError(
                            _current,
                            DiagnosticMessages.AccessModifierAlreadySpecified,
                            accessModifier.ToString()
                        );

                        Advance();
                        break;
                    }

                    accessModifier = Advance();
                    break;
                }

                case SyntaxKind.EnumKeyword:
                {
                    items.Add(ParseEnumDefinition(accessModifier));
                    MatchNewLine();
                    accessModifier = null;
                    break;
                }

                case SyntaxKind.FunKeyword:
                {
                    items.Add(ParseFunctionDefinition(accessModifier));
                    MatchNewLine();
                    accessModifier = null;
                    break;
                }

                case SyntaxKind.StructKeyword:
                case SyntaxKind.IdentifierToken when _current.ContextualKeywordKind == ContextualKeywordKind.Data:
                {
                    items.Add(ParseStructDefinition(accessModifier, keyword: Advance()));
                    MatchNewLine();
                    accessModifier = null;
                    break;
                }

                default:
                {
                    _diagnostics.AddError(_current, DiagnosticMessages.ItemExpected);
                    Advance();
                    continue;
                }
            }
        }
    }

    // struct-definition = item-access-modifier? struct-keyword ID '{' member-declarations? '}'
    // struct-keyword = 'struct' | 'data'
    // member-declarations = NL member-declaration (NL member-declaration)* NL
    private StructDefinitionSyntax ParseStructDefinition(Token? accessModifier, Token keyword)
    {
        var identifier = MatchIdentifier();
        var openBrace = Match(SyntaxKind.OpenBraceToken);
        MatchNewLine();

        var members = ParseMemberDeclarations();
        var closeBrace = Match(SyntaxKind.CloseBraceToken);
        return _syntaxFactory.StructDefinition(accessModifier, keyword, identifier, openBrace, members, closeBrace);
    }

    // member-access-modifier = 'pub' | 'pkg' | 'mod' | 'file'
    // member-declaration = field-declaration | method-definition
    private SyntaxList<MemberDeclarationSyntax> ParseMemberDeclarations()
    {
        SyntaxListBuilder<MemberDeclarationSyntax> members = new();

        Token? accessModifier = null;
        while (true)
        {
            switch (_current.Kind)
            {
                case SyntaxKind.EndOfFileToken:
                case SyntaxKind.CloseBraceToken:
                    break;

                case SyntaxKind.PubKeyword:
                case SyntaxKind.PkgKeyword:
                case SyntaxKind.ModKeyword:
                case SyntaxKind.IdentifierToken when _current.ContextualKeywordKind == ContextualKeywordKind.File:
                    accessModifier = Advance();
                    continue;

                case SyntaxKind.MutKeyword:
                case SyntaxKind.IdentifierToken:
                    members.Add(ParseFieldDeclaration(accessModifier));
                    MatchNewLine();
                    continue;

                case SyntaxKind.FunKeyword:
                    members.Add(ParseMethodDefinition(accessModifier));
                    MatchNewLine();
                    continue;

                default:
                    _diagnostics.AddError(_current, DiagnosticMessages.MemberExpected, _current.ToString());

                    Advance();
                    continue;
            }

            return members.ToSyntaxList();
        }
    }

    // field-declaration = member-access-modifier? 'mut'? ID type-reference
    private FieldDeclarationSyntax ParseFieldDeclaration(Token? accessModifier)
    {
        var mutableKeyword = _current.Kind == SyntaxKind.MutKeyword ? Advance() : null;
        var identifier = MatchIdentifier();
        var type = ParseTypeReference();
        return _syntaxFactory.FieldDeclaration(accessModifier, mutableKeyword, identifier, type);
    }

    // enum-definition = item-access-modifier 'enum' ID '{' enum-member-declarations '}'
    // enum-member-declarations = NL enum-member-declaration (NL enum-member-declaration)* NL
    private EnumDefinitionSyntax ParseEnumDefinition(Token? accessModifier)
    {
        var enumKeyword = Match(SyntaxKind.EnumKeyword);
        var identifier = MatchIdentifier();
        var openBrace = Match(SyntaxKind.OpenBraceToken);
        var members = ParseEnumMemberDeclarations();
        var closeBrace = Match(SyntaxKind.CloseBraceToken);
        return _syntaxFactory.EnumDefinition(accessModifier, enumKeyword, identifier, openBrace, members, closeBrace);
    }

    // enum-member-declaration = enum-case-declaration | method-definition
    private SyntaxList<MemberDeclarationSyntax> ParseEnumMemberDeclarations()
    {
        SyntaxListBuilder<MemberDeclarationSyntax> members = new();

        while (true)
        {
            switch (_current.Kind)
            {
                case SyntaxKind.EndOfFileToken:
                case SyntaxKind.CloseBraceToken:
                    break;

                case SyntaxKind.PubKeyword:
                case SyntaxKind.ModKeyword:
                case SyntaxKind.FunKeyword:
                case SyntaxKind.IdentifierToken when _current.ContextualKeywordKind == ContextualKeywordKind.File:
                    members.Add(ParseMethodDefinition(null));
                    MatchNewLine();
                    continue;

                case SyntaxKind.IdentifierToken:
                    members.Add(ParseEnumCaseDeclaration());
                    MatchNewLine();
                    continue;

                default:
                    _diagnostics.AddError(_current, DiagnosticMessages.MemberExpected, _current.ToString());

                    Advance();
                    continue;
            }

            return members.ToSyntaxList();
        }
    }

    // enum-case-declaration = ID ('(' type-reference ')')?
    private EnumCaseDeclarationSyntax ParseEnumCaseDeclaration()
    {
        var identifier = MatchIdentifier();
        if (_current.Kind != SyntaxKind.OpenParenthesisToken)
        {
            return _syntaxFactory.EnumCaseDeclaration(identifier, null);
        }

        var openParenthesis = Match(SyntaxKind.OpenParenthesisToken);
        var type = ParseTypeReference();
        var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
        return _syntaxFactory.EnumCaseDeclaration(
            identifier,
            _syntaxFactory.EnumCaseArgument(openParenthesis, type, closeParenthesis)
        );
    }

    // function-item-definition = item-access-modifier? function-declaration
    // function-definition = 'fun' ID '(' parameter-declarations? ')' type-reference? block
    private FunctionDefinitionSyntax ParseFunctionDefinition(Token? accessModifier)
    {
        var funKeyword = Match(SyntaxKind.FunKeyword);
        var identifier = MatchIdentifier();
        var openParenthesis = Match(SyntaxKind.OpenParenthesisToken);
        var parameters = ParseParameterDeclarations();
        var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
        var returnType = CurrentIsTypeReferenceStart() ? ParseTypeReference() : null;
        var block = ParseBlockStatement();
        return _syntaxFactory.FunctionDefinition(
            accessModifier,
            funKeyword,
            identifier,
            openParenthesis,
            parameters,
            closeParenthesis,
            returnType,
            block
        );
    }

    // method-definition = member-access-modifier? function-definition
    private MethodDefinitionSyntax ParseMethodDefinition(Token? accessModifier)
    {
        var funKeyword = Match(SyntaxKind.FunKeyword);
        var identifier = MatchIdentifier();
        var openParenthesis = Match(SyntaxKind.OpenParenthesisToken);
        var parameters = ParseParameterDeclarations();
        var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
        var returnType = CurrentIsTypeReferenceStart() ? ParseTypeReference() : null;
        var block = ParseBlockStatement();
        return _syntaxFactory.MethodDefinition(
            accessModifier,
            funKeyword,
            identifier,
            openParenthesis,
            parameters,
            closeParenthesis,
            returnType,
            block
        );
    }

    // parameter-declarations = parameter-declaration ((',' parameter-declaration)+ ','?)?
    private SyntaxList<ParameterDeclarationSyntax> ParseParameterDeclarations()
    {
        SyntaxListBuilder<ParameterDeclarationSyntax> parameters = new();
        while (CurrentIsNotDelimiter(SyntaxKind.CloseParenthesisToken))
        {
            var parameter = ParseParameterDeclaration();
            parameters.Add(parameter);

            if (_current.Kind == SyntaxKind.CommaToken)
            {
                Advance();
                continue;
            }

            break;
        }

        return parameters.ToSyntaxList();
    }

    // parameter-declaration = simple-parameter-declaration | self-parameter-declaration
    // simple-parameter-declaration = 'mut'? ID type-reference
    // self-parameter-declaration = 'mut'? '*'? 'self' | 'mut'? 'self' type-reference
    private ParameterDeclarationSyntax ParseParameterDeclaration()
    {
        var mutableKeyword = _current.Kind == SyntaxKind.MutKeyword ? Advance() : null;
        if (_current.Kind == SyntaxKind.StarToken)
        {
            var starToken = Advance();
            var selfKeyword = Match(SyntaxKind.SelfKeyword);
            return _syntaxFactory.SelfParameterDeclaration(mutableKeyword, starToken, selfKeyword, null);
        }

        if (_current.Kind == SyntaxKind.SelfKeyword)
        {
            var selfKeyword = Match(SyntaxKind.SelfKeyword);
            var optionalType = CurrentIsTypeReferenceStart() ? ParseTypeReference() : null;
            return _syntaxFactory.SelfParameterDeclaration(mutableKeyword, null, selfKeyword, optionalType);
        }

        var identifier = MatchIdentifier();
        var type = ParseTypeReference();
        return _syntaxFactory.SimpleParameterDeclaration(mutableKeyword, identifier, type);
    }

    // block = '{' (statements NL)? '}'
    private BlockStatementSyntax ParseBlockStatement()
    {
        SyntaxListBuilder<StatementSyntax> statements = new();

        if (_current.Kind == SyntaxKind.NewLineToken)
        {
            _diagnostics.AddError(_current, DiagnosticMessages.BraceShouldGoOnTheSameLine);
            Advance();
        }

        var openBrace = Match(SyntaxKind.OpenBraceToken);
        var isMultiline = _current.Kind == SyntaxKind.NewLineToken;
        if (isMultiline)
        {
            Advance();
        }

        while (CurrentIsNotDelimiter(SyntaxKind.CloseBraceToken))
        {
            var startToken = _current;

            var statement = ParseStatement();
            statements.Add(statement);

            // Avoid infinite loop
            if (_current == startToken)
            {
                Advance();
                continue;
            }

            if (!isMultiline)
            {
                break;
            }

            MatchNewLine();
        }

        var closeBrace = Match(SyntaxKind.CloseBraceToken);
        return _syntaxFactory.BlockStatement(openBrace, statements.ToSyntaxList(), closeBrace);
    }

    // statement = local-declaration | return-statement | if-statement | assignment-statement | expression
    private StatementSyntax ParseStatement()
    {
        return _current.Kind switch
        {
            SyntaxKind.LetKeyword => ParseLocalDeclaration(),
            SyntaxKind.IfKeyword => ParseIfStatement(),
            SyntaxKind.WhileKeyword => ParseWhileStatement(),
            SyntaxKind.ReturnKeyword => ParseReturnStatement(),
            _ => ParseAssignmentOrExpressionStatement(),
        };
    }

    // local-declaration = 'let' 'mut'? ID type-reference? '=' expression | 'let' 'mut'? ID type-reference
    private LocalDeclarationSyntax ParseLocalDeclaration()
    {
        var letKeyword = Match(SyntaxKind.LetKeyword);
        var mutableKeyword = _current.Kind == SyntaxKind.MutKeyword ? Advance() : null;
        var identifier = MatchIdentifier();
        var type = CurrentIsTypeReferenceStart() ? ParseTypeReference() : null;
        InitializerSyntax? initializer = null;
        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            var operatorToken = Advance();
            var initializerExpression = ParseExpression();
            initializer = _syntaxFactory.Initializer(operatorToken, initializerExpression);
        }

        return _syntaxFactory.LocalDeclaration(letKeyword, mutableKeyword, identifier, type, initializer);
    }

    // assignment-statement = expression '=' expression
    // expression-statement = expression
    private StatementSyntax ParseAssignmentOrExpressionStatement()
    {
        var expression = ParseExpression();
        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            var assignee = expression;
            var operatorToken = Advance();
            var right = ParseExpression();
            return _syntaxFactory.AssignmentStatement(assignee, operatorToken, right);
        }

        return _syntaxFactory.ExpressionStatement(expression);
    }

    // while-statement = 'while' expression block
    private WhileStatementSyntax ParseWhileStatement()
    {
        var whileKeyword = Match(SyntaxKind.WhileKeyword);
        var condition = ParseExpression();
        var body = ParseBlockStatement();
        return _syntaxFactory.WhileStatement(whileKeyword, condition, body);
    }

    // if-statement = 'if' expression block else-block?
    // else-block = 'else' (if-statement | block)
    private IfStatementSyntax ParseIfStatement()
    {
        var ifKeyword = Match(SyntaxKind.IfKeyword);
        var condition = ParseExpression();
        var thenBlock = ParseBlockStatement();
        if (_current.Kind != SyntaxKind.ElseKeyword)
        {
            return _syntaxFactory.IfStatement(ifKeyword, condition, thenBlock, null);
        }

        var elseKeyword = Advance();
        StatementSyntax elseBody = _current.Kind == SyntaxKind.IfKeyword ? ParseIfStatement() : ParseBlockStatement();
        var @else = _syntaxFactory.ElseBlock(elseKeyword, elseBody);
        return _syntaxFactory.IfStatement(ifKeyword, condition, thenBlock, @else);
    }

    // return-statement = 'return' expression
    private ReturnStatementSyntax ParseReturnStatement()
    {
        var returnKeyword = Match(SyntaxKind.ReturnKeyword);
        var expression = ParseExpression();
        return _syntaxFactory.ReturnStatement(returnKeyword, expression);
    }

    // expression = literal | identifier-expression | call-expression | struct-creation-expression | unary-expression | binary-expression
    private ExpressionSyntax ParseExpression()
    {
        return ParseBinaryExpression();
    }

    // unary-expression = ('not' | '-') expression
    // binary-expression = expression (
    //     '*' | '/' | '%'
    //     | '+' | '-'
    //     | '>' | '>=' | '<' | '<='
    //     | '==' | '<>'
    //     | '&'
    //     | '|'
    //     | 'and'
    //     | 'or'
    // ) expression
    private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryOperatorPrecedence = SyntaxFacts.GetUnaryOperatorPrecedence(_current.Kind);
        if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
        {
            var operatorToken = Advance();
            var operand = ParseBinaryExpression(unaryOperatorPrecedence);
            left = _syntaxFactory.UnaryExpression(operatorToken, operand);
        }
        else
        {
            left = ParseCallExpression();
        }

        while (true)
        {
            var precedence = SyntaxFacts.GetBinaryOperatorPrecedence(_current.Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = Advance();
            var right = ParseBinaryExpression(precedence);
            left = _syntaxFactory.BinaryExpression(left, operatorToken, right);
        }

        return left;
    }

    // call-expression = primary-expression '(' arguments? ')'
    private ExpressionSyntax ParseCallExpression()
    {
        var left = ParsePrimary();
        if (_current.Kind != SyntaxKind.OpenParenthesisToken)
        {
            return left;
        }

        var openParenthesis = Advance();
        var arguments = ParseArguments();
        var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
        return _syntaxFactory.CallExpression(left, openParenthesis, arguments, closeParenthesis);
    }

    // arguments = expression ((',' expression)+ ','?)?
    private SyntaxList<ExpressionSyntax> ParseArguments()
    {
        SyntaxListBuilder<ExpressionSyntax> arguments = new();
        while (CurrentIsNotDelimiter(SyntaxKind.CloseParenthesisToken))
        {
            var argument = ParseExpression();
            arguments.Add(argument);
            if (_current.Kind == SyntaxKind.CommaToken)
            {
                Advance();
                continue;
            }

            break;
        }

        return arguments.ToSyntaxList();
    }

    // primary-expression = literal | identifier-expression
    // literal = INT | 'true' | 'false'
    // identifier-expression = ID
    private ExpressionSyntax ParsePrimary()
    {
        switch (_current.Kind)
        {
            case SyntaxKind.DotToken:
                return ParseStructCreationExpression();

            case SyntaxKind.IntegerLiteral:
            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
                return _syntaxFactory.LiteralExpression(Advance());

            case SyntaxKind.IdentifierToken:
                return _syntaxFactory.IdentifierExpression(MatchIdentifier());

            case SyntaxKind.OpenParenthesisToken:
            {
                var openParenthesis = Advance();
                var expression = ParseExpression();
                var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
                return _syntaxFactory.ParenthesizedExpression(openParenthesis, expression, closeParenthesis);
            }

            default:
                _diagnostics.AddError(_current, DiagnosticMessages.ExpressionExpected);
                return _syntaxFactory.MissingExpression(_current);
        }
    }

    // struct-creation-expression = '.' '{' NL? (field-initialization (NL field-initialization)*)? '}'
    // field-initialization = ID ('=' expression)?
    private StructCreationExpressionSyntax ParseStructCreationExpression()
    {
        var dot = Match(SyntaxKind.DotToken);
        var openBrace = Match(SyntaxKind.OpenBraceToken);
        var isMultiline = _current.Kind == SyntaxKind.NewLineToken;
        if (isMultiline)
        {
            Advance();
        }

        if (_current.Kind == SyntaxKind.CloseBraceToken)
        {
            return _syntaxFactory.StructCreationExpression(
                dot,
                openBrace,
                new SyntaxList<StructCreationFieldInitializerSyntax>(),
                Advance()
            );
        }

        SyntaxListBuilder<StructCreationFieldInitializerSyntax> fields = new();
        while (CurrentIsNotDelimiter(SyntaxKind.CloseBraceToken))
        {
            var identifier = MatchIdentifier();
            InitializerSyntax? initializer = null;
            if (_current.Kind == SyntaxKind.EqualsToken)
            {
                var operatorToken = Advance();
                var expression = ParseExpression();
                initializer = _syntaxFactory.Initializer(operatorToken, expression);
            }

            var field = _syntaxFactory.StructCreationFieldInitializer(identifier, initializer);
            fields.Add(field);

            if (!isMultiline)
            {
                break;
            }

            MatchNewLine();
        }

        var closeBrace = Match(SyntaxKind.CloseBraceToken);
        return _syntaxFactory.StructCreationExpression(dot, openBrace, fields.ToSyntaxList(), closeBrace);
    }

    private Token Advance()
    {
        var previous = _current;
        _current = _scanner.NextToken();
        return previous;
    }

    private Token Match(SyntaxKind kind)
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

        return _syntaxFactory.MissingToken(kind, _current.Location.Start, _current.Location.Length);
    }

    private IdentifierToken MatchIdentifier()
    {
        if (_current.Kind == SyntaxKind.IdentifierToken)
        {
            return (IdentifierToken)Advance();
        }

        _diagnostics.AddError(
            _current,
            DiagnosticMessages.TokenMismatch,
            SyntaxFacts.GetKindDisplayText(SyntaxKind.IdentifierToken),
            _current.ToString()
        );

        return _syntaxFactory.MissingIdentifier(_current.Location.Start, _current.Location.Length);
    }

    private void MatchNewLine()
    {
        if (_current.Kind == SyntaxKind.NewLineToken)
        {
            Advance();
            return;
        }

        if (_current.Kind == SyntaxKind.EndOfFileToken)
        {
            return;
        }

        _diagnostics.AddError(
            _current,
            DiagnosticMessages.TokenMismatch,
            SyntaxFacts.GetKindDisplayText(SyntaxKind.NewLineToken),
            _current.ToString()
        );
    }

    private bool CurrentIsNotDelimiter(SyntaxKind delimiter)
    {
        return _current.Kind != delimiter && _current.Kind != SyntaxKind.EndOfFileToken;
    }
}
