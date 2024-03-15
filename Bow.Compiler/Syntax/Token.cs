namespace Bow.Compiler.Syntax;

public enum TokenKind
{
    // Special
    Unknown = -1,
    EndOfFile,
    NewLine,

    // Keywords
    F32,
    F64,

    Enum,
    Fun,
    Mod,
    Mut,
    Never,
    Pub,

    S8,
    S16,
    S32,
    S64,

    Self,
    Struct,

    U8,
    U16,
    U32,
    U64,

    Unit,
    Use,

    // Literals
    Identifier,
    StringLiteral,
    UnterminatedStringLiteral,

    // Symbols
    Comma,
    Dot,
    OpenBrace,
    CloseBrace,
    OpenParenthesis,
    CloseParenthesis,
    Star
}

public enum ContextualKeywordKind
{
    None,
    Data,
    File,
    Self,
    Type
}

public class Token(SyntaxTree syntaxTree, TokenKind kind, Location location)
    : SyntaxNode(syntaxTree)
{
    public TokenKind Kind { get; } = kind;
    public override Location Location { get; } = location;

    public ContextualKeywordKind ContextualKeywordKind
    {
        get
        {
            if (Kind != TokenKind.Identifier)
            {
                return ContextualKeywordKind.None;
            }

            return GetText().Span switch
            {
                "data" => ContextualKeywordKind.Data,
                "file" => ContextualKeywordKind.File,
                "type" => ContextualKeywordKind.Type,
                _ => ContextualKeywordKind.None
            };
        }
    }
}

public sealed class IdentifierToken : Token
{
    public IdentifierToken(SyntaxTree syntaxTree, Location location)
        : base(syntaxTree, TokenKind.Identifier, location)
    {
        IdentifierText = ToString();
    }

    public string IdentifierText { get; }
}
