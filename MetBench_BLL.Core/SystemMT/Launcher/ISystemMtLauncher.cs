namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Stage 4 facade: WPF (and other UI / CLI) callers go through this interface
/// to list available system-MT MRs and execute them. The interface owns
/// the responsibility of routing a MR id to the correct SUT runner +
/// transformation + assertion combination, persisting the result, and
/// returning a UI-friendly summary.
/// </summary>
public interface ISystemMtLauncher
{
    /// <summary>
    /// Enumerate the MRs this launcher knows about. Stable across
    /// process restarts; UI may cache.
    /// </summary>
    Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run one MR. <paramref name="parameterOverrides"/> replaces the
    /// MR's default parameters where keys overlap; missing keys fall
    /// back to defaults. The persisted record is keyed by the MR's
    /// <see cref="MrSummary.DisplayName"/> as
    /// <c>SystemMtResultRecord.MrName</c>.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="mrId"/> is unknown or blank.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> trips.</exception>
    Task<MrRunResult> RunAsync(
        string mrId,
        IReadOnlyDictionary<string, string>? parameterOverrides = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run a sequence of MRs. Pre-validates the whole batch (blank or
    /// unknown ids fail fast before any MR starts). MRs run
    /// sequentially in the order given. An infrastructure exception thrown
    /// inside one MR (Python failure, missing SUT file, etc.) is
    /// captured as a failed <see cref="MrRunResult"/> so the remaining
    /// MRs continue; <see cref="OperationCanceledException"/> propagates
    /// immediately. <paramref name="progress"/>, if provided, receives two
    /// events per MR (see <see cref="BatchProgress"/>).
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="requests"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If any request has a blank or unknown MR id.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> trips.</exception>
    Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
        IReadOnlyList<BatchMrRunRequest> requests,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
