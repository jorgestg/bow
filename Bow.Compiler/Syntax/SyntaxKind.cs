namespace Bow.Compiler.Syntax;

public enum SyntaxKind
{
    // Special tokens
    UnknownToken = -1,
    EndOfFileToken,
    NewLineToken,

    // Keyword tokens
    AndKeyword,
    ContinueKeyword,
    BreakKeyword,
    ElseKeyword,
    EnumKeyword,
    FalseKeyword,
    F32Keyword,
    F64Keyword,
    FunKeyword,
    IfKeyword,
    LetKeyword,
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
    WhileKeyword,

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
    MethodDefinition,
    EnumDefinition,
    EnumCaseDeclaration,
    EnumCaseArgument,
    FunctionDefinition,
    SimpleParameterDeclaration,
    SelfParameterDeclaration,

    // Other nodes
    Initializer,

    // Statement nodes
    LocalDeclaration,
    BlockStatement,
    IfStatement,
    ElseBlock,
    WhileStatement,
    BreakStatement,
    ContinueStatement,
    ReturnStatement,
    AssignmentStatement,
    ExpressionStatement,

    // Expression nodes
    MissingExpression,
    LiteralExpression,
    ParenthesizedExpression,
    IdentifierExpression,
    CallExpression,
    UnaryExpression,
    BinaryExpression,
    StructCreationExpression,
    StructCreationFieldInitializer,
}
