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

    private static BoundExpression CreateImplicitCastExpression(
        BoundExpression expression,
        TypeSymbol type
    )
    {
        return expression.Type == type
            ? expression
            : new BoundCastExpression(expression.Syntax, expression, type);
    }

    private BoundUnaryExpression BindUnaryExpression(
        UnaryExpressionSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var operand = BindExpression(syntax.Operand, diagnostics);
        var @operator = BoundOperator.UnaryOperatorFor(syntax.Operator.Kind, operand.Type);
        if (@operator.OperandType == MissingTypeSymbol.Instance)
        {
            diagnostics.AddError(
                syntax.Operator,
                DiagnosticMessages.UnaryOperatorTypeMismatch,
                SyntaxFacts.GetKindDisplayText(syntax.Operator.Kind),
                operand.Type.Name
            );
        }
        else
        {
            operand = CreateImplicitCastExpression(operand, @operator.OperandType);
        }

        return new BoundUnaryExpression(syntax, @operator, operand);
    }

    private BoundBinaryExpression BindBinaryExpression(
        BinaryExpressionSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var left = BindExpression(syntax.Left, diagnostics);
        var right = BindExpression(syntax.Right, diagnostics);
        var @operator = BoundOperator.BinaryOperatorFor(
            syntax.Operator.Kind,
            left.Type,
            right.Type
        );

        if (@operator.ResultType.IsMissing)
        {
            diagnostics.AddError(
                syntax.Operator,
                DiagnosticMessages.BinaryOperatorTypeMismatch,
                SyntaxFacts.GetKindDisplayText(syntax.Operator.Kind),
                left.Type.Name,
                right.Type.Name
            );
        }
        else
        {
            left = CreateImplicitCastExpression(left, @operator.OperandType);
            right = CreateImplicitCastExpression(right, @operator.OperandType);
        }

        return new BoundBinaryExpression(syntax, left, @operator, right);
    }

    private static FileBinder GetParentBinder(FunctionSymbol function)
    {
        return function switch
        {
            FunctionItemSymbol i => GetFileBinder(i),
            MethodSymbol m => GetFileBinder((IItemSymbol)m.Container),
            _ => throw new UnreachableException(),
        };
    }

    private BoundReturnStatement BindReturnStatement(
        ReturnStatementSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        if (syntax.Expression == null)
        {
            if (_function.ReturnType != BuiltInPackage.UnitType)
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
        if (!expression.Type.IsAssignableTo(_function.ReturnType))
        {
            diagnostics.AddError(
                syntax.Expression,
                DiagnosticMessages.ReturnTypeMismatch,
                _function.ReturnType.Name,
                expression.Type.Name
            );
        }
        else
        {
            expression = CreateImplicitCastExpression(expression, _function.ReturnType);
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
            SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword => BuiltInPackage.BoolType,
            SyntaxKind.IntegerLiteral => BuiltInPackage.Signed32Type,
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
            BuiltInPackage.Signed32Type.Name
        );

        return 0;
    }
}
