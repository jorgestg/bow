namespace Bow.Compiler.Syntax;

public enum SyntaxKind
{
    // Special tokens
    UnknownToken = -1,
    EndOfFileToken,
    NewLineToken,

    // Keyword tokens
    AndKeyword,
    EnumKeyword,
    FalseKeyword,
    F32Keyword,
    F64Keyword,
    FunKeyword,
    ModKeyword,
    MutKeyword,
    NeverKeyword,
    NotKeyword,
    OrKeyword,
    PkgKeyword,
    PubKeyword,
    ReturnKeyword,
    S8Keyword,
    S16Keyword,
    S32Keyword,
    S64Keyword,
    SelfKeyword,
    StructKeyword,
    TrueKeyword,
    U8Keyword,
    U16Keyword,
    U32Keyword,
    U64Keyword,
    UnitKeyword,
    UseKeyword,

    // Literal tokens
    IdentifierToken,
    IntegerLiteral,
    StringLiteral,
    UnterminatedStringLiteral,

    // Delimiter tokens
    CommaToken,
    DotToken,
    OpenBraceToken,
    CloseBraceToken,
    OpenParenthesisToken,
    CloseParenthesisToken,

    // Operator tokens
    StarToken,
    SlashToken,
    PercentToken,
    PlusToken,
    MinusToken,
    GreaterThanToken,
    GreaterThanEqualToken,
    LessThanToken,
    LessThanEqualToken,
    EqualEqualToken,
    DiamondToken,
    AmpersandToken,
    PipeToken,
    EqualsToken,

    // Top level nodes
    CompilationUnit,
    ModClause,
    UseClause,

    // Name nodes
    SimpleName,
    QualifiedName,
    NamedTypeReference,

    // Type reference nodes
    MissingTypeReference,
    KeywordTypeReference,
    PointerTypeReference,

    // Definition/declaration nodes
    StructDefinition,
    FieldDeclaration,
    EnumDefinition,
    EnumCaseDeclaration,
    EnumCaseArgument,
    FunctionDefinition,
    SimpleParameterDeclaration,
    SelfParameterDeclaration,

    // Statement nodes
    BlockStatement,
    ExpressionStatement,
    ReturnStatement,

    // Expression nodes
    MissingExpression,
    LiteralExpression,
    UnaryExpression,
    BinaryExpression,
}
