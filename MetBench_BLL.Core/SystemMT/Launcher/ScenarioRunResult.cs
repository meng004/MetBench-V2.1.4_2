namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// What the launcher returns after a scenario run. The <see cref="RecordId"/>
/// is the persisted record's id; callers can fetch the full
/// <see cref="Persistence.SystemMtResultRecord"/> via the repository if they
/// need detail beyond the summary fields here.
/// </summary>
public sealed record ScenarioRunResult(
    string RecordId,
    string ScenarioId,
    bool Passed,
    string FailureReason,
    string ValueName,
    double SourceValue,
    double FollowUpValue,
    TimeSpan SourceElapsed,
    TimeSpan FollowUpElapsed);
