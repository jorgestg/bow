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
            if (IsItemDeclarationStart())
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

    private bool IsItemDeclarationStart()
    {
        return _current.Kind switch
        {
            SyntaxKind.EnumKeyword or SyntaxKind.FunKeyword or SyntaxKind.StructKeyword => true,
            _ when _current.ContextualKeywordKind == ContextualKeywordKind.Data => true,
            _ => false,
        };
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

    private bool IsTypeReferenceStart()
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

        var slot = 0;
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
                    items.Add(ParseEnumDefinition(slot++, accessModifier));
                    MatchNewLine();
                    accessModifier = null;
                    break;
                }

                case SyntaxKind.FunKeyword:
                {
                    items.Add(ParseFunctionDefinition(slot++, accessModifier));
                    MatchNewLine();
                    accessModifier = null;
                    break;
                }

                case SyntaxKind.StructKeyword:
                case SyntaxKind.IdentifierToken when _current.ContextualKeywordKind == ContextualKeywordKind.Data:
                {
                    items.Add(ParseStructDefinition(slot++, accessModifier, keyword: Advance()));
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
    private StructDefinitionSyntax ParseStructDefinition(int slot, Token? accessModifier, Token keyword)
    {
        var identifier = MatchIdentifier();
        var openBrace = Match(SyntaxKind.OpenBraceToken);
        MatchNewLine();

        var (fields, methods) = ParseMemberDeclarations();
        var closeBrace = Match(SyntaxKind.CloseBraceToken);
        return _syntaxFactory.StructDefinition(
            slot,
            accessModifier,
            keyword,
            identifier,
            openBrace,
            fields,
            methods,
            closeBrace
        );
    }

    // member-access-modifier = 'pub' | 'pkg' | 'mod' | 'file'
    // member-declaration = field-declaration | method-definition
    private (SyntaxList<FieldDeclarationSyntax>, SyntaxList<FunctionDefinitionSyntax>) ParseMemberDeclarations()
    {
        SyntaxListBuilder<FieldDeclarationSyntax> fields = new();
        SyntaxListBuilder<FunctionDefinitionSyntax> methods = new();

        var slot = 0;
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
                    fields.Add(ParseFieldDeclaration(slot++, accessModifier));
                    MatchNewLine();
                    continue;

                case SyntaxKind.FunKeyword:
                    methods.Add(ParseFunctionDefinition(slot++, accessModifier));
                    MatchNewLine();
                    continue;

                default:
                    _diagnostics.AddError(_current, DiagnosticMessages.MemberExpected, _current.ToString());

                    Advance();
                    continue;
            }

            return (fields.ToSyntaxList(), methods.ToSyntaxList());
        }
    }

    // field-declaration = member-access-modifier? 'mut'? ID type-reference
    private FieldDeclarationSyntax ParseFieldDeclaration(int slot, Token? accessModifier)
    {
        var mutableKeyword = _current.Kind == SyntaxKind.MutKeyword ? Advance() : null;
        var identifier = MatchIdentifier();
        var type = ParseTypeReference();
        return _syntaxFactory.FieldDeclaration(slot, accessModifier, mutableKeyword, identifier, type);
    }

    // enum-definition = item-access-modifier 'enum' ID '{' enum-member-declarations '}'
    // enum-member-declarations = NL enum-member-declaration (NL enum-member-declaration)* NL
    private EnumDefinitionSyntax ParseEnumDefinition(int slot, Token? accessModifier)
    {
        var enumKeyword = Match(SyntaxKind.EnumKeyword);
        var identifier = MatchIdentifier();
        var openBrace = Match(SyntaxKind.OpenBraceToken);
        var (cases, methods) = ParseEnumMemberDeclarations();
        var closeBrace = Match(SyntaxKind.CloseBraceToken);
        return _syntaxFactory.EnumDefinition(
            slot,
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
    private (SyntaxList<EnumCaseDeclarationSyntax>, SyntaxList<FunctionDefinitionSyntax>) ParseEnumMemberDeclarations()
    {
        SyntaxListBuilder<EnumCaseDeclarationSyntax> cases = new();
        SyntaxListBuilder<FunctionDefinitionSyntax> methods = new();

        var slot = 0;
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
                    methods.Add(ParseFunctionDefinition(slot++, null));
                    MatchNewLine();
                    continue;

                case SyntaxKind.IdentifierToken:
                    cases.Add(ParseEnumCaseDeclaration(slot));
                    MatchNewLine();
                    continue;

                default:
                    _diagnostics.AddError(_current, DiagnosticMessages.MemberExpected, _current.ToString());

                    Advance();
                    continue;
            }

            return (cases.ToSyntaxList(), methods.ToSyntaxList());
        }
    }

    // enum-case-declaration = ID ('(' type-reference ')')?
    private EnumCaseDeclarationSyntax ParseEnumCaseDeclaration(int slot)
    {
        var identifier = MatchIdentifier();
        if (_current.Kind != SyntaxKind.OpenParenthesisToken)
        {
            return _syntaxFactory.EnumCaseDeclaration(slot, identifier, null);
        }

        var openParenthesis = Match(SyntaxKind.OpenParenthesisToken);
        var type = ParseTypeReference();
        var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
        return _syntaxFactory.EnumCaseDeclaration(
            slot,
            identifier,
            _syntaxFactory.EnumCaseArgument(openParenthesis, type, closeParenthesis)
        );
    }

    // method-definition = member-access-modifier? function-definition
    // function-item-definition = item-access-modifier? function-declaration
    // function-definition = 'fun' ID '(' parameter-declarations? ')' type-reference? block
    private FunctionDefinitionSyntax ParseFunctionDefinition(int slot, Token? accessModifier)
    {
        var funKeyword = Match(SyntaxKind.FunKeyword);
        var identifier = MatchIdentifier();
        var openParenthesis = Match(SyntaxKind.OpenParenthesisToken);
        var parameters = ParseParameterDeclarations();
        var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
        var returnType = IsTypeReferenceStart() ? ParseTypeReference() : null;
        var block = ParseBlockStatement();
        return _syntaxFactory.FunctionDefinition(
            slot,
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

        while (_current.Kind is not SyntaxKind.CloseParenthesisToken and not SyntaxKind.EndOfFileToken)
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
            var optionalType = IsTypeReferenceStart() ? ParseTypeReference() : null;
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
        MatchNewLine();

        while (_current.Kind is not SyntaxKind.EndOfFileToken and not SyntaxKind.CloseBraceToken)
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
        var type = IsTypeReferenceStart() ? ParseTypeReference() : null;
        LocalDeclarationInitializerSyntax? initializer = null;
        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            var operatorToken = Advance();
            var initializerExpression = ParseExpression();
            initializer = _syntaxFactory.LocalDeclarationInitializer(operatorToken, initializerExpression);
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

    // if-statement = 'if' expression block ('else' 'if' expression block)* ('else' block)?
    private IfStatementSyntax ParseIfStatement()
    {
        var ifKeyword = Match(SyntaxKind.IfKeyword);
        var condition = ParseExpression();
        var thenBlock = ParseBlockStatement();
        if (_current.Kind != SyntaxKind.ElseKeyword)
        {
            return _syntaxFactory.IfStatement(
                ifKeyword,
                condition,
                thenBlock,
                new SyntaxList<ElseIfClauseSyntax>(),
                null
            );
        }

        SyntaxListBuilder<ElseIfClauseSyntax> elseIfClauses = new();
        while (_current.Kind == SyntaxKind.ElseKeyword)
        {
            var elseKeyword = Advance();
            var elseIfKeyword = Match(SyntaxKind.IfKeyword);
            var elseIfCondition = ParseExpression();
            var elseIfBlock = ParseBlockStatement();
            elseIfClauses.Add(_syntaxFactory.ElseIfClause(elseKeyword, elseIfKeyword, elseIfCondition, elseIfBlock));
        }

        ElseClauseSyntax? elseClause = null;
        if (_current.Kind == SyntaxKind.ElseKeyword)
        {
            var elseKeyword = Advance();
            var elseBlock = ParseBlockStatement();
            elseClause = _syntaxFactory.ElseClause(elseKeyword, elseBlock);
        }

        return _syntaxFactory.IfStatement(ifKeyword, condition, thenBlock, elseIfClauses.ToSyntaxList(), elseClause);
    }

    // return-statement = 'return' expression
    private ReturnStatementSyntax ParseReturnStatement()
    {
        var returnKeyword = Match(SyntaxKind.ReturnKeyword);
        var expression = ParseExpression();
        MatchNewLine();
        return _syntaxFactory.ReturnStatement(returnKeyword, expression);
    }

    // expression = literal | identifier-expression | call-expression | unary-expression | binary-expression
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
    // arguments = expression ((',' expression)+ ','?)?
    private ExpressionSyntax ParseCallExpression()
    {
        var expression = ParsePrimary();
        if (_current.Kind != SyntaxKind.OpenParenthesisToken)
        {
            return expression;
        }

        var openParenthesis = Advance();
        var arguments = ParseArguments();
        var closeParenthesis = Match(SyntaxKind.CloseParenthesisToken);
        return _syntaxFactory.CallExpression(expression, openParenthesis, arguments, closeParenthesis);
    }

    private SyntaxList<ExpressionSyntax> ParseArguments()
    {
        SyntaxListBuilder<ExpressionSyntax> arguments = new();
        while (_current.Kind is not SyntaxKind.CloseParenthesisToken and not SyntaxKind.EndOfFileToken)
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

    private Token MatchNewLine()
    {
        if (_current.Kind == SyntaxKind.NewLineToken)
        {
            return Advance();
        }

        if (_current.Kind == SyntaxKind.EndOfFileToken)
        {
            return _current;
        }

        _diagnostics.AddError(
            _current,
            DiagnosticMessages.TokenMismatch,
            SyntaxFacts.GetKindDisplayText(SyntaxKind.NewLineToken),
            _current.ToString()
        );

        return _syntaxFactory.MissingToken(SyntaxKind.NewLineToken, _current.Location.Start, _current.Location.Length);
    }
}
