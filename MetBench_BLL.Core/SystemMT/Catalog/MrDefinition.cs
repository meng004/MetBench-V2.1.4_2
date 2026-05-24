namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Stub for MR-level (not binding-level) catalog metadata. Full responsibility table in spec §5.1.
/// Phase A keeps this minimal; Phase B/C will expand with cross-SUT MR identity fields.
/// </summary>
public sealed class MrDefinition
{
    public string MrId { get; set; } = string.Empty;
    public string MrFamily { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
