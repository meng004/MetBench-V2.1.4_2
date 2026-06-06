namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// Unified async operation submission model. Roots are explicit because import/export jobs
/// must not infer filesystem effects from validation-only input.
/// </summary>
public sealed record SystemMtOperationJobRequest(
    SystemMtJobKind Kind,
    string? MrId = null,
    IReadOnlyList<string>? MrIds = null,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null,
    string? PackageRoot = null,
    string? StagingRoot = null,
    string? ExportRoot = null,
    Guid? ExecutionId = null,
    IReadOnlyList<Guid>? ExecutionIds = null);
