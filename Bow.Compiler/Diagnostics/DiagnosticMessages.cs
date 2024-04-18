namespace Bow.Compiler.Diagnostics;

internal static class DiagnosticMessages
{
    public const string NameNotFound = "The name '{0}' could not be found.";
    public const string NameNotFoundIn = "The name '{0}' could not be found in '{1}'.";
    public const string NameIsNotAPackage = "'{0}' is not a package.";
    public const string NameIsNotAModule = "'{0}' is not a module.";
    public const string NameIsNotAType = "'{0}' is not a type.";
    public const string NameIsAlreadyDefined = "'{0}' is already defined.";
    public const string SymbolIsNotAccessible = "'{0}' is not accessible from this context.";
    public const string TokenMismatch = "'{0}' expected, but got '{1}'.";
    public const string TypeNameExpected = "Type name expected.";
    public const string AccessModifierAlreadySpecified = "The access modifier '{0}' is already present.";
    public const string ItemExpected = "Type or function expected.";
    public const string MemberExpected = "Field or method expected.";
    public const string SelfParameterCannotHaveAType = "'self' parameters on methods cannot be type-annotated.";
    public const string ExpressionExpected = "Expression expected.";
    public const string BraceShouldGoOnTheSameLine = "'{' should go on the same line as the declaration or statement.";
    public const string IntegerIsTooLargeForSize = "Integer is too large for '{0}'.";
    public const string ReturnExpressionExpected = "Return expression of type '{0}' expected.";
    public const string TypeMismatch = "Type mismatch: '{0}' expected, but got '{1}'.";
    public const string ReturnTypeMismatch =
        "The function return type is '{0}', but the return expression is of type '{1}'.";

    public const string UnaryOperatorTypeMismatch = "Cannot apply operator '{0}' to operand of type '{1}'.";
    public const string BinaryOperatorTypeMismatch = "Cannot apply operator '{0}' to operands of type '{1}' and '{2}'.";
    public const string ExpressionNotCallable = "Expression is not callable.";
    public const string ArgumentCountMismatch = "Expected {0} arguments, but got {1}.";
    public const string ExpressionIsNotAssignable = "Expression is not assignable.";
    public const string LocalVariableIsNotInitialized = "Local variable '{0}' is not initialized.";
    public const string VariableIsImmutable = "'{0}' is immutable.";
    public const string CouldNotInferStructType = "Could not infer struct type.";
    public const string NameIsNotAMemberOfType = "'{0}' is not a member of '{1}'.";
    public const string BreakOrContinueOutsideOfLoop = "'{0}' can only be used inside of loops.";
}
