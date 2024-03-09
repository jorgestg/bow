using System.Collections.Immutable;
using Bow.Compiler;
using Bow.Compiler.CodeGen;
using Bow.Compiler.Syntax;

namespace Bow.Tests;

public sealed class CEmitterTests
{
    [Fact]
    public void Emit_EmitsTaggedUnion()
    {
        SyntaxTree root =
            new(
                new SourceText(
                    "test/test.bow",
                    "mod test\nenum JsonValue { Undefined\n Number(f64) }"
                ),
                f =>
                    f.CompilationUnit(
                        f.ModClause(f.Identifier(0, 3), f.SimpleName(f.Identifier(4, 4))),
                        f.SyntaxList<UseClauseSyntax>(),
                        f.SyntaxList<ItemSyntax>(
                            f.EnumDefinition(
                                null,
                                f.Token(TokenKind.Enum, 9, 4),
                                f.Identifier(14, 9),
                                f.Token(TokenKind.LeftBrace, 24, 1),
                                f.SyntaxList(
                                    f.EnumCaseDeclaration(f.Identifier(26, 9), null),
                                    f.EnumCaseDeclaration(
                                        f.Identifier(37, 6),
                                        f.EnumCaseArgument(
                                            f.Token(TokenKind.LeftParenthesis, 43, 1),
                                            f.KeywordTypeReference(f.Token(TokenKind.F64, 44, 3)),
                                            f.Token(TokenKind.RightParenthesis, 47, 1)
                                        )
                                    )
                                ),
                                f.SyntaxList<FunctionDefinitionSyntax>(),
                                f.Token(TokenKind.RightBrace, 49, 1)
                            )
                        )
                    )
            );

        Compilation compilation = new([root]);

        ImmutableArray<OutputText> actual = CEmitter.Emit(compilation);

        Assert.Empty(compilation.Diagnostics);

        Assert.Equal(2, actual.Length);
        Assert.Equal("test.h", actual[0].FileName);
        Assert.Equal(
            """
            #pragma once
            #include "bow.h"

            """,
            actual[0].Text
        );

        Assert.Equal("test.c", actual[1].FileName);
        Assert.Equal(
            """
            #include "test.h"
            struct JsonValue {
                enum {
                    JsonValue_Undefined,
                    JsonValue_Number
                } tag;
                union {
                    f64 Number;
                } value;
            };

            """,
            actual[1].Text
        );
    }

    [Fact]
    public void Emit_EmitsSimpleEnumDefinition()
    {
        SyntaxTree root =
            new(
                new SourceText("test/test.bow", "mod test\nenum Color { Red\n Green\n Blue }"),
                f =>
                    f.CompilationUnit(
                        f.ModClause(f.Identifier(0, 3), f.SimpleName(f.Identifier(4, 4))),
                        f.SyntaxList<UseClauseSyntax>(),
                        f.SyntaxList<ItemSyntax>(
                            f.EnumDefinition(
                                null,
                                f.Token(TokenKind.Enum, 9, 4),
                                f.Identifier(14, 5),
                                f.Token(TokenKind.LeftBrace, 20, 1),
                                f.SyntaxList(
                                    f.EnumCaseDeclaration(f.Identifier(22, 3), null),
                                    f.EnumCaseDeclaration(f.Identifier(27, 5), null),
                                    f.EnumCaseDeclaration(f.Identifier(34, 4), null)
                                ),
                                f.SyntaxList<FunctionDefinitionSyntax>(),
                                f.Token(TokenKind.RightBrace, 39, 1)
                            )
                        )
                    )
            );

        Compilation compilation = new([root]);

        ImmutableArray<OutputText> actual = CEmitter.Emit(compilation);

        Assert.Empty(compilation.Diagnostics);

        Assert.Equal(2, actual.Length);
        Assert.Equal("test.h", actual[0].FileName);
        Assert.Equal(
            """
            #pragma once
            #include "bow.h"

            """,
            actual[0].Text
        );

        Assert.Equal("test.c", actual[1].FileName);
        Assert.Equal(
            """
            #include "test.h"
            enum Color {
                Color_Red,
                Color_Green,
                Color_Blue
            };

            """,
            actual[1].Text
        );
    }

    [Fact]
    public void Emit_EmitsStructDefinition()
    {
        SyntaxTree root =
            new(
                new SourceText("test/test.bow", "mod bow.test\nstruct Point { x s32\n y s32 }"),
                f =>
                    f.CompilationUnit(
                        f.ModClause(
                            f.Identifier(0, 3),
                            f.QualifiedName(f.SyntaxList(f.Identifier(4, 3), f.Identifier(8, 4)))
                        ),
                        f.SyntaxList<UseClauseSyntax>(),
                        f.SyntaxList<ItemSyntax>(
                            [
                                f.StructDefinition(
                                    null,
                                    f.Token(TokenKind.Struct, 10, 6),
                                    f.Identifier(20, 5),
                                    f.Token(TokenKind.LeftBrace, 22, 1),
                                    f.SyntaxList(
                                        f.FieldDeclaration(
                                            null,
                                            null,
                                            f.Identifier(28, 1),
                                            f.KeywordTypeReference(f.Token(TokenKind.S32, 30, 3))
                                        ),
                                        f.FieldDeclaration(
                                            null,
                                            null,
                                            f.Identifier(35, 1),
                                            f.KeywordTypeReference(f.Token(TokenKind.S32, 37, 3))
                                        )
                                    ),
                                    f.SyntaxList<FunctionDefinitionSyntax>(),
                                    f.Token(TokenKind.RightBrace, 41, 1)
                                )
                            ]
                        )
                    )
            );

        Compilation compilation = new([root]);

        ImmutableArray<OutputText> actual = CEmitter.Emit(compilation);

        Assert.Equal(2, actual.Length);
        Assert.Equal("bow_test.h", actual[0].FileName);
        Assert.Equal(
            """
            #pragma once
            #include "bow.h"

            """,
            actual[0].Text
        );

        Assert.Equal("bow_test.c", actual[1].FileName);
        Assert.Equal(
            """
            #include "bow_test.h"
            struct Point {
                s32 x;
                s32 y;
            };

            """,
            actual[1].Text
        );
    }
}
