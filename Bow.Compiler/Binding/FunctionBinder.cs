using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class FunctionBinder(FunctionSymbol function) : Binder(GetParentBinder(function))
{
    private readonly FunctionSymbol _function = function;

    public override Symbol? Lookup(string name)
    {
        return _function.ParameterMap.TryGetValue(name, out var symbol)
            ? symbol
            : Parent.Lookup(name);
    }

    public BoundBlockStatement BindBlockStatement(
        BlockStatementSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>(syntax.Statements.Count);
        foreach (var statementSyntax in syntax.Statements)
        {
            var statement = BindStatement(statementSyntax, diagnostics);
            statements.Add(statement);
        }

        return new BoundBlockStatement(syntax, statements.MoveToImmutable());
    }

    public BoundStatement BindStatement(StatementSyntax syntax, DiagnosticBag diagnostics)
    {
        return syntax.Kind switch
        {
            SyntaxKind.BlockStatement
                => BindBlockStatement((BlockStatementSyntax)syntax, diagnostics),

            SyntaxKind.ReturnStatement
                => BindReturnStatement((ReturnStatementSyntax)syntax, diagnostics),

            SyntaxKind.ExpressionStatement
                => BindExpressionStatement((ExpressionStatementSyntax)syntax, diagnostics),

            _ => throw new UnreachableException()
        };
    }

    public BoundExpression BindExpression(ExpressionSyntax syntax, DiagnosticBag diagnostics)
    {
        return syntax.Kind switch
        {
            SyntaxKind.MissingExpression
                => new BoundMissingExpression((MissingExpressionSyntax)syntax),

            SyntaxKind.LiteralExpression
                => BindLiteralExpression((LiteralExpressionSyntax)syntax, diagnostics),

            SyntaxKind.UnaryExpression
                => BindUnaryExpression((UnaryExpressionSyntax)syntax, diagnostics),

            SyntaxKind.BinaryExpression
                => BindBinaryExpression((BinaryExpressionSyntax)syntax, diagnostics),

            _ => throw new UnreachableException()
        };
    }

    private BoundUnaryExpression BindUnaryExpression(
        UnaryExpressionSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var operand = BindExpression(syntax.Operand, diagnostics);
        var (@operator, type) = BindUnaryOperator(syntax, operand.Type, diagnostics);
        return new BoundUnaryExpression(syntax, @operator, operand, type);
    }

    private static (BoundUnaryOperatorKind, TypeSymbol) BindUnaryOperator(
        UnaryExpressionSyntax syntax,
        TypeSymbol operandType,
        DiagnosticBag diagnostics
    )
    {
        switch (syntax.Kind)
        {
            case SyntaxKind.MinusToken:
            {
                if (!operandType.IsNumericType())
                {
                    diagnostics.AddError(
                        syntax,
                        DiagnosticMessages.UnaryOperatorTypeMismatch,
                        SyntaxFacts.GetKindDisplayText(syntax.Kind),
                        operandType.Name
                    );

                    operandType = new MissingTypeSymbol(syntax);
                }

                return (BoundUnaryOperatorKind.Negation, operandType);
            }

            case SyntaxKind.NotKeyword:
            {
                if (operandType != BuiltInModule.Bool)
                {
                    diagnostics.AddError(
                        syntax,
                        DiagnosticMessages.UnaryOperatorTypeMismatch,
                        SyntaxFacts.GetKindDisplayText(syntax.Kind),
                        BuiltInModule.Bool.Name
                    );
                }

                return (BoundUnaryOperatorKind.LogicalNegation, BuiltInModule.Bool);
            }

            default:
                throw new UnreachableException();
        }
    }

    private BoundBinaryExpression BindBinaryExpression(
        BinaryExpressionSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var left = BindExpression(syntax.Left, diagnostics);
        var right = BindExpression(syntax.Right, diagnostics);
        var (@operator, type) = BindBinaryOperator(syntax, left.Type, right.Type, diagnostics);
        return new BoundBinaryExpression(syntax, left, @operator, right, type);
    }

    private static (BoundBinaryOperatorKind, TypeSymbol) BindBinaryOperator(
        BinaryExpressionSyntax syntax,
        TypeSymbol leftType,
        TypeSymbol rightType,
        DiagnosticBag diagnostics
    )
    {
        var kind = syntax.Kind switch
        {
            SyntaxKind.StarToken => BoundBinaryOperatorKind.Multiplication,
            SyntaxKind.SlashToken => BoundBinaryOperatorKind.Division,
            SyntaxKind.PercentToken => BoundBinaryOperatorKind.Modulo,
            SyntaxKind.PlusToken => BoundBinaryOperatorKind.Addition,
            SyntaxKind.MinusToken => BoundBinaryOperatorKind.Subtraction,
            SyntaxKind.GreaterThanToken => BoundBinaryOperatorKind.Greater,
            SyntaxKind.GreaterThanEqualsToken => BoundBinaryOperatorKind.GreaterOrEqual,
            SyntaxKind.LessThanToken => BoundBinaryOperatorKind.Less,
            SyntaxKind.LessThanEqualsToken => BoundBinaryOperatorKind.LessOrEqual,
            SyntaxKind.EqualsEqualsToken => BoundBinaryOperatorKind.Equals,
            SyntaxKind.DiamondToken => BoundBinaryOperatorKind.NotEquals,
            SyntaxKind.AmpersandToken => BoundBinaryOperatorKind.BitwiseAnd,
            SyntaxKind.PipeToken => BoundBinaryOperatorKind.BitwiseOr,
            _ => throw new UnreachableException()
        };

        switch (kind)
        {
            case BoundBinaryOperatorKind.Multiplication:
            case BoundBinaryOperatorKind.Division:
            case BoundBinaryOperatorKind.Modulo:
            case BoundBinaryOperatorKind.Addition:
            case BoundBinaryOperatorKind.Subtraction:
            {
                if (!leftType.IsNumericType() || !rightType.IsNumericType())
                {
                    diagnostics.AddError(
                        syntax,
                        DiagnosticMessages.BinaryOperatorTypeMismatch,
                        SyntaxFacts.GetKindDisplayText(syntax.Kind),
                        leftType.Name,
                        rightType.Name
                    );

                    return (kind, new MissingTypeSymbol(syntax));
                }

                return (kind, leftType);
            }

            case BoundBinaryOperatorKind.Greater:
            case BoundBinaryOperatorKind.GreaterOrEqual:
            case BoundBinaryOperatorKind.Less:
            case BoundBinaryOperatorKind.LessOrEqual:
            {
                if (!leftType.IsNumericType() || !rightType.IsNumericType())
                {
                    diagnostics.AddError(
                        syntax,
                        DiagnosticMessages.BinaryOperatorTypeMismatch,
                        SyntaxFacts.GetKindDisplayText(syntax.Kind),
                        leftType.Name,
                        rightType.Name
                    );

                    return (kind, new MissingTypeSymbol(syntax));
                }

                return (kind, BuiltInModule.Bool);
            }

            case BoundBinaryOperatorKind.Equals:
            case BoundBinaryOperatorKind.NotEquals:
            {
                if (leftType != rightType)
                {
                    diagnostics.AddError(
                        syntax,
                        DiagnosticMessages.BinaryOperatorTypeMismatch,
                        SyntaxFacts.GetKindDisplayText(syntax.Kind),
                        leftType.Name,
                        rightType.Name
                    );

                    return (kind, new MissingTypeSymbol(syntax));
                }

                return (kind, BuiltInModule.Bool);
            }

            case BoundBinaryOperatorKind.BitwiseAnd:
            case BoundBinaryOperatorKind.BitwiseOr:
            {
                if (leftType != BuiltInModule.Signed32 || rightType != BuiltInModule.Signed32)
                {
                    diagnostics.AddError(
                        syntax,
                        DiagnosticMessages.BinaryOperatorTypeMismatch,
                        SyntaxFacts.GetKindDisplayText(syntax.Kind),
                        leftType.Name,
                        rightType.Name
                    );

                    return (kind, new MissingTypeSymbol(syntax));
                }

                return (kind, BuiltInModule.Signed32);
            }

            default:
                throw new UnreachableException();
        }
    }

    private static FileBinder GetParentBinder(FunctionSymbol function)
    {
        switch (function)
        {
            case FunctionItemSymbol i:
                return GetFileBinder(i);

            case MethodSymbol m:
            {
                return m.Container switch
                {
                    EnumSymbol e => GetFileBinder(e),
                    StructSymbol s => GetFileBinder(s),
                    _ => throw new UnreachableException()
                };
            }
        }

        throw new UnreachableException();
    }

    private BoundReturnStatement BindReturnStatement(
        ReturnStatementSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        if (syntax.Expression == null)
        {
            if (_function.ReturnType != BuiltInModule.Unit)
            {
                diagnostics.AddError(
                    syntax.Keyword,
                    DiagnosticMessages.ReturnExpressionExpected,
                    _function.ReturnType.Name
                );
            }

            return new BoundReturnStatement(syntax, expression: null);
        }

        var expression = BindExpression(syntax.Expression, diagnostics);
        if (expression.Type != _function.ReturnType)
        {
            diagnostics.AddError(
                syntax.Expression,
                DiagnosticMessages.ReturnTypeMismatch,
                _function.ReturnType.Name,
                expression.Type.Name
            );
        }

        return new BoundReturnStatement(syntax, expression);
    }

    private BoundExpressionStatement BindExpressionStatement(
        ExpressionStatementSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var expression = BindExpression(syntax.Expression, diagnostics);
        return new BoundExpressionStatement(syntax, expression);
    }

    private static BoundLiteralExpression BindLiteralExpression(
        LiteralExpressionSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        TypeSymbol type = syntax.Literal.Kind switch
        {
            SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword => BuiltInModule.Bool,
            SyntaxKind.IntegerLiteral => BuiltInModule.Signed32,
            _ => throw new UnreachableException()
        };

        object value = syntax.Literal.Kind switch
        {
            SyntaxKind.TrueKeyword => true,
            SyntaxKind.FalseKeyword => false,
            SyntaxKind.IntegerLiteral => BindIntegerLiteral(syntax.Literal, diagnostics),
            _ => throw new UnreachableException()
        };

        return new BoundLiteralExpression(syntax, type, value);
    }

    private static int BindIntegerLiteral(Token literal, DiagnosticBag diagnostics)
    {
        if (int.TryParse(literal.GetText().Span, out var value))
        {
            return value;
        }

        diagnostics.AddError(
            literal,
            DiagnosticMessages.IntegerIsTooLargeForSize,
            BuiltInModule.Signed32.Name
        );

        return 0;
    }
}
