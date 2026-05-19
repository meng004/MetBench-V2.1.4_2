namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// What the launcher returns after an MR run. The <see cref="RecordId"/>
/// is the persisted record's id; callers can fetch the full persisted
/// <see cref="Persistence.SystemMtResultRecord"/> via the repository if they
/// need detail beyond the summary fields here.
/// </summary>
public sealed record MrRunResult(
    string RecordId,
    string MrId,
    bool Passed,
    string FailureReason,
    string ValueName,
    double SourceValue,
    double FollowUpValue,
    TimeSpan SourceElapsed,
    TimeSpan FollowUpElapsed);
