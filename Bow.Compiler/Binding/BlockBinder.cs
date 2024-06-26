using System.Numerics;
using Bow.Compiler.Diagnostics;
using Bow.Compiler.Symbols;
using Bow.Compiler.Syntax;

namespace Bow.Compiler.Binding;

internal sealed class BlockBinder : Binder
{
    private readonly FunctionSymbol _function;

    private readonly Stack<TypeSymbol> _ambientTypeStack = new();

    private BoundLabelGenerator _labelGenerator = new();
    private BoundLabel _breakLabel;
    private BoundLabel _continueLabel;

    public BlockBinder(Binder parent, FunctionSymbol function)
        : base(parent)
    {
        _function = function;
        _ambientTypeStack.Push(PlaceholderTypeSymbol.Instance);
    }

    private Dictionary<string, LocalSymbol>? _lazyLocals;
    private Dictionary<string, LocalSymbol> Locals => _lazyLocals ??= [];

    public override Symbol? Lookup(string name)
    {
        if (_lazyLocals == null)
        {
            return Parent.Lookup(name);
        }

        return Locals.TryGetValue(name, out var local) ? local : Parent.Lookup(name);
    }

    public BoundStatement BindStatement(StatementSyntax syntax, DiagnosticBag diagnostics)
    {
        return syntax.Kind switch
        {
            SyntaxKind.LocalDeclaration => BindLocalDeclaration((LocalDeclarationSyntax)syntax, diagnostics),
            SyntaxKind.BlockStatement => BindBlockStatement((BlockStatementSyntax)syntax, diagnostics),
            SyntaxKind.IfStatement => BindIfStatement((IfStatementSyntax)syntax, diagnostics),
            SyntaxKind.WhileStatement => BindWhileStatement((WhileStatementSyntax)syntax, diagnostics),
            SyntaxKind.BreakStatement => BindBreakStatement((BreakStatementSyntax)syntax, diagnostics),
            SyntaxKind.ContinueStatement => BindContinueStatement((ContinueStatementSyntax)syntax, diagnostics),
            SyntaxKind.ReturnStatement => BindReturnStatement((ReturnStatementSyntax)syntax, diagnostics),
            SyntaxKind.AssignmentStatement => BindAssignmentStatement((AssignmentStatementSyntax)syntax, diagnostics),
            SyntaxKind.ExpressionStatement => BindExpressionStatement((ExpressionStatementSyntax)syntax, diagnostics),
            _ => throw new UnreachableException()
        };
    }

    private static BoundExpression CreateImplicitCastExpression(BoundExpression expression, TypeSymbol type)
    {
        return expression.Type == type ? expression : new BoundCastExpression(expression.Syntax, expression, type);
    }

    private BoundLocalDeclaration BindLocalDeclaration(LocalDeclarationSyntax syntax, DiagnosticBag diagnostics)
    {
        var name = syntax.Identifier.IdentifierText;
        var type = syntax.Type == null ? PlaceholderTypeSymbol.ToBeInferred : BindType(syntax.Type, diagnostics);
        LocalSymbol local;
        if (syntax.Initializer == null)
        {
            LocalSymbolBuilder localBuilder = new(_function, syntax, type);
            local = new LateInitLocalSymbol(localBuilder);
            Locals.Add(name, local);
            return new BoundLocalDeclaration(syntax, local, null);
        }

        _ambientTypeStack.Push(type);
        var initializer = BindExpression(syntax.Initializer.Expression, diagnostics);
        _ambientTypeStack.Pop();

        if (type == PlaceholderTypeSymbol.ToBeInferred)
        {
            type = initializer.Type;
        }
        else if (initializer.Type.IsAssignableTo(type))
        {
            initializer = CreateImplicitCastExpression(initializer, type);
        }
        else
        {
            diagnostics.AddError(
                syntax.Initializer.Expression,
                DiagnosticMessages.TypeMismatch,
                type.Name,
                initializer.Type.Name
            );
        }

        local = new InitializedLocalSymbol(_function, syntax, type);
        Locals.Add(name, local);
        return new BoundLocalDeclaration(syntax, local, initializer);
    }

