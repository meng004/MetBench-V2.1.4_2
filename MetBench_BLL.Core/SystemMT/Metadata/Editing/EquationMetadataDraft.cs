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
    public string EquationClass { get; set; } = string.Empty;
    public string EquationFamily { get; set; } = string.Empty;
    public List<string> PrimaryVariables { get; set; } = new();
    public string PhysicalMeaning { get; set; } = string.Empty;
    public string BenchmarkRationale { get; set; } = string.Empty;
    public List<string> ExpectedLaws { get; set; } = new();

    public static EquationMetadataDraft FromMetadata(EquationMetadata md) => new()
    {
        EquationKey = md.EquationKey,
        Name = md.Name,
        CanonicalForm = md.CanonicalForm,
        SymbolSystem = md.SymbolSystem,
        EquationClass = md.EquationClass,
        EquationFamily = md.EquationFamily,
        PrimaryVariables = md.PrimaryVariables?.ToList() ?? new List<string>(),
        PhysicalMeaning = md.PhysicalMeaning,
        BenchmarkRationale = md.BenchmarkRationale,
        ExpectedLaws = md.ExpectedLaws?.ToList() ?? new List<string>(),
    };

    public EquationMetadata ToMetadata() => new()
    {
        EquationKey = EquationKey,
        Name = Name,
        CanonicalForm = CanonicalForm,
        SymbolSystem = SymbolSystem,
        EquationClass = EquationClass,
        EquationFamily = EquationFamily,
        PrimaryVariables = PrimaryVariables?.ToList() ?? new List<string>(),
        PhysicalMeaning = PhysicalMeaning,
        BenchmarkRationale = BenchmarkRationale,
        ExpectedLaws = ExpectedLaws?.ToList() ?? new List<string>(),
    };
}
