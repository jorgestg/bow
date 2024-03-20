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

    public BoundBlock BindBlock(BlockSyntax syntax)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>(syntax.Statements.Count);
        foreach (var statementSyntax in syntax.Statements)
        {
            var statement = BindStatement(statementSyntax);
            statements.Add(statement);
        }

        return new BoundBlock(syntax, statements.MoveToImmutable());
    }

    public BoundStatement BindStatement(StatementSyntax syntax)
    {
        return syntax switch
        {
            BlockSyntax s => BindBlock(s),
            ReturnStatementSyntax s => BindReturnStatement(s),
            ExpressionStatementSyntax s => BindExpressionStatement(s),
            _ => throw new UnreachableException()
        };
    }

    public BoundExpression BindExpression(ExpressionSyntax syntax)
    {
        return syntax switch
        {
            LiteralExpressionSyntax s => BindLiteralExpression(s),
            _ => throw new UnreachableException()
        };
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

    private BoundReturnStatement BindReturnStatement(ReturnStatementSyntax syntax)
    {
        var expression = syntax.Expression == null ? null : BindExpression(syntax.Expression);
        return new BoundReturnStatement(syntax, expression);
    }

    private BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
    {
        var expression = BindExpression(syntax.Expression);
        return new BoundExpressionStatement(syntax, expression);
    }

    private static BoundLiteralExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
    {
        var type = syntax.Literal.Kind switch
        {
            TokenKind.True or TokenKind.False => BuiltInModule.Bool,
            _ => throw new UnreachableException()
        };

        object value = syntax.Literal.Kind switch
        {
            TokenKind.True => true,
            TokenKind.False => false,
            _ => throw new UnreachableException()
        };

        return new BoundLiteralExpression(syntax, type, value);
    }
}
