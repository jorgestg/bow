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
        var newLine = SkipWhitespace();
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
                    newLineIndex = _currentIndex;
                    _currentIndex++;
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
            "f32" => CreateToken(TokenKind.F32),
            "f64" => CreateToken(TokenKind.F64),
            "enum" => CreateToken(TokenKind.Enum),
            "fun" => CreateToken(TokenKind.Fun),
            "mut" => CreateToken(TokenKind.Mut),
            "never" => CreateToken(TokenKind.Never),
            "pub" => CreateToken(TokenKind.Pub),
            "s8" => CreateToken(TokenKind.S8),
            "s16" => CreateToken(TokenKind.S16),
            "s32" => CreateToken(TokenKind.S32),
            "s64" => CreateToken(TokenKind.S64),
            "struct" => CreateToken(TokenKind.Struct),
            "u8" => CreateToken(TokenKind.U8),
            "u16" => CreateToken(TokenKind.U16),
            "u32" => CreateToken(TokenKind.U32),
            "u64" => CreateToken(TokenKind.U64),
            "unit" => CreateToken(TokenKind.Unit),
            "use" => CreateToken(TokenKind.Use),
            // "let" => CreateToken(TokenKind.Let),
            // "if" => CreateToken(TokenKind.If),
            // "else" => CreateToken(TokenKind.Else),
            // "true" => CreateToken(TokenKind.True),
            // "false" => CreateToken(TokenKind.False),
            // "return" => CreateToken(TokenKind.Return),
            // "extern" => CreateToken(TokenKind.Extern),
            // "as" => CreateToken(TokenKind.As),
            // "const" => CreateToken(TokenKind.Const),
            _ => _syntaxFactory.Identifier(_startIndex, _currentIndex - _startIndex)
        };
    }

    private Token CreateToken(TokenKind kind)
    {
        return _syntaxFactory.Token(kind, _startIndex, _currentIndex - _startIndex);
    }
}
