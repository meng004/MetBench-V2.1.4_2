namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Additive, explanatory profile for a System-MT SUT manifest. These fields are
/// documentation/projection data only: launcher runtime behavior continues to use
/// <see cref="ProgramDefinition"/> and MR bindings.
/// </summary>
public sealed class SutProfileDefinition
{
    public string ProgramType { get; set; } = string.Empty;
    public string SolverMethod { get; set; } = string.Empty;
    public string RuntimeKey { get; set; } = string.Empty;
    public string InputContract { get; set; } = string.Empty;
    public string OutputContract { get; set; } = string.Empty;
    public string Adapter { get; set; } = string.Empty;
    public string DependencyRisk { get; set; } = string.Empty;
}