    private BoundBlockStatement BindBlockStatement(BlockStatementSyntax syntax, DiagnosticBag diagnostics)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>(syntax.Statements.Count);
        foreach (var statementSyntax in syntax.Statements)
        {
            var statement = BindStatement(statementSyntax, diagnostics);
            statements.Add(statement);
        }

        return new BoundBlockStatement(syntax, statements.MoveToImmutable());
    }

    private BoundIfStatement BindIfStatement(IfStatementSyntax syntax, DiagnosticBag diagnostics)
    {
        var condition = BindExpression(syntax.Condition, diagnostics);
        var then = BindBlockStatement(syntax.Then, diagnostics);
        var @else = syntax.Else == null ? null : BindStatement(syntax.Else.Body, diagnostics);
        return new BoundIfStatement(syntax, condition, then, @else);
    }

    private BoundWhileStatement BindWhileStatement(WhileStatementSyntax syntax, DiagnosticBag diagnostics)
    {
        var condition = BindExpression(syntax.Condition, diagnostics);

        var previousBreakLabel = _breakLabel;
        var breakLabel = _labelGenerator.GenerateLabel();
        _breakLabel = breakLabel;

        var previousContinueLabel = _continueLabel;
        var continueLabel = _labelGenerator.GenerateLabel();
        _continueLabel = continueLabel;

        var body = BindBlockStatement(syntax.Body, diagnostics);

        _breakLabel = previousBreakLabel;
        _continueLabel = previousContinueLabel;

        return new BoundWhileStatement(syntax, condition, body, breakLabel, continueLabel);
    }

    private BoundGotoStatement BindBreakStatement(BreakStatementSyntax syntax, DiagnosticBag diagnostics)
    {
        if (_breakLabel.IsDefault)
        {
            diagnostics.AddError(
                syntax,
                DiagnosticMessages.BreakOrContinueOutsideOfLoop,
                SyntaxFacts.GetKindDisplayText(syntax.Keyword.Kind)
            );
        }

        return new BoundGotoStatement(syntax, _breakLabel);
    }

    private BoundGotoStatement BindContinueStatement(ContinueStatementSyntax syntax, DiagnosticBag diagnostics)
    {
        if (_continueLabel.IsDefault)
        {
            diagnostics.AddError(
                syntax,
                DiagnosticMessages.BreakOrContinueOutsideOfLoop,
                SyntaxFacts.GetKindDisplayText(syntax.Keyword.Kind)
            );
        }

        return new BoundGotoStatement(syntax, _continueLabel);
    }

    private BoundReturnStatement BindReturnStatement(ReturnStatementSyntax syntax, DiagnosticBag diagnostics)
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

    private BoundAssignmentStatement BindAssignmentStatement(
        AssignmentStatementSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var assignee = BindExpression(syntax.Assignee, diagnostics);

        var isAssignable = TryGetReferencedSymbol(assignee, out var referencedSymbol);
        _ambientTypeStack.Push(isAssignable ? assignee.Type : PlaceholderTypeSymbol.Instance);

        var expression = BindExpression(syntax.Expression, diagnostics);

        _ambientTypeStack.Pop();

        if (!isAssignable)
        {
            diagnostics.AddError(syntax.Assignee, DiagnosticMessages.ExpressionIsNotAssignable);
            return new BoundAssignmentStatement(syntax, referencedSymbol, expression);
        }

        if (referencedSymbol is LateInitLocalSymbol { HasResolvedType: false } local)
        {
            local.Builder.Type = expression.Type;
            local.Builder.IsInitialized = true;
            return new BoundAssignmentStatement(syntax, referencedSymbol, expression);
        }

        if (!referencedSymbol.IsMutable)
        {
            diagnostics.AddError(syntax.Assignee, DiagnosticMessages.VariableIsImmutable, referencedSymbol.Name);
        }

        if (!expression.Type.IsAssignableTo(assignee.Type))
        {
            diagnostics.AddError(
                syntax.Expression,
                DiagnosticMessages.TypeMismatch,
                assignee.Type.Name,
                expression.Type.Name
            );
        }
        else
        {
            expression = CreateImplicitCastExpression(expression, assignee.Type);
        }

        return new BoundAssignmentStatement(syntax, referencedSymbol, expression);
    }

    private static bool TryGetReferencedSymbol(BoundExpression expression, out Symbol symbol)
    {
        switch (expression.Kind)
        {
            case BoundNodeKind.IdentifierExpression:
                symbol = ((BoundIdentifierExpression)expression).ReferencedSymbol;
                return true;
            default:
                symbol = PlaceholderSymbol.Instance;
                return false;
        }
    }

    private BoundExpressionStatement BindExpressionStatement(
        ExpressionStatementSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var expression = BindExpression(syntax.Expression, diagnostics);
        return new BoundExpressionStatement(syntax, expression);
    }

    private BoundExpression BindExpression(ExpressionSyntax syntax, DiagnosticBag diagnostics)
    {
        return syntax.Kind switch
        {
            SyntaxKind.MissingExpression => new BoundMissingExpression((MissingExpressionSyntax)syntax),
            SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)syntax, diagnostics),

            SyntaxKind.OpenParenthesisToken
                => BindExpression(((ParenthesizedExpressionSyntax)syntax).Expression, diagnostics),

            SyntaxKind.IdentifierExpression
                => BindIdentifierExpression((IdentifierExpressionSyntax)syntax, diagnostics),

            SyntaxKind.CallExpression => BindCallExpression((CallExpressionSyntax)syntax, diagnostics),
            SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)syntax, diagnostics),
            SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)syntax, diagnostics),

            SyntaxKind.StructCreationExpression
                => BindStructCreationExpression((StructCreationExpressionSyntax)syntax, diagnostics),

            _ => throw new UnreachableException()
        };
    }

    private BoundLiteralExpression BindLiteralExpression(LiteralExpressionSyntax syntax, DiagnosticBag diagnostics)
    {
        TypeSymbol type;
        var pushedDefaultTypeToAmbientStack = false;
        switch (syntax.Literal.Kind)
        {
            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
            {
                type = BuiltInPackage.BoolType;
                break;
            }

            case SyntaxKind.IntegerLiteral:
            {
                var ambientType = _ambientTypeStack.Peek();
                if (ambientType.IsNumericType())
                {
                    type = ambientType;
                    break;
                }

                bool shouldReportError =
                    ambientType != PlaceholderTypeSymbol.ToBeInferred && ambientType != PlaceholderTypeSymbol.Instance
                    || ambientType.PrimitiveTypeKind is PrimitiveTypeKind.Float32 or PrimitiveTypeKind.Float64;

                if (shouldReportError)
                {
                    diagnostics.AddError(
                        syntax.Literal,
                        DiagnosticMessages.TypeMismatch,
                        BuiltInPackage.Signed32Type.Name,
                        ambientType.Name
                    );
                }

                // The ambient type is not a numeric type, so we push s32 to the ambient type stack.
                // That way, we don't need to handle any special case when binding the literal
                _ambientTypeStack.Push(BuiltInPackage.Signed32Type);
                pushedDefaultTypeToAmbientStack = true;

                type = BuiltInPackage.Signed32Type;
                break;
            }

            default:
                throw new UnreachableException();
        }

        object value = syntax.Literal.Kind switch
        {
            SyntaxKind.TrueKeyword => true,
            SyntaxKind.FalseKeyword => false,
            SyntaxKind.IntegerLiteral => BindIntegerLiteral(syntax.Literal, diagnostics),
            _ => throw new UnreachableException()
        };

        if (pushedDefaultTypeToAmbientStack)
        {
            _ambientTypeStack.Pop();
        }

        return new BoundLiteralExpression(syntax, type, value);
    }

    private object BindIntegerLiteral(Token literal, DiagnosticBag diagnostics)
    {
        var parsed = BigInteger.TryParse(literal.GetText().Span, out var result);
        Debug.Assert(parsed);

        var expectedType = _ambientTypeStack.Peek();
        switch (expectedType.PrimitiveTypeKind)
        {
            case PrimitiveTypeKind.Signed8:
            {
                if (result >= sbyte.MinValue && result <= sbyte.MaxValue)
                {
                    return (sbyte)result;
                }

                break;
            }

            case PrimitiveTypeKind.Signed16:
            {
                if (result >= short.MinValue && result <= short.MaxValue)
                {
                    return (short)result;
                }

                break;
            }

            case PrimitiveTypeKind.Signed32:
            {
                if (result >= int.MinValue && result <= int.MaxValue)
                {
                    return (int)result;
                }

                break;
            }

            case PrimitiveTypeKind.Signed64:
            {
                if (result >= long.MinValue && result <= long.MaxValue)
                {
                    return (long)result;
                }

                break;
            }

            case PrimitiveTypeKind.Unsigned8:
            {
                if (result >= byte.MinValue && result <= byte.MaxValue)
                {
                    return (byte)result;
                }

                break;
            }

            case PrimitiveTypeKind.Unsigned16:
            {
                if (result >= ushort.MinValue && result <= ushort.MaxValue)
                {
                    return (ushort)result;
                }

                break;
            }

            case PrimitiveTypeKind.Unsigned32:
            {
                if (result >= uint.MinValue && result <= uint.MaxValue)
                {
                    return (uint)result;
                }

                break;
            }

            case PrimitiveTypeKind.Unsigned64:
            {
                if (result >= ulong.MinValue && result <= ulong.MaxValue)
                {
                    return (ulong)result;
                }

                break;
            }

            default:
                throw new UnreachableException();
        }

        diagnostics.AddError(literal, DiagnosticMessages.IntegerIsTooLargeForSize, expectedType.Name);
        return 0;
    }

    private BoundIdentifierExpression BindIdentifierExpression(
        IdentifierExpressionSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var name = syntax.Identifier.IdentifierText;
        Symbol? symbol = Lookup(name);
        if (symbol == null)
        {
            diagnostics.AddError(syntax.Identifier, DiagnosticMessages.NameNotFound, name);
            symbol = PlaceholderSymbol.Instance;
        }

        return new BoundIdentifierExpression(syntax, symbol);
    }

    private BoundCallExpression BindCallExpression(CallExpressionSyntax syntax, DiagnosticBag diagnostics)
    {
        var expression = BindExpression(syntax.Callee, diagnostics);
        FunctionSymbol function;
        if (expression.Type is FunctionTypeSymbol functionType)
        {
            function = functionType.Function;
        }
        else
        {
            function = PlaceholderFunctionSymbol.Instance;
            if (expression.Type != PlaceholderTypeSymbol.Instance)
            {
                diagnostics.AddError(syntax.Callee, DiagnosticMessages.ExpressionNotCallable, expression.Type.Name);
            }
        }

        if (syntax.Arguments.Count != function.Parameters.Length && function != PlaceholderFunctionSymbol.Instance)
        {
            diagnostics.AddError(
                syntax,
                DiagnosticMessages.ArgumentCountMismatch,
                function.Parameters.Length.ToString(),
                syntax.Arguments.Count.ToString()
            );
        }

        var arguments = ImmutableArray.CreateBuilder<BoundExpression>(syntax.Arguments.Count);
        for (var i = 0; i < syntax.Arguments.Count; i++)
        {
            var parameter = function.Parameters.ElementAtOrDefault(i);
            _ambientTypeStack.Push(parameter == null ? PlaceholderTypeSymbol.Instance : parameter.Type);

            var argumentSyntax = syntax.Arguments[i];
            var argument = BindExpression(argumentSyntax, diagnostics);
            arguments.Add(argument);
            _ambientTypeStack.Pop();
        }

        return new BoundCallExpression(syntax, function, arguments.MoveToImmutable());
    }

    private BoundUnaryExpression BindUnaryExpression(UnaryExpressionSyntax syntax, DiagnosticBag diagnostics)
    {
        var operand = BindExpression(syntax.Operand, diagnostics);
        BoundOperator @operator;
        if (operand.Type == PlaceholderTypeSymbol.Instance)
        {
            @operator = BoundOperator.CreateErrorUnaryOperator(syntax.Operator.Kind);
            return new BoundUnaryExpression(syntax, @operator, operand);
        }

        if (BoundOperator.TryBindUnaryOperator(syntax.Operator.Kind, operand.Type, out @operator))
        {
            operand = CreateImplicitCastExpression(operand, @operator.OperandType);
        }
        else
        {
            diagnostics.AddError(
                syntax.Operator,
                DiagnosticMessages.UnaryOperatorTypeMismatch,
                SyntaxFacts.GetKindDisplayText(syntax.Operator.Kind),
                operand.Type.Name
            );
        }

        return new BoundUnaryExpression(syntax, @operator, operand);
    }

    private BoundBinaryExpression BindBinaryExpression(BinaryExpressionSyntax syntax, DiagnosticBag diagnostics)
    {
        var left = BindExpression(syntax.Left, diagnostics);
        var right = BindExpression(syntax.Right, diagnostics);
        BoundOperator @operator;
        if (left.Type.IsPlaceholder || right.Type.IsPlaceholder)
        {
            @operator = BoundOperator.CreateErrorBinaryOperator(syntax.Operator.Kind);
            return new BoundBinaryExpression(syntax, left, @operator, right);
        }

        if (BoundOperator.TryBindBinaryOperator(syntax.Operator.Kind, left.Type, right.Type, out @operator))
        {
            left = CreateImplicitCastExpression(left, @operator.OperandType);
            right = CreateImplicitCastExpression(right, @operator.OperandType);
        }
        else
        {
            diagnostics.AddError(
                syntax.Operator,
                DiagnosticMessages.BinaryOperatorTypeMismatch,
                SyntaxFacts.GetKindDisplayText(syntax.Operator.Kind),
                left.Type.Name,
                right.Type.Name
            );
        }

        return new BoundBinaryExpression(syntax, left, @operator, right);
    }

    private BoundStructCreationExpression BindStructCreationExpression(
        StructCreationExpressionSyntax syntax,
        DiagnosticBag diagnostics
    )
    {
        var expectedType = _ambientTypeStack.Peek();
        StructSymbol? @struct = expectedType as StructSymbol;
        if (@struct == null)
        {
            diagnostics.AddError(syntax, DiagnosticMessages.CouldNotInferStructType);
        }

        var fieldInitializers = ImmutableArray.CreateBuilder<BoundFieldInitializer>(syntax.FieldInitializers.Count);
        foreach (var fieldInitializerSyntax in syntax.FieldInitializers)
        {
            var name = fieldInitializerSyntax.Identifier.IdentifierText;
            FieldSymbol? field = @struct?.Fields.FindByName(name);
            if (@struct != null && field == null)
            {
                diagnostics.AddError(
                    fieldInitializerSyntax.Identifier,
                    DiagnosticMessages.NameIsNotAMemberOfType,
                    name,
                    @struct.Name
                );
            }

            BoundExpression initializer;
            if (fieldInitializerSyntax.Initializer == null)
            {
                IdentifierExpressionSyntax identifierSyntax =
                    new(fieldInitializerSyntax.SyntaxTree, fieldInitializerSyntax.Identifier);

                initializer = BindIdentifierExpression(identifierSyntax, diagnostics);
            }
            else
            {
                initializer = BindExpression(fieldInitializerSyntax.Initializer.Expression, diagnostics);
            }

            if (field == null)
            {
                continue;
            }

            if (!initializer.Type.IsAssignableTo(field.Type))
            {
                diagnostics.AddError(
                    (SyntaxNode?)fieldInitializerSyntax.Initializer?.Operator ?? fieldInitializerSyntax.Identifier,
                    DiagnosticMessages.TypeMismatch,
                    field.Type.Name,
                    initializer.Type.Name
                );
            }
            else
            {
                initializer = CreateImplicitCastExpression(initializer, field.Type);
            }

            BoundFieldInitializer fieldInitializer = new(field, initializer);
            fieldInitializers.Add(fieldInitializer);
        }

        var type = (TypeSymbol?)@struct ?? PlaceholderTypeSymbol.Instance;
        return new BoundStructCreationExpression(syntax, type, fieldInitializers.MoveToImmutable());
    }
}
