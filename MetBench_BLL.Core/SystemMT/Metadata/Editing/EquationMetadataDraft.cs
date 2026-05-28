namespace MetBench_BLL.SystemMT.Metadata.Editing;

/// <summary>
/// Editable view of an <see cref="EquationMetadata"/> row. Mutable so XAML
/// two-way binding works directly; the editor copies into a fresh
/// <see cref="EquationMetadata"/> on save.
/// </summary>
public sealed record EquationMetadataDraft
{
    public string EquationKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CanonicalForm { get; set; } = string.Empty;
    public string SymbolSystem { get; set; } = string.Empty;

    public static EquationMetadataDraft FromMetadata(EquationMetadata md) => new()
    {
        EquationKey = md.EquationKey,
        Name = md.Name,
        CanonicalForm = md.CanonicalForm,
        SymbolSystem = md.SymbolSystem,
    };

    public EquationMetadata ToMetadata() => new()
    {
        EquationKey = EquationKey,
        Name = Name,
        CanonicalForm = CanonicalForm,
        SymbolSystem = SymbolSystem,
    };
}
