using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.ControlPlane;

public sealed class SystemMtControlPlaneService : ISystemMtControlPlaneService
{
    private static readonly HashSet<string> ReservedOverrideKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "argv",
        "command",
        "manifestPath",
        "artifactRoot",
        "packageRoot",
        "stagingRoot",
        "exportRoot",
        "workingDirectory",
        "executable",
        "executablePath",
    };

    private readonly ISystemMtJobService _jobs;
    private readonly IExecutionEvidenceRepository? _evidence;

    public SystemMtControlPlaneService(
        ISystemMtJobService jobs,
        IExecutionEvidenceRepository? evidence = null)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _evidence = evidence;
    }

    public async Task<SystemMtControlPlaneJobReceipt> SubmitRunAsync(
        SystemMtControlPlaneRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mrId = request.MrId?.Trim();
        if (string.IsNullOrWhiteSpace(mrId))
            throw new ArgumentException("MrId must be non-blank.", nameof(request));

        var handle = await _jobs.SubmitAsync(
            new SystemMtJobRequest(mrId, CopyAndValidateOverrides(request.ParameterOverrides)),
            cancellationToken).ConfigureAwait(false);

        return new SystemMtControlPlaneJobReceipt(handle.JobId, handle.AcceptedAtUtc);
    }

    public async Task<SystemMtControlPlaneJobSnapshot?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var status = await _jobs.GetStatusAsync(jobId, cancellationToken).ConfigureAwait(false);
        return status is null ? null : ToJobSnapshot(status);
    }

    public async Task<SystemMtControlPlaneRunResult?> GetResultAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var status = await _jobs.GetStatusAsync(jobId, cancellationToken).ConfigureAwait(false);
        var result = await _jobs.GetResultAsync(jobId, cancellationToken).ConfigureAwait(false);
        return result is null ? null : ToRunResult(jobId, status?.ExecutionId, result);
    }

    public async Task<SystemMtControlPlaneEvidenceSnapshot?> GetEvidenceAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (_evidence is null)
            return null;

        var status = await _jobs.GetStatusAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (status?.ExecutionId is not { } executionId)
            return null;

        var evidence = await _evidence.GetByExecutionAsync(executionId, cancellationToken)
            .ConfigureAwait(false);
        var runtime = evidence?.RuntimeEvidence;
        return runtime is null
            ? null
            : new SystemMtControlPlaneEvidenceSnapshot(
                jobId,
                executionId,
                runtime.RuntimeKind,
                runtime.RuntimeKey,
                runtime.Passed,
                runtime.FailureKind,
                runtime.SourceRunId,
                runtime.FollowupRunId);
    }

    public Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _jobs.CancelAsync(jobId, cancellationToken);

    private static SystemMtControlPlaneJobSnapshot ToJobSnapshot(SystemMtJobStatus status) => new(
        status.JobId,
        status.MrId,
        status.SutName,
        status.State,
        status.CurrentPhase,
        status.ProgressPercent,
        status.CreatedAtUtc,
        status.UpdatedAtUtc,
        status.FinishedAtUtc,
        status.FailureReason,
        status.FailureKind,
        status.ExecutionId);

    private static SystemMtControlPlaneRunResult ToRunResult(
        Guid jobId,
        Guid? executionId,
        MrRunResult result) => new(
            jobId,
            executionId,
            result.MrId,
            result.Passed,
            result.FailureReason,
            result.ValueName,
            result.SourceValue,
            result.FollowUpValue,
            result.SourceElapsed,
            result.FollowUpElapsed);

    private static IReadOnlyDictionary<string, string>? CopyAndValidateOverrides(
        IReadOnlyDictionary<string, string>? overrides)
    {
        if (overrides is null)
            return null;

        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in overrides)
        {
            var normalizedKey = key.Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
                throw new ArgumentException("Parameter override keys must be non-blank.", nameof(overrides));
            if (ReservedOverrideKeys.Contains(normalizedKey))
                throw new ArgumentException(
                    $"Parameter override '{normalizedKey}' is reserved for infrastructure and cannot be submitted through the control plane.",
                    nameof(overrides));
            if (value is null)
                throw new ArgumentException(
                    $"Parameter override '{normalizedKey}' must not be null.",
                    nameof(overrides));

            copy[normalizedKey] = value;
        }

        return copy.Count == 0 ? null : copy;
    }
}
