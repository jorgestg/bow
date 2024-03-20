namespace Bow.Compiler.Syntax;

public enum TokenKind
{
    // Special
    Unknown = -1,
    EndOfFile,
    NewLine,

    // Keywords
    Enum,
    False,

    F32,
    F64,

    Fun,
    Mod,
    Mut,
    Never,
    Pub,
    Return,

    S8,
    S16,
    S32,
    S64,

    Self,
    Struct,
    True,

    U8,
    U16,
    U32,
    U64,

    Unit,
    Use,

    // Literals
    Identifier,
    IntegerLiteral,
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
    Type
}

public class Token(SyntaxTree syntaxTree, TokenKind kind, Location location, bool isMissing)
    : SyntaxNode(syntaxTree)
{
    public TokenKind Kind { get; } = kind;
    public override Location Location { get; } = location;
    public override bool IsMissing { get; } = isMissing;

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
    public IdentifierToken(SyntaxTree syntaxTree, Location location, bool isMissing)
        : base(syntaxTree, TokenKind.Identifier, location, isMissing)
    {
        IdentifierText = ToString();
    }

    public string IdentifierText { get; }
}
