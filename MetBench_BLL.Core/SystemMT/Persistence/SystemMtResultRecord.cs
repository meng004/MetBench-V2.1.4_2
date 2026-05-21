namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Persisted summary projection of a <see cref="SystemMtResult"/>. Captures the
/// fields needed to review a run later (scenario, MR direction, numeric values,
/// pass/fail, transformation params) without hauling the full stdout/stderr of
/// the underlying CLI runs, which can be megabytes per OpenMOC iteration.
/// </summary>
/// <remarks>
/// The full <c>SystemMtResult</c> is intentionally not persisted. If a future
/// review needs raw stdout/stderr, the working directories that
/// <see cref="CliProgramRunner"/> wrote to are still on disk for the session
/// that produced the record; reconstruct paths from <see cref="SourceCaseName"/>
/// and <see cref="FollowUpCaseName"/> if needed.
/// </remarks>
public sealed class SystemMtResultRecord
{
    /// <summary>
    /// Business identity of this result. LiteDB persists the same value as the
    /// document <c>_id</c>, generating a fresh Guid on Insert when this field
    /// is <see cref="Guid.Empty"/>. Stable Guid format means downstream
    /// consumers (e.g. <c>AnomalyService.RecordAnomalyAsync</c>) can parse it
    /// without bridging through a BSON ObjectId.
    /// </summary>
    public Guid Id { get; set; }

    public string MrName { get; set; } = string.Empty;

    public DateTimeOffset RunAt { get; set; }

    public string AssertionName { get; set; } = string.Empty;

    public string ValueName { get; set; } = string.Empty;

    public double SourceValue { get; set; }

    public double FollowUpValue { get; set; }

    public bool Passed { get; set; }

    public string FailureReason { get; set; } = string.Empty;

    public string SourceCaseName { get; set; } = string.Empty;

    public string FollowUpCaseName { get; set; } = string.Empty;

    public TimeSpan SourceElapsed { get; set; }

    public TimeSpan FollowUpElapsed { get; set; }

    public int SourceExitCode { get; set; }

    public int FollowUpExitCode { get; set; }

    public Dictionary<string, double> SourceMetrics { get; set; } = new();

    public Dictionary<string, double> FollowUpMetrics { get; set; } = new();

    public string? TransformationName { get; set; }

    public Dictionary<string, string>? TransformationParameters { get; set; }

    public bool? InputGenerationSucceeded { get; set; }

    public string? InputGenerationLog { get; set; }

    /// <summary>
    /// Project a runner result into the persistable summary shape. The
    /// repository fills <see cref="Id"/> and <see cref="RunAt"/> on Save.
    /// </summary>
    public static SystemMtResultRecord FromResult(string mrName, SystemMtResult result)
    {
        if (string.IsNullOrWhiteSpace(mrName))
        {
            throw new ArgumentException("MR name is required", nameof(mrName));
        }
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return new SystemMtResultRecord
        {
            MrName = mrName,
            AssertionName = result.Assertion.AssertionName,
            ValueName = result.Assertion.ValueName,
            SourceValue = result.Assertion.SourceValue,
            FollowUpValue = result.Assertion.FollowUpValue,
            Passed = result.Passed,
            FailureReason = result.FailureReason ?? string.Empty,
            SourceCaseName = result.SourceRun.CaseName,
            FollowUpCaseName = result.FollowUpRun.CaseName,
            SourceElapsed = result.SourceRun.Elapsed,
            FollowUpElapsed = result.FollowUpRun.Elapsed,
            SourceExitCode = result.SourceRun.ExitCode,
            FollowUpExitCode = result.FollowUpRun.ExitCode,
            SourceMetrics = new Dictionary<string, double>(result.SourceOutput.Values),
            FollowUpMetrics = new Dictionary<string, double>(result.FollowUpOutput.Values),
            TransformationName = result.InputGeneration?.Transformation.Name,
            TransformationParameters = result.InputGeneration is null
                ? null
                : new Dictionary<string, string>(result.InputGeneration.Transformation.Parameters),
            InputGenerationSucceeded = result.InputGeneration?.Succeeded,
            InputGenerationLog = result.InputGeneration?.Log,
        };
    }
}
