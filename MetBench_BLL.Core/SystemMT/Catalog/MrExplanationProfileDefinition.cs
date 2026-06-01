namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Additive, explanatory profile for a manifest MR binding. These fields are
/// documentation/projection data only and are not consumed by launcher kernels,
/// predicates, transformations, sample resolution, or runtime dispatch.
/// </summary>
public sealed class MrExplanationProfileDefinition
{
    public string MetaPatternRationale { get; set; } = string.Empty;
    public string TransformationSemantics { get; set; } = string.Empty;
    public string ObservableSummary { get; set; } = string.Empty;
    public string PredicateSummary { get; set; } = string.Empty;
    public string ToleranceSummary { get; set; } = string.Empty;
    public string Applicability { get; set; } = string.Empty;
    public string FailureMeaning { get; set; } = string.Empty;
}
