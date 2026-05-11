namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Progress event emitted twice per scenario inside a batch run: once with
/// <see cref="LastResult"/> = <c>null</c> just before the scenario starts, and
/// once with the actual result after it completes.
/// </summary>
/// <param name="Completed">
/// Number of scenarios finished (0..<see cref="Total"/>). When emitting the
/// "starting" event for scenario <c>i</c>, <see cref="Completed"/> = <c>i</c>;
/// when emitting the "finished" event, <see cref="Completed"/> = <c>i + 1</c>.
/// </param>
/// <param name="Total">Total scenarios in the batch.</param>
/// <param name="CurrentScenarioId">The scenario id this event refers to.</param>
/// <param name="LastResult">
/// <c>null</c> on the "starting" event; the just-finished result on the
/// "finished" event.
/// </param>
public sealed record BatchProgress(
    int Completed,
    int Total,
    string CurrentScenarioId,
    ScenarioRunResult? LastResult);
