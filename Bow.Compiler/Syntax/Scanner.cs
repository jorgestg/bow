namespace Bow.Compiler.Syntax;

internal sealed class Scanner(SyntaxFactory syntaxFactory)
{
    private readonly SyntaxFactory _syntaxFactory = syntaxFactory;
    private readonly string _source = syntaxFactory.SyntaxTree.SourceText.Text;

    private int _startIndex;
    private int _currentIndex;

    private char CurrentChar => _source.Length > _currentIndex ? _source[_currentIndex] : '\0';
    private char LookAheadChar =>
        _source.Length > _currentIndex + 1 ? _source[_currentIndex + 1] : '\0';

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
            '\0' => CreateToken(TokenKind.EndOfFile),
            '\n' => CreateToken(TokenKind.NewLine),
            '{' => CreateToken(TokenKind.OpenBrace),
            '}' => CreateToken(TokenKind.CloseBrace),
            ',' => CreateToken(TokenKind.Comma),
            '.' => CreateToken(TokenKind.Dot),
            '(' => CreateToken(TokenKind.OpenParenthesis),
            ')' => CreateToken(TokenKind.CloseParenthesis),
            '*' => CreateToken(TokenKind.Star),
            '"' => CreateStringLiteralToken(),
            _ when char.IsAsciiLetter(c) => CreateIdentifierOrKeywordToken(),
            _ when char.IsDigit(c) => CreateNumberToken(),
            _ => CreateToken(TokenKind.Unknown)
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
                    return newLineIndex == -1
                        ? null
                        : _syntaxFactory.Token(TokenKind.NewLine, newLineIndex, 1);
            }
        }
    }

    public Token CreateStringLiteralToken()
    {
        while (true)
        {
            switch (CurrentChar)
            {
                case '\0':
                case '\n':
                    return CreateToken(TokenKind.UnterminatedStringLiteral);

                case '"':
                    // Eat closing "
                    _currentIndex++;
                    return CreateToken(TokenKind.StringLiteral);

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
            "enum" => CreateToken(TokenKind.Enum),
            "false" => CreateToken(TokenKind.False),
            "f32" => CreateToken(TokenKind.F32),
            "f64" => CreateToken(TokenKind.F64),
            "fun" => CreateToken(TokenKind.Fun),
            "mod" => CreateToken(TokenKind.Mod),
            "mut" => CreateToken(TokenKind.Mut),
            "never" => CreateToken(TokenKind.Never),
            "pkg" => CreateToken(TokenKind.Pkg),
            "pub" => CreateToken(TokenKind.Pub),
            "return" => CreateToken(TokenKind.Return),
            "s8" => CreateToken(TokenKind.S8),
            "s16" => CreateToken(TokenKind.S16),
            "s32" => CreateToken(TokenKind.S32),
            "s64" => CreateToken(TokenKind.S64),
            "self" => CreateToken(TokenKind.Self),
            "struct" => CreateToken(TokenKind.Struct),
            "true" => CreateToken(TokenKind.True),
            "u8" => CreateToken(TokenKind.U8),
            "u16" => CreateToken(TokenKind.U16),
            "u32" => CreateToken(TokenKind.U32),
            "u64" => CreateToken(TokenKind.U64),
            "unit" => CreateToken(TokenKind.Unit),
            "use" => CreateToken(TokenKind.Use),
            // "let" => CreateToken(TokenKind.Let),
            // "if" => CreateToken(TokenKind.If),
            // "else" => CreateToken(TokenKind.Else),
            // "extern" => CreateToken(TokenKind.Extern),
            // "as" => CreateToken(TokenKind.As),
            // "const" => CreateToken(TokenKind.Const),
            _ => _syntaxFactory.Identifier(_startIndex, _currentIndex - _startIndex)
        };
    }

    // number: [0-9]+ ('_' [0-9]+)*
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
                    return CreateToken(TokenKind.IntegerLiteral);
            }
        }
    }

    private Token CreateToken(TokenKind kind)
    {
        return _syntaxFactory.Token(kind, _startIndex, _currentIndex - _startIndex);
    }
}
