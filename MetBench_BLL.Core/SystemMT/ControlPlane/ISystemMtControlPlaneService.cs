using MetBench_BLL.SystemMT.Jobs;

namespace MetBench_BLL.SystemMT.ControlPlane;

/// <summary>
/// Business-facing System MT control plane. Protocol adapters (REST/MCP) should
/// depend on this facade instead of runtime, manifest, or filesystem primitives.
/// </summary>
public interface ISystemMtControlPlaneService
{
    Task<SystemMtControlPlaneJobReceipt> SubmitRunAsync(
        SystemMtControlPlaneRunRequest request,
        CancellationToken cancellationToken = default);

    Task<SystemMtControlPlaneJobSnapshot?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<SystemMtControlPlaneRunResult?> GetResultAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<SystemMtControlPlaneEvidenceSnapshot?> GetEvidenceAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public sealed record SystemMtControlPlaneRunRequest(
    string MrId,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);

public sealed record SystemMtControlPlaneJobReceipt(
    Guid JobId,
    DateTime AcceptedAtUtc);

public sealed record SystemMtControlPlaneJobSnapshot(
    Guid JobId,
    string MrId,
    string SutName,
    SystemMtJobState State,
    string CurrentPhase,
    int ProgressPercent,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? FinishedAtUtc,
    string? FailureReason,
    string? FailureKind,
    Guid? ExecutionId);

public sealed record SystemMtControlPlaneRunResult(
    Guid JobId,
    Guid? ExecutionId,
    string MrId,
    bool Passed,
    string FailureReason,
    string ValueName,
    double SourceValue,
    double FollowUpValue,
    TimeSpan SourceElapsed,
    TimeSpan FollowUpElapsed);

public sealed record SystemMtControlPlaneEvidenceSnapshot(
    Guid JobId,
    Guid ExecutionId,
    string RuntimeKind,
    string RuntimeKey,
    bool RuntimePassed,
    string FailureKind,
    string SourceRunId,
    string FollowupRunId);
