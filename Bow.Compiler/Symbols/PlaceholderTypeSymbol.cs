using Bow.Compiler.Syntax;

namespace Bow.Compiler.Symbols;

/// <summary>
/// Compiler-only types for entities that have no type.
/// </summary>
public sealed class PlaceholderTypeSymbol : TypeSymbol
{
    /// <summary>
    /// Placeholder type for missing type references or errors in binding.
    /// </summary>
    public static readonly PlaceholderTypeSymbol Instance = new("???");

    /// <summary>
    /// Placeholder for a type that is currently being inferred.
    /// </summary>
    public static readonly PlaceholderTypeSymbol ToBeInferred = new("???");

    /// <summary>
    /// Placeholder type for types.
    /// </summary>
    public static readonly PlaceholderTypeSymbol MetaType = new("type");

    /// <summary>
    /// Placeholder type for packages.
    /// </summary>
    public static readonly PlaceholderTypeSymbol PackageType = new("package");

    /// <summary>
    /// Placeholder type for modules.
    /// </summary>
    public static readonly PlaceholderTypeSymbol ModuleType = new("module");

    private PlaceholderTypeSymbol(string name)
    {
        Name = name;
    }

    public override string Name { get; }
    public override SyntaxNode Syntax => throw new InvalidOperationException();
    public override ModuleSymbol Module => throw new InvalidOperationException();
    public override bool IsPlaceholder => true;
}
