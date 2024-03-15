using System.Collections.Immutable;
using Bow.Compiler;
using Bow.Compiler.CodeGen;
using Bow.Compiler.Syntax;

namespace Bow.Tests.CodeGen;

public sealed class CEmitterTests
{
    [Fact]
    public void Emit_WhenEnumIsTaggedUnion_EmitsCStruct()
    {
        var root = SyntaxTree.Create(
            "test/test.bow",
            """
            mod test

            enum JsonValue {
                Undefined
                Number(f64)
            }
            """
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
    public void Emit_WhenSimpleEnum_EmitsCEnum()
    {
        var root = SyntaxTree.Create(
            "test/test.bow",
            """
            mod test

            enum Color {
                Red
                Green
                Blue
            }
            """
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
    public void Emit_WhenStruct_EmitsCStruct()
    {
        var root = SyntaxTree.Create(
            "test/test.bow",
            """
            mod bow.test

            struct Point { 
                x s32
                y s32
            }
            """
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
