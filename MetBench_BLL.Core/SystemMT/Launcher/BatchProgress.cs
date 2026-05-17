namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Progress event emitted twice per MR inside a batch run: once with
/// <see cref="LastResult"/> = <c>null</c> just before the MR starts, and
/// once with the actual result after it completes.
/// </summary>
/// <param name="Completed">
/// Number of MRs finished (0..<see cref="Total"/>). When emitting the
/// "starting" event for MR <c>i</c>, <see cref="Completed"/> = <c>i</c>;
/// when emitting the "finished" event, <see cref="Completed"/> = <c>i + 1</c>.
/// </param>
/// <param name="Total">Total MRs in the batch.</param>
/// <param name="CurrentMrId">The MR id this event refers to.</param>
/// <param name="LastResult">
/// <c>null</c> on the "starting" event; the just-finished result on the
/// "finished" event.
/// </param>
public sealed record BatchProgress(
    int Completed,
    int Total,
    string CurrentMrId,
    MrRunResult? LastResult);
