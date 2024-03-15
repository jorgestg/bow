using System.CodeDom.Compiler;
using Bow.Compiler.Symbols;

namespace Bow.Compiler.CodeGen;

internal record struct OutputText(string FileName, string Text);

internal sealed class CEmitter(Compilation compilation)
{
    private readonly Compilation _compilation = compilation;

    private readonly ImmutableArray<OutputText>.Builder _output =
        ImmutableArray.CreateBuilder<OutputText>(compilation.Modules.Length);

    private readonly IndentedTextWriter _header = new(new StringWriter());
    private readonly IndentedTextWriter _impl = new(new StringWriter());

    public static ImmutableArray<OutputText> Emit(Compilation compilation)
    {
        CEmitter emitter = new(compilation);
        emitter.EmitCompilation();
        return emitter._output.MoveToImmutable();
    }

    private static void WriteTypeName(TextWriter writer, TypeSymbol type)
    {
        if (type is StructSymbol)
            writer.Write("struct ");
        else if (type is EnumSymbol)
            writer.Write("enum ");

        writer.Write(type.Name);
    }

    private static void WriteSignature(TextWriter writer, FunctionItemSymbol symbol)
    {
        WriteTypeName(writer, symbol.ReturnType);
        writer.Write(' ');
        writer.Write(symbol.Name);
        writer.Write('(');
        var first = true;
        foreach (var parameter in symbol.Parameters)
        {
            if (!first)
            {
                writer.Write(", ");
            }

            writer.Write(parameter.Type.Name);
            writer.Write(' ');
            writer.Write(parameter.Name);
            first = false;
        }

        writer.Write(')');
    }

    private static void EmitEnumCase(IndentedTextWriter writer, EnumCaseSymbol enumCase)
    {
        writer.Write(enumCase.Enum.Name);
        writer.Write('_');
        writer.Write(enumCase.Name);
    }

    private void EmitCompilation()
    {
        foreach (var module in _compilation.Modules)
        {
            ((StringWriter)_header.InnerWriter).GetStringBuilder().Clear();
            ((StringWriter)_impl.InnerWriter).GetStringBuilder().Clear();

            EmitModule(module);
        }
    }

    private void EmitModule(ModuleSymbol module)
    {
        _header.WriteLine("#pragma once");
        _header.WriteLine("#include \"bow.h\"");

        var fileName = module.Syntax.ModClause?.Name.ToString().Replace('.', '_') ?? module.Name;
        _impl.Write("#include ");
        _impl.Write('"');
        _impl.Write(fileName);
        _impl.Write(".h");
        _impl.WriteLine('"');

        foreach (var type in module.Types)
        {
            EmitType(type);
        }

        foreach (var function in module.Functions)
        {
            EmitFunctionItemForwardDeclaration(function);
        }

        foreach (var function in module.Functions)
        {
            EmitFunctionItemDefinition(function);
        }

        _output.Add(new OutputText(fileName + ".h", _header.InnerWriter.ToString()!));
        _output.Add(new OutputText(fileName + ".c", _impl.InnerWriter.ToString()!));
    }

    private void EmitType(TypeSymbol symbol)
    {
        switch (symbol)
        {
            case EnumSymbol enumSymbol:
                if (IsTaggedUnion(enumSymbol))
                {
                    EmitTaggedUnion(enumSymbol);
                    break;
                }

                EmitEnum(enumSymbol);
                break;
            case StructSymbol structSymbol:
                EmitStruct(structSymbol);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static bool IsTaggedUnion(EnumSymbol symbol)
    {
        foreach (var enumCase in symbol.Cases)
        {
            if (enumCase.ArgumentType != null)
            {
                return true;
            }
        }

        return false;
    }

    private void EmitEnum(EnumSymbol symbol)
    {
        var writer = symbol.Accessibility == SymbolAccessibility.Public ? _header : _impl;
        writer.Write("enum ");
        writer.Write(symbol.Name);
        writer.Write(' ');
        writer.WriteLine('{');
        writer.Indent++;
        var first = true;
        foreach (var enumCase in symbol.Cases)
        {
            if (!first)
            {
                writer.WriteLine(',');
            }

            EmitEnumCase(writer, enumCase);
            first = false;
        }

        writer.WriteLine();
        writer.Indent--;
        writer.Write('}');
        writer.WriteLine(';');
    }

    /* // Bow
    enum IntOrFloat {
        Int(i32),
        Float(f32)
    }

    // C
    struct IntOrFloat {
        enum {
            IntOrFloat_Int,
            IntOrFloat_Float,
        } tag;
        union {
            i32 Int,
            f32 Float,
        } value;
    } */
    private void EmitTaggedUnion(EnumSymbol symbol)
    {
        var writer = symbol.Accessibility == SymbolAccessibility.Public ? _header : _impl;
        writer.Write("struct ");
        writer.Write(symbol.Name);
        writer.Write(' ');
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("enum {");
        writer.Indent++;

        var first = true;
        foreach (var enumCase in symbol.Cases)
        {
            if (!first)
            {
                writer.WriteLine(',');
            }

            EmitEnumCase(writer, enumCase);
            first = false;
        }

        writer.WriteLine();
        writer.Indent--;
        writer.WriteLine("} tag;");
        writer.WriteLine("union {");
        writer.Indent++;
        foreach (var enumCase in symbol.Cases)
        {
            if (enumCase.ArgumentType == null)
            {
                continue;
            }

            WriteTypeName(writer, enumCase.ArgumentType);
            writer.Write(' ');
            writer.Write(enumCase.Name);
            writer.WriteLine(';');
        }

        writer.Indent--;
        writer.WriteLine("} value;");
        writer.Indent--;
        writer.Write('}');
        writer.WriteLine(';');
    }

    private void EmitStruct(StructSymbol symbol)
    {
        var writer = symbol.Accessibility == SymbolAccessibility.Public ? _header : _impl;
        writer.Write("struct ");
        writer.Write(symbol.Name);
        writer.Write(' ');
        writer.WriteLine('{');
        writer.Indent++;
        foreach (var field in symbol.Fields)
        {
            EmitField(writer, field);
        }

        writer.Indent--;
        writer.Write('}');
        writer.WriteLine(';');
    }

    private static void EmitField(IndentedTextWriter writer, FieldSymbol field)
    {
        WriteTypeName(writer, field.Type);
        writer.Write(' ');
        writer.Write(field.Name);
        writer.WriteLine(';');
    }

    private void EmitFunctionItemForwardDeclaration(FunctionItemSymbol symbol)
    {
        var writer = symbol.Accessibility == SymbolAccessibility.Public ? _header : _impl;
        WriteSignature(writer, symbol);
        writer.WriteLine(';');
    }

    private void EmitFunctionItemDefinition(FunctionItemSymbol symbol)
    {
        WriteSignature(_impl, symbol);
        _impl.Write(' ');
        _impl.WriteLine('{');
        _impl.WriteLine('}');
    }
}
