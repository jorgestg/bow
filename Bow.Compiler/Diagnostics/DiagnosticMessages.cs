namespace Bow.Compiler.Diagnostics;

internal static class DiagnosticMessages
{
    public const string NameNotFound = "The name '{0}' could not be found.";
    public const string NameNotFoundIn = "The name '{0}' could not be found in '{1}'.";
    public const string NameIsNotAModule = "'{0}' is not a module.";
    public const string NameIsNotAType = "'{0}' is not a type.";
    public const string NameIsAlreadyDefined = "'{0}' is already defined.";
    public const string SymbolIsNotAccessible = "'{0}' is not accessible from this context.";
    public const string TokenMismatch = "'{0}' expected, but got '{1}'.";
    public const string TypeNameExpected = "Type name expected.";
    public const string ItemExpected = "Type or function expected.";
    public const string MemberExpected = "Field or method expected.";

    public const string SelfParameterCannotHaveAType =
        "'self' parameters on methods cannot be type-annotated.";

    public const string ExpressionExpected = "Expression expected.";

    public const string BraceShouldGoOnTheSameLine =
        "'{' should go on the same line as the declaration.";
}
