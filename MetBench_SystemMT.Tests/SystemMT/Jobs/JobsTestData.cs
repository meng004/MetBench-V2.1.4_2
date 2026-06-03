using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>共享测试构造器：用真实 <see cref="MrRunResult"/> 9 参签名 + 基础 job 记录。</summary>
internal static class JobsTestData
{
    internal static readonly DateTime T0 = new(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);

    internal static MrRunResult Result(string mrId, bool passed, string reason = "") => new(
        RecordId: Guid.NewGuid().ToString(),
        MrId: mrId,
        Passed: passed,
        FailureReason: reason,
        ValueName: "k_eff",
        SourceValue: 1.0,
        FollowUpValue: 1.0,
        SourceElapsed: TimeSpan.FromSeconds(1),
        FollowUpElapsed: TimeSpan.FromSeconds(1));

    internal static SystemMtJobRecord Record(Guid id, string mrId = "mr", string sut = "openmc") => new()
    {
        JobId = id,
        MrId = mrId,
        SutName = sut,
        State = SystemMtJobState.Queued,
        ProgressPercent = 0,
        CurrentPhase = "queued",
        CreatedAtUtc = T0,
        UpdatedAtUtc = T0,
    };
}
