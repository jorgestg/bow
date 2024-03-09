namespace Bow.Compiler.Syntax;

public enum TokenKind
{
    // Special
    Unknown = -1,
    EndOfFile,

    // Keywords
    F32,
    F64,

    Enum,
    Func,
    Mut,
    Pub,

    S8,
    S16,
    S32,
    S64,

    Struct,

    U8,
    U16,
    U32,
    U64,

    Unit,
    Use,

    // Literals
    StringLiteral,
    Identifier,

    // Symbols
    LeftBrace,
    RightBrace,
    Comma,
    LeftParenthesis,
    RightParenthesis,
    Ampersand
}

public enum ContextualKeywordKind
{
    None,
    Data,
    File,
    Mod,
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
                "mod" => ContextualKeywordKind.Mod,
                "self" => ContextualKeywordKind.Self,
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
