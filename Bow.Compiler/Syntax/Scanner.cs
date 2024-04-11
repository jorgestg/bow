namespace Bow.Compiler.Syntax;

internal sealed class Scanner(SyntaxFactory syntaxFactory)
{
    private readonly SyntaxFactory _syntaxFactory = syntaxFactory;
    private readonly string _source = syntaxFactory.SyntaxTree.SourceText.Text;

    private int _startIndex;
    private int _currentIndex;

    private char CurrentChar => _source.Length > _currentIndex ? _source[_currentIndex] : '\0';
    private char LookAheadChar => _source.Length > _currentIndex + 1 ? _source[_currentIndex + 1] : '\0';

    public Token NextToken()
    {
        Token? newLine = SkipWhitespace();
        if (newLine != null)
        {
            return newLine;
        }

        _startIndex = _currentIndex;
        var c = CurrentChar;
        _currentIndex++;

        return c switch
        {
            // Special
            '\0' => CreateToken(SyntaxKind.EndOfFileToken),
            '\n' => CreateToken(SyntaxKind.NewLineToken),

            // Delimiters
            ',' => CreateToken(SyntaxKind.CommaToken),
            '.' => CreateToken(SyntaxKind.DotToken),
            '{' => CreateToken(SyntaxKind.OpenBraceToken),
            '}' => CreateToken(SyntaxKind.CloseBraceToken),
            '(' => CreateToken(SyntaxKind.OpenParenthesisToken),
            ')' => CreateToken(SyntaxKind.CloseParenthesisToken),

            // Operators
            '*' => CreateToken(SyntaxKind.StarToken),
            '/' => CreateToken(SyntaxKind.SlashToken),
            '+' => CreateToken(SyntaxKind.PlusToken),
            '-' => char.IsAsciiDigit(CurrentChar) ? CreateNumberToken() : CreateToken(SyntaxKind.MinusToken),
            '%' => CreateToken(SyntaxKind.PercentToken),
            '=' => CreateCompoundToken('=', SyntaxKind.EqualEqualToken, SyntaxKind.EqualsToken),
            '>' => CreateCompoundToken('=', SyntaxKind.GreaterThanEqualToken, SyntaxKind.GreaterThanToken),

            '<'
                => CurrentChar == '>'
                    ? CreateToken(SyntaxKind.DiamondToken)
                    : CreateCompoundToken('=', SyntaxKind.LessThanEqualToken, SyntaxKind.LessThanToken),

            '&' => CreateToken(SyntaxKind.AmpersandToken),
            '|' => CreateToken(SyntaxKind.PipeToken),

            // Literals
            '"' => CreateStringLiteralToken(),
            _ when char.IsAsciiLetter(c) => CreateIdentifierOrKeywordToken(),
            _ when char.IsDigit(c) => CreateNumberToken(),
            _ => CreateToken(SyntaxKind.UnknownToken)
        };
    }

    private Token? SkipWhitespace()
    {
        var newLineIndex = -1;
        while (true)
        {
            switch (CurrentChar)
            {
                case '\0':
                    return null;

                case '\n':
                    newLineIndex = _currentIndex++;
                    break;

                case ' ':
                case '\t':
                case '\r':
                    _currentIndex++;
                    break;

                case '/' when LookAheadChar == '/':
                    // Skip comment
                    while (CurrentChar != '\n' && CurrentChar != '\0')
                    {
                        _currentIndex++;
                    }

                    break;

                default:
                    return newLineIndex == -1 ? null : _syntaxFactory.Token(SyntaxKind.NewLineToken, newLineIndex, 1);
            }
        }
    }

    private Token CreateCompoundToken(char nextChar, SyntaxKind compound, SyntaxKind simple)
    {
        if (CurrentChar == nextChar)
        {
            _currentIndex++;
            return CreateToken(compound);
        }

        return CreateToken(simple);
    }

    public Token CreateStringLiteralToken()
    {
        while (true)
        {
            switch (CurrentChar)
            {
                case '\0':
                case '\n':
                    return CreateToken(SyntaxKind.UnterminatedStringLiteral);

                case '"':
                    // Eat closing "
                    _currentIndex++;
                    return CreateToken(SyntaxKind.StringLiteral);

                default:
                    _currentIndex++;
                    break;
            }
        }
    }

    private Token CreateIdentifierOrKeywordToken()
    {
        char c = CurrentChar;
        while (char.IsAsciiLetterOrDigit(c) || c == '_')
        {
            _currentIndex++;
            c = CurrentChar;
        }

        var span = _source.AsSpan(_startIndex, _currentIndex - _startIndex);
        return span switch
        {
            "and" => CreateToken(SyntaxKind.AndKeyword),
            "else" => CreateToken(SyntaxKind.ElseKeyword),
            "enum" => CreateToken(SyntaxKind.EnumKeyword),
            "false" => CreateToken(SyntaxKind.FalseKeyword),
            "f32" => CreateToken(SyntaxKind.F32Keyword),
            "f64" => CreateToken(SyntaxKind.F64Keyword),
            "fun" => CreateToken(SyntaxKind.FunKeyword),
            "mod" => CreateToken(SyntaxKind.ModKeyword),
            "mut" => CreateToken(SyntaxKind.MutKeyword),
            "if" => CreateToken(SyntaxKind.IfKeyword),
            "let" => CreateToken(SyntaxKind.LetKeyword),
            "never" => CreateToken(SyntaxKind.NeverKeyword),
            "not" => CreateToken(SyntaxKind.NotKeyword),
            "or" => CreateToken(SyntaxKind.OrKeyword),
            "pkg" => CreateToken(SyntaxKind.PkgKeyword),
            "pub" => CreateToken(SyntaxKind.PubKeyword),
            "return" => CreateToken(SyntaxKind.ReturnKeyword),
            "s8" => CreateToken(SyntaxKind.S8Keyword),
            "s16" => CreateToken(SyntaxKind.S16Keyword),
            "s32" => CreateToken(SyntaxKind.S32Keyword),
            "s64" => CreateToken(SyntaxKind.S64Keyword),
            "self" => CreateToken(SyntaxKind.SelfKeyword),
            "struct" => CreateToken(SyntaxKind.StructKeyword),
            "true" => CreateToken(SyntaxKind.TrueKeyword),
            "u8" => CreateToken(SyntaxKind.U8Keyword),
            "u16" => CreateToken(SyntaxKind.U16Keyword),
            "u32" => CreateToken(SyntaxKind.U32Keyword),
            "u64" => CreateToken(SyntaxKind.U64Keyword),
            "unit" => CreateToken(SyntaxKind.UnitKeyword),
            "use" => CreateToken(SyntaxKind.UseKeyword),
            "while" => CreateToken(SyntaxKind.WhileKeyword),
            _ => _syntaxFactory.Identifier(_startIndex, _currentIndex - _startIndex)
        };
    }

    // number: '-'? [0-9]+ ('_' [0-9]+)*
    private Token CreateNumberToken()
    {
        while (true)
        {
            switch (CurrentChar)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    _currentIndex++;
                    break;

                case '_':
                    if (char.IsDigit(LookAheadChar))
                    {
                        _currentIndex++;
                        continue;
                    }

                    goto default;

                default:
                    return CreateToken(SyntaxKind.IntegerLiteral);
            }
        }
    }

    private Token CreateToken(SyntaxKind kind)
    {
        return _syntaxFactory.Token(kind, _startIndex, _currentIndex - _startIndex);
    }
}
