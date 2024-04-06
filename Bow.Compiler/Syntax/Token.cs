namespace Bow.Compiler.Syntax;

public enum ContextualKeywordKind
{
    None,
    Data,
    File,
    Type
}

public class Token(SyntaxTree syntaxTree, SyntaxKind kind, Location location, bool isMissing)
    : SyntaxNode(syntaxTree)
{
    public override SyntaxKind Kind { get; } = kind;
    public override Location Location { get; } = location;
    public override bool IsMissing { get; } = isMissing;

    public ContextualKeywordKind ContextualKeywordKind
    {
        get
        {
            if (Kind != SyntaxKind.IdentifierToken)
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
        : base(syntaxTree, SyntaxKind.IdentifierToken, location, isMissing)
    {
        IdentifierText = ToString();
    }

    public string IdentifierText { get; }
}
