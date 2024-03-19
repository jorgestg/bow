namespace Bow.Compiler.Symbols;

public sealed class BuiltInModule
{
    public static readonly BuiltInModule Instance = new();

    public static readonly TypeSymbol Bool = new PrimitiveTypeSymbol("f32", PrimitiveTypeKind.Bool);

    public static readonly TypeSymbol Float32 = new PrimitiveTypeSymbol(
        "f32",
        PrimitiveTypeKind.Float32
    );

    public static readonly TypeSymbol Float64 = new PrimitiveTypeSymbol(
        "f64",
        PrimitiveTypeKind.Float64
    );

    public static readonly TypeSymbol Never = new PrimitiveTypeSymbol(
        "never",
        PrimitiveTypeKind.Never
    );

    public static readonly TypeSymbol Signed8 = new PrimitiveTypeSymbol(
        "s8",
        PrimitiveTypeKind.Signed8
    );

    public static readonly TypeSymbol Signed16 = new PrimitiveTypeSymbol(
        "s16",
        PrimitiveTypeKind.Signed16
    );

    public static readonly TypeSymbol Signed32 = new PrimitiveTypeSymbol(
        "s32",
        PrimitiveTypeKind.Signed32
    );

    public static readonly TypeSymbol Signed64 = new PrimitiveTypeSymbol(
        "s64",
        PrimitiveTypeKind.Signed64
    );

    public static readonly TypeSymbol Unit = new PrimitiveTypeSymbol(
        "unit",
        PrimitiveTypeKind.Unit
    );

    public static readonly TypeSymbol Unsigned8 = new PrimitiveTypeSymbol(
        "u8",
        PrimitiveTypeKind.Unsigned8
    );

    public static readonly TypeSymbol Unsigned16 = new PrimitiveTypeSymbol(
        "u16",
        PrimitiveTypeKind.Unsigned16
    );

    public static readonly TypeSymbol Unsigned32 = new PrimitiveTypeSymbol(
        "u32",
        PrimitiveTypeKind.Unsigned32
    );

    public static readonly TypeSymbol Unsigned64 = new PrimitiveTypeSymbol(
        "u64",
        PrimitiveTypeKind.Unsigned64
    );

    private BuiltInModule() { }
}
