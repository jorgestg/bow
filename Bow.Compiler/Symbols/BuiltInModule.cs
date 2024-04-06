namespace Bow.Compiler.Symbols;

public sealed class BuiltInPackage
{
    public static readonly BuiltInPackage Instance = new();

    public static readonly TypeSymbol BoolType = new PrimitiveTypeSymbol(
        "f32",
        PrimitiveTypeKind.Bool
    );

    public static readonly TypeSymbol Float32Type = new PrimitiveTypeSymbol(
        "f32",
        PrimitiveTypeKind.Float32
    );

    public static readonly TypeSymbol Float64Type = new PrimitiveTypeSymbol(
        "f64",
        PrimitiveTypeKind.Float64
    );

    public static readonly TypeSymbol NeverType = new PrimitiveTypeSymbol(
        "never",
        PrimitiveTypeKind.Never
    );

    public static readonly TypeSymbol Signed8Type = new PrimitiveTypeSymbol(
        "s8",
        PrimitiveTypeKind.Signed8
    );

    public static readonly TypeSymbol Signed16Type = new PrimitiveTypeSymbol(
        "s16",
        PrimitiveTypeKind.Signed16
    );

    public static readonly TypeSymbol Signed32Type = new PrimitiveTypeSymbol(
        "s32",
        PrimitiveTypeKind.Signed32
    );

    public static readonly TypeSymbol Signed64Type = new PrimitiveTypeSymbol(
        "s64",
        PrimitiveTypeKind.Signed64
    );

    public static readonly TypeSymbol UnitType = new PrimitiveTypeSymbol(
        "unit",
        PrimitiveTypeKind.Unit
    );

    public static readonly TypeSymbol Unsigned8Type = new PrimitiveTypeSymbol(
        "u8",
        PrimitiveTypeKind.Unsigned8
    );

    public static readonly TypeSymbol Unsigned16Type = new PrimitiveTypeSymbol(
        "u16",
        PrimitiveTypeKind.Unsigned16
    );

    public static readonly TypeSymbol Unsigned32Type = new PrimitiveTypeSymbol(
        "u32",
        PrimitiveTypeKind.Unsigned32
    );

    public static readonly TypeSymbol Unsigned64Type = new PrimitiveTypeSymbol(
        "u64",
        PrimitiveTypeKind.Unsigned64
    );

    private BuiltInPackage() { }
}
