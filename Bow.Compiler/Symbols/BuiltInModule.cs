namespace Bow.Compiler.Symbols;

public sealed class BuiltInModule
{
    public static readonly BuiltInModule Instance = new();

    public static readonly TypeSymbol Unit = new PrimitiveTypeSymbol(
        Instance,
        "unit",
        PrimitiveTypeKind.Unit
    );

    public static readonly TypeSymbol Unsigned8 = new PrimitiveTypeSymbol(
        Instance,
        "u8",
        PrimitiveTypeKind.Unsigned8
    );

    public static readonly TypeSymbol Unsigned16 = new PrimitiveTypeSymbol(
        Instance,
        "u16",
        PrimitiveTypeKind.Unsigned16
    );

    public static readonly TypeSymbol Unsigned32 = new PrimitiveTypeSymbol(
        Instance,
        "u32",
        PrimitiveTypeKind.Unsigned32
    );

    public static readonly TypeSymbol Unsigned64 = new PrimitiveTypeSymbol(
        Instance,
        "u64",
        PrimitiveTypeKind.Unsigned64
    );

    public static readonly TypeSymbol Signed8 = new PrimitiveTypeSymbol(
        Instance,
        "s8",
        PrimitiveTypeKind.Signed8
    );

    public static readonly TypeSymbol Signed16 = new PrimitiveTypeSymbol(
        Instance,
        "s16",
        PrimitiveTypeKind.Signed16
    );

    public static readonly TypeSymbol Signed32 = new PrimitiveTypeSymbol(
        Instance,
        "s32",
        PrimitiveTypeKind.Signed32
    );

    public static readonly TypeSymbol Signed64 = new PrimitiveTypeSymbol(
        Instance,
        "s64",
        PrimitiveTypeKind.Signed64
    );

    public static readonly TypeSymbol Float32 = new PrimitiveTypeSymbol(
        Instance,
        "f32",
        PrimitiveTypeKind.Float32
    );

    public static readonly TypeSymbol Float64 = new PrimitiveTypeSymbol(
        Instance,
        "f64",
        PrimitiveTypeKind.Float64
    );

    private BuiltInModule()
    {
        Types =
        [
            Unit,
            Unsigned8,
            Unsigned16,
            Unsigned32,
            Unsigned64,
            Signed8,
            Signed16,
            Signed32,
            Signed64,
            Float32,
            Float64
        ];
    }

    public ImmutableArray<TypeSymbol> Types { get; }
}
