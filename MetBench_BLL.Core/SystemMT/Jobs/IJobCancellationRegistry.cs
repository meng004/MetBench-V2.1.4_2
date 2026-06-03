namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// Bridges <c>ISystemMtJobService.CancelAsync</c> (which marks the durable store Cancelled)
/// to the actually-running worker, so a user cancel can co-operatively interrupt the in-flight
/// SUT execution instead of only flipping the store record (which would leave the SUT running
/// and produce an orphaned result under a Cancelled record).
/// </summary>
public interface IJobCancellationRegistry
{
    /// <summary>Begin tracking a job; returns a token that trips when <see cref="Cancel"/> is called for it.</summary>
    CancellationToken Register(Guid jobId);

    /// <summary>Trip the registered token for <paramref name="jobId"/>; no-op if not registered.</summary>
    void Cancel(Guid jobId);

    /// <summary>Stop tracking a finished job and release its token source.</summary>
    void Release(Guid jobId);
}
