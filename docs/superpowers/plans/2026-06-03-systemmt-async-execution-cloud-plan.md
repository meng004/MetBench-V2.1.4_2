---
状态: Implemented — Task 1-3 merged via PR #278; Task 4-10 in PR-2 (this chain)
环境: Cloud (Linux / CI-gated)
依赖设计: docs/superpowers/specs/2026-06-03-systemmt-async-execution-polling-design.md
配套计划: docs/superpowers/plans/2026-06-03-systemmt-async-execution-vm-plan.md (VM 消费侧，后行)
---

# System MT 异步执行 + Polling（Cloud 侧契约层）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **实施偏差记录（§12.4 R3 — 实施时对照真实 `MrRunResult.cs` 修正了计划占位）：**
> 1. **无 `MrRunResult.Passed()/Failed()` 工厂** —— `MrRunResult` 是 9 参位置 record（`RecordId/MrId/Passed/FailureReason/ValueName/SourceValue/FollowUpValue/SourceElapsed/FollowUpElapsed`）。Task 2/4/7/8 的测试改用真实构造（见 `JobsTestData.Result`）。
> 2. **`MrRunResult` 无 `SutName` 字段** —— SUT 名改由 `SystemMtAsyncPipeline` 经 `ISystemMtLauncher.ListAvailableAsync()` 按 MrId 查 `MrSummary.SutName` 解析；worker 在 finalize 时回填 `SystemMtJobRecord.SutName`；未知 MR 留空字符串。
> 3. **`MrRunResult.Passed` 是 MR 断言位、非基础设施位** —— `RunAsync` 返回即基础设施成功 → 终止态 `Succeeded`（即便断言 `Passed=false`，按 spec §10 走结果/异常调查路径）。`SystemMtAsyncPipeline` 因此**删去 Failed 分支**；基础设施失败由 `RunAsync` 抛异常表达，在 `SystemMtJobWorker` catch 中归类 Failed。v1 launcher 路径只产生 `Succeeded`（或抛异常→Failed）；`TimedOut`/`ArtifactMissing` 为后续 backend 预留。
> 4. **worker 进度回写并发修正** —— 用内联同步 `IProgress`（回调原地有序执行）替代默认 `Progress<T>`，避免线程池投递导致中间进度写在终止态 finalize **之后**、覆盖终止态。
> 5. **manifest `execution` 扩展（spec §9）v1 不实现** —— 所有 job 走 launcher 默认同步路径；catalog 解析延后到接 Docker/remote 后端时。

**Goal:** 在 `MetBench_BLL.Core` 内新增一个**加性的** System MT job 子系统，让调用方提交一个 MR 运行后立即拿到 `JobId`，由后台 worker 执行、把状态变更落库，调用方仅通过 polling（`GetStatusAsync(jobId)`）读取持久快照；不改 MR 语义 / typed predicate / catalog 含义 / 现有同步 `SystemMtLauncher.RunAsync` 行为。

**Architecture:** Job 层位于现有 launcher/pipeline 之上。`ISystemMtJobService.SubmitAsync` 入队并落库 `Queued` 记录后立即返回 `JobId`；`SystemMtJobWorker` 从 `IJobQueue` 取作业，调用 `ISystemMtAsyncPipeline`（v1 直接委托既有 `ISystemMtLauncher.RunAsync`，复用已验证的同步路径），把状态机推进写入 `IJobStore`；polling 只读 `IJobStore` 的持久快照，**绝不**直接探测进程 / 容器 / 远程后端。所有代码 `net8.0`、Linux 可编译、CI 可门禁、无需安装 OpenMC。

**Tech Stack:** C# `net8.0`、`System.Threading.Channels`（队列）、LiteDB（durable job store，DAL 侧，复用现有 BsonMapper 隔离约定）、xUnit（fake-backend TDD）。

---

## 0. 范围与边界（先读，决定每个 Task 的落点）

按设计 spec §12：

**本计划 in-scope（v1）：**
1. job service 抽象 + DTO（不泄漏引擎内部类型）
2. durable job state（InMemoryJobStore 默认 + LiteDbJobStore 持久）
3. polling 状态 API（只读 store 快照）
4. local backend = 委托既有 `ISystemMtLauncher.RunAsync`（兼容路径）
5. 与现有同步 launcher 完全兼容（既有端到端测试保持绿）

**本计划 out-of-scope（显式不做，接 spec §12 + CLAUDE.md §1.2 最小修改）：**
- WPF UI 改动 → 见配套 VM 计划。
- webhook / hook 回调。
- `ISutExecutionBackend` 的 Docker / remote / HPC 实现。
- 改 typed semantic catalog predicate / OpenMC 科学模型 / MR 定义。

### 架构决策（接 CLAUDE.md §1.5「冲突挑明」）

设计 §5 同时画了两条抽象：(a) job 层（service/worker/store/queue）和 (b) `ISutExecutionBackend` 的 per-SUT 后端粒度（Local/Docker/Remote/Hpc）。**v1 选择路径 (a)，把 (b) 的非 Local 实现标为待清理项。** 理由：`ISystemMtLauncher.RunAsync` 已是验证过的、把 parser→transform→writer→source SUT→follow-up SUT→output parser→assertion 跑完的兼容入口；v1 的 `SystemMtAsyncPipeline` 直接委托它，即等价于设计里的 "LocalProcessBackend：wraps current subprocess execution and proves compatibility"。`ISutExecutionBackend` 接口**作为 seam 定义出来**（供 Docker/remote/HPC 后续接入），但 v1 不接非 Local 实现。被标记的待清理项：`DockerBackend` / `RemoteServerBackend` / `HpcQueueBackend`（spec §8 候选序列 2-4）。

### §6 facade type-leakage 合规

新公开方法签名只允许：primitives / `string` / `Guid` / `Dictionary<string,string>` / 本计划新增的 record DTO（`SystemMtJobHandle` / `SystemMtJobStatus` / `SystemMtJobRequest` / `SystemMtJobRecord` / `SystemMtJobState`）/ 既有 facade DTO `MrRunResult`。**不得**通过 job API 暴露 `SystemMtTask` / `SystemMtRunner` / `PipelineContext` / `IMrAssertion` / `SystemMtResult` 等引擎内部类型。

---

## 1. File Structure（落点先锁死）

新增命名空间 `MetBench_BLL.SystemMT.Jobs`，目录 `MetBench_BLL.Core/SystemMT/Jobs/`：

| 文件 | 职责 |
|---|---|
| `Jobs/SystemMtJobState.cs` | 状态机枚举（11 态，spec §6）。 |
| `Jobs/SystemMtJobRequest.cs` | 提交请求 DTO：`MrId` + 可选 `ParameterOverrides`。 |
| `Jobs/SystemMtJobHandle.cs` | `SubmitAsync` 返回值：`JobId` + 受理时间。 |
| `Jobs/SystemMtJobRecord.cs` | durable 记录（spec §5 字段）。 |
| `Jobs/SystemMtJobStatus.cs` | polling 返回的只读快照（spec §7 字段）。 |
| `Jobs/IJobStore.cs` | 状态持久契约（Create/UpdateStatus/Get/SaveResult/GetResult）。 |
| `Jobs/InMemoryJobStore.cs` | 默认 + 测试用线程安全内存实现。 |
| `Jobs/IJobQueue.cs` | 入队 / 取队契约。 |
| `Jobs/ChannelJobQueue.cs` | `System.Threading.Channels` 无界队列实现。 |
| `Jobs/ISystemMtAsyncPipeline.cs` | `ExecuteJobAsync(jobId, request, progress, ct)` 抽象（worker 调它）。 |
| `Jobs/SystemMtAsyncPipeline.cs` | v1 实现：委托既有 `ISystemMtLauncher.RunAsync`，把 phase 进度映射到状态机。 |
| `Jobs/ISystemMtJobService.cs` | 对外 facade：Submit/GetStatus/GetResult/Cancel。 |
| `Jobs/SystemMtJobService.cs` | 默认实现（store + queue）。 |
| `Jobs/SystemMtJobWorker.cs` | 后台 worker：取队 → 执行 async pipeline → 写状态机。 |
| `Jobs/ISutExecutionBackend.cs` | 后端 seam 接口 + `SutExecutionRequest`/`SutRunHandle`/`SutRunStatus`/`SutRunArtifacts`（仅定义，v1 不接非 Local 实现）。 |

DAL 侧持久实现：

| 文件 | 职责 |
|---|---|
| `MetBench_DAL/LiteDbJobStore.cs` | LiteDB 持久 `IJobStore`，隔离 `BsonMapper`，独立库文件 `SystemMtJobs.Litedb`。 |

测试（`MetBench_SystemMT.Tests/SystemMT/Jobs/`）：

| 文件 | 职责 |
|---|---|
| `Jobs/FakeAsyncPipeline.cs` | 确定性 fake：可配置走 Succeeded / TimedOut / ArtifactMissing / Failed。 |
| `Jobs/InMemoryJobStoreTests.cs` | store round-trip + 状态更新 + 结果保存。 |
| `Jobs/SystemMtJobServiceTests.cs` | Submit 立即返回 + Queued 落库；polling 只读 store 快照。 |
| `Jobs/SystemMtJobWorkerTests.cs` | worker 推进状态机：success / timeout / artifact-missing / cancel。 |
| `Jobs/SystemMtAsyncPipelineCompatTests.cs` | async pipeline 委托既有 launcher，结果与同步 `RunAsync` 等价。 |
| `Jobs/LiteDbJobStoreTests.cs` | LiteDB durable round-trip（临时库文件）。 |

---

## 2. 验收标准（Acceptance Criteria — 你点名要的）

整体计划在以下全部满足时才算 **Done**（每条都可在 Cloud / CI 上机械核验）：

- **AC-1 兼容性零回归**：`dotnet test MetBench_SystemMT.Tests` 全绿，且**既有** `SystemMtLauncher` 端到端测试（`Launcher/` 下）数量与断言不变（async 层是加性的）。命令：`dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Launcher"`。
- **AC-2 异步提交不阻塞**：`SystemMtJobServiceTests.SubmitAsync_returns_handle_before_pipeline_completes` 通过——`SubmitAsync` 在 pipeline 仍在跑（fake 用 `TaskCompletionSource` 卡住）时即返回非空 `JobId`，且此刻 `GetStatusAsync` 读到 `Queued` 或 `Preparing`。
- **AC-3 polling 只读 store**：`SystemMtJobServiceTests.GetStatusAsync_reads_store_only` 通过——给一个会抛异常的 fake backend，但 store 里有持久快照，`GetStatusAsync` 仍返回 store 的 last-known state，**不触发** backend 调用（用 spy 断言 backend 调用次数为 0）。
- **AC-4 状态机四条终止路径**：`SystemMtJobWorkerTests` 覆盖并通过：(a) 正常 `Queued→…→Succeeded` 且 `GetResultAsync` 返回非空 `MrRunResult`；(b) 超时 → `TimedOut`，`FailureReason` 非空；(c) backend 报完成但产物缺失 → `ArtifactMissing`（不是 `Succeeded`）；(d) 取消 → `Cancelled`。
- **AC-5 durable round-trip**：`LiteDbJobStoreTests.Create_then_Get_roundtrips_all_fields` 通过——`SystemMtJobRecord` 全字段写入 LiteDB 后读回逐字段相等；且该库文件与 `SystemMT.Litedb` / `MR.Litedb` 物理隔离（独立连接串 + 独立 BsonMapper）。
- **AC-6 fail-closed**：未知 backend key / 提交即失败时，记录落 `Failed` 且 `FailureReason` 指名原因（`SystemMtJobWorkerTests.Unknown_backend_fails_closed_before_queue`）。
- **AC-7 §6 不泄漏**：新增一个守护测试 `JobFacadeTypeLeakageTests`，用反射断言 `ISystemMtJobService` 所有 public 方法的参数与返回类型只落在白名单集合内（见 §1 §6 合规清单）；泄漏引擎内部类型即红。
- **AC-8 无 OpenMC 依赖**：上述测试在未安装 OpenMC/OpenMOC 的 CI runner 上全绿（fake backend，环境无关）。CI workflow `dotnet-test.yml` 的 `test` job 通过。
- **AC-9 治理门禁**：PR body 填满 `pr-gate-checklist.md` 7 节；若新增 record 有多投影路径，按 §12.4 R1 加 `*ParityTests.cs`（本计划 job DTO 目前单投影，无需 parity，但需在 PR body「Tests」节显式声明「single-projection, R1 N/A」）。

---

## 3. Tasks

### Task 1: 状态机枚举 + 提交 / 句柄 / 快照 DTO

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobState.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRequest.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobHandle.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobStateTests.cs`

- [ ] **Step 1: Write the failing test**（锁死 11 个状态名 + 终止态判定）

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobStateTests.cs
using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtJobStateTests
{
    [Theory]
    [InlineData(SystemMtJobState.Queued, false)]
    [InlineData(SystemMtJobState.Preparing, false)]
    [InlineData(SystemMtJobState.RunningSource, false)]
    [InlineData(SystemMtJobState.RunningFollowup, false)]
    [InlineData(SystemMtJobState.ParsingOutputs, false)]
    [InlineData(SystemMtJobState.Asserting, false)]
    [InlineData(SystemMtJobState.Succeeded, true)]
    [InlineData(SystemMtJobState.Failed, true)]
    [InlineData(SystemMtJobState.TimedOut, true)]
    [InlineData(SystemMtJobState.Cancelled, true)]
    [InlineData(SystemMtJobState.ArtifactMissing, true)]
    public void IsTerminal_matches_state_model(SystemMtJobState state, bool terminal)
        => Assert.Equal(terminal, state.IsTerminal());
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtJobStateTests"`
Expected: FAIL（编译错误：`SystemMtJobState` / `IsTerminal` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobState.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>System MT 异步作业状态机（spec §6）。非终止态可转入任一终止态。</summary>
public enum SystemMtJobState
{
    Queued,
    Preparing,
    RunningSource,
    RunningFollowup,
    ParsingOutputs,
    Asserting,
    Succeeded,
    Failed,
    TimedOut,
    Cancelled,
    ArtifactMissing,
}

public static class SystemMtJobStateExtensions
{
    /// <summary>终止态：Succeeded / Failed / TimedOut / Cancelled / ArtifactMissing。</summary>
    public static bool IsTerminal(this SystemMtJobState state) => state switch
    {
        SystemMtJobState.Succeeded => true,
        SystemMtJobState.Failed => true,
        SystemMtJobState.TimedOut => true,
        SystemMtJobState.Cancelled => true,
        SystemMtJobState.ArtifactMissing => true,
        _ => false,
    };
}
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRequest.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>异步提交一个 MR 运行的请求。语义与 ISystemMtLauncher.RunAsync 的 (mrId, overrides) 对齐。</summary>
public sealed record SystemMtJobRequest(
    string MrId,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobHandle.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>SubmitAsync 的返回值：受理凭据，后续 polling 用 JobId。</summary>
public sealed record SystemMtJobHandle(Guid JobId, DateTime AcceptedAtUtc);
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>polling 返回的只读快照（spec §7）。来源仅为 IJobStore，不反映 live backend。</summary>
public sealed record SystemMtJobStatus(
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
    string? BackendKind = null,
    string? BackendExternalId = null,
    DateTime? LastPolledAtUtc = null);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtJobStateTests"`
Expected: PASS（11 个 InlineData 全过）。

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobState.cs \
        MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRequest.cs \
        MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobHandle.cs \
        MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobStateTests.cs
git commit -m "feat(systemmt): add async job state machine + submit/handle/status DTOs"
```

---

### Task 2: durable 记录 + IJobStore + InMemoryJobStore

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/IJobStore.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/InMemoryJobStore.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/InMemoryJobStoreTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/InMemoryJobStoreTests.cs
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class InMemoryJobStoreTests
{
    private static SystemMtJobRecord NewRecord(Guid id) => new()
    {
        JobId = id,
        MrId = "openmc-fission-q-value-invariance",
        SutName = "openmc",
        State = SystemMtJobState.Queued,
        ProgressPercent = 0,
        CurrentPhase = "queued",
        CreatedAtUtc = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAtUtc = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public async Task Create_then_Get_returns_same_record()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(NewRecord(id), default);

        var got = await store.GetAsync(id, default);

        Assert.NotNull(got);
        Assert.Equal("openmc", got!.SutName);
        Assert.Equal(SystemMtJobState.Queued, got.State);
    }

    [Fact]
    public async Task UpdateStatus_then_Get_reflects_new_state()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(NewRecord(id), default);

        var updated = NewRecord(id) with
        {
            State = SystemMtJobState.RunningSource,
            ProgressPercent = 40,
            CurrentPhase = "running-source",
            UpdatedAtUtc = new DateTime(2026, 6, 3, 0, 1, 0, DateTimeKind.Utc),
        };
        await store.UpdateStatusAsync(updated, default);

        var got = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.RunningSource, got!.State);
        Assert.Equal(40, got.ProgressPercent);
    }

    [Fact]
    public async Task SaveResult_then_GetResult_roundtrips()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(NewRecord(id), default);
        var result = MrRunResult.Passed("openmc-fission-q-value-invariance", "openmc", "k_eff invariant");

        await store.SaveResultAsync(id, result, default);

        var got = await store.GetResultAsync(id, default);
        Assert.NotNull(got);
        Assert.True(got!.Passed);
    }

    [Fact]
    public async Task Get_unknown_id_returns_null()
        => Assert.Null(await new InMemoryJobStore().GetAsync(Guid.NewGuid(), default));
}
```

> 注意：`MrRunResult.Passed(...)` 是占位调用名；实现 Step 3 前先 `Read MetBench_BLL.Core/SystemMT/Launcher/MrRunResult.cs` 确认真实工厂 / 构造签名，并把测试里的构造改成真实 API（若无 `Passed` 工厂则直接 `new MrRunResult(...)` 按其真实字段）。

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~InMemoryJobStoreTests"`
Expected: FAIL（`SystemMtJobRecord` / `IJobStore` / `InMemoryJobStore` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>durable job 记录（spec §5）。job store 落这一行；polling 从它投影 SystemMtJobStatus。</summary>
public sealed record SystemMtJobRecord
{
    public Guid JobId { get; init; }
    public string MrId { get; init; } = string.Empty;
    public string SutName { get; init; } = string.Empty;
    public SystemMtJobState State { get; init; } = SystemMtJobState.Queued;
    public int ProgressPercent { get; init; }
    public string CurrentPhase { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public string? BackendKind { get; init; }
    public string? BackendExternalId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public DateTime? LastPolledAtUtc { get; init; }

    /// <summary>投影成 polling 快照。</summary>
    public SystemMtJobStatus ToStatus() => new(
        JobId, MrId, SutName, State, CurrentPhase, ProgressPercent,
        CreatedAtUtc, UpdatedAtUtc, FinishedAtUtc, FailureReason,
        BackendKind, BackendExternalId, LastPolledAtUtc);
}
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/IJobStore.cs
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>job 持久契约。polling 只读这里；后台 worker 只写这里。</summary>
public interface IJobStore
{
    Task CreateAsync(SystemMtJobRecord record, CancellationToken cancellationToken);
    Task UpdateStatusAsync(SystemMtJobRecord record, CancellationToken cancellationToken);
    Task<SystemMtJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken);
    Task SaveResultAsync(Guid jobId, MrRunResult result, CancellationToken cancellationToken);
    Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken);
}
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/InMemoryJobStore.cs
using System.Collections.Concurrent;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>线程安全内存 job store：默认实现 + 测试 double。durable 持久用 LiteDbJobStore。</summary>
public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, SystemMtJobRecord> _records = new();
    private readonly ConcurrentDictionary<Guid, MrRunResult> _results = new();

    public Task CreateAsync(SystemMtJobRecord record, CancellationToken cancellationToken)
    {
        if (!_records.TryAdd(record.JobId, record))
            throw new InvalidOperationException($"Job {record.JobId} already exists.");
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(SystemMtJobRecord record, CancellationToken cancellationToken)
    {
        _records[record.JobId] = record;
        return Task.CompletedTask;
    }

    public Task<SystemMtJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken)
        => Task.FromResult(_records.TryGetValue(jobId, out var r) ? r : null);

    public Task SaveResultAsync(Guid jobId, MrRunResult result, CancellationToken cancellationToken)
    {
        _results[jobId] = result;
        return Task.CompletedTask;
    }

    public Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
        => Task.FromResult(_results.TryGetValue(jobId, out var r) ? r : null);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~InMemoryJobStoreTests"`
Expected: PASS（4 测试全过）。

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs \
        MetBench_BLL.Core/SystemMT/Jobs/IJobStore.cs \
        MetBench_BLL.Core/SystemMT/Jobs/InMemoryJobStore.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/InMemoryJobStoreTests.cs
git commit -m "feat(systemmt): add durable job record + IJobStore + in-memory store"
```

---

### Task 3: IJobQueue + ChannelJobQueue

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/IJobQueue.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/ChannelJobQueue.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/ChannelJobQueueTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/ChannelJobQueueTests.cs
using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class ChannelJobQueueTests
{
    [Fact]
    public async Task Enqueued_id_is_dequeued_in_fifo_order()
    {
        var queue = new ChannelJobQueue();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await queue.EnqueueAsync(a, default);
        await queue.EnqueueAsync(b, default);

        Assert.Equal(a, await queue.DequeueAsync(default));
        Assert.Equal(b, await queue.DequeueAsync(default));
    }

    [Fact]
    public async Task DequeueAsync_honors_cancellation_when_empty()
    {
        var queue = new ChannelJobQueue();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await queue.DequeueAsync(cts.Token));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ChannelJobQueueTests"`
Expected: FAIL（`IJobQueue` / `ChannelJobQueue` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/IJobQueue.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>job 入队 / 取队。worker 在 DequeueAsync 上阻塞等下一个 job。</summary>
public interface IJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/ChannelJobQueue.cs
using System.Threading.Channels;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>无界 FIFO 队列。单进程内 worker 消费；多进程部署时换 LiteDb/外部队列实现。</summary>
public sealed class ChannelJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(jobId, cancellationToken);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ChannelJobQueueTests"`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Jobs/IJobQueue.cs \
        MetBench_BLL.Core/SystemMT/Jobs/ChannelJobQueue.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/ChannelJobQueueTests.cs
git commit -m "feat(systemmt): add channel-backed job queue"
```

---

### Task 4: ISystemMtAsyncPipeline + backend seam + FakeAsyncPipeline

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/ISystemMtAsyncPipeline.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/ISutExecutionBackend.cs`（seam 定义；v1 无非 Local 实现）
- Create: `MetBench_SystemMT.Tests/SystemMT/Jobs/FakeAsyncPipeline.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/FakeAsyncPipelineTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/FakeAsyncPipelineTests.cs
using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class FakeAsyncPipelineTests
{
    [Fact]
    public async Task Fake_emits_phase_progress_then_returns_outcome()
    {
        var fake = FakeAsyncPipeline.Succeeds("openmc-q-value", "openmc");
        var phases = new List<SystemMtJobProgress>();
        var progress = new Progress<SystemMtJobProgress>(p => phases.Add(p));

        var outcome = await fake.ExecuteJobAsync(
            Guid.NewGuid(),
            new SystemMtJobRequest("openmc-q-value"),
            progress,
            default);

        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.NotNull(outcome.Result);
        // progress collection is observed by the worker, not asserted for exact phases here.
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~FakeAsyncPipelineTests"`
Expected: FAIL（`ISystemMtAsyncPipeline` / `SystemMtJobProgress` / `JobExecutionOutcome` / `FakeAsyncPipeline` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/ISystemMtAsyncPipeline.cs
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>worker 调用它执行一个 job。v1 实现委托既有 ISystemMtLauncher.RunAsync。</summary>
public interface ISystemMtAsyncPipeline
{
    Task<JobExecutionOutcome> ExecuteJobAsync(
        Guid jobId,
        SystemMtJobRequest request,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>worker 据此把状态机写入 store 的进度事件。</summary>
public sealed record SystemMtJobProgress(SystemMtJobState State, string Phase, int ProgressPercent);

/// <summary>async pipeline 的最终产物。FinalState 必属终止态；Succeeded 时 Result 非空。</summary>
public sealed record JobExecutionOutcome(
    SystemMtJobState FinalState,
    string SutName,
    MrRunResult? Result,
    string? FailureReason);
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/ISutExecutionBackend.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 执行后端 seam（spec §8）。v1 只定义接口，不接 Docker/remote/HPC 实现 —— 见本计划 §0 架构决策待清理项。
/// v1 的 SystemMtAsyncPipeline 直接委托 ISystemMtLauncher，不经过本接口。
/// </summary>
public interface ISutExecutionBackend
{
    Task<SutRunHandle> SubmitAsync(SutExecutionRequest request, CancellationToken cancellationToken);
    Task<SutRunStatus> GetStatusAsync(SutRunHandle handle, CancellationToken cancellationToken);
    Task<SutRunArtifacts> FetchArtifactsAsync(SutRunHandle handle, CancellationToken cancellationToken);
    Task CancelAsync(SutRunHandle handle, CancellationToken cancellationToken);
}

public sealed record SutExecutionRequest(string SutName, string WorkingDirectory, int TimeoutSeconds);
public sealed record SutRunHandle(string BackendKind, string ExternalId);
public sealed record SutRunStatus(bool Completed, bool Faulted, string? Diagnostic);
public sealed record SutRunArtifacts(bool AllPresent, IReadOnlyList<string> MissingPaths);
```

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/FakeAsyncPipeline.cs
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>确定性 fake：按预设走 Succeeded / TimedOut / ArtifactMissing / Failed，可选 gate 卡住执行。</summary>
public sealed class FakeAsyncPipeline : ISystemMtAsyncPipeline
{
    private readonly SystemMtJobState _final;
    private readonly string _sut;
    private readonly MrRunResult? _result;
    private readonly string? _failure;
    public TaskCompletionSource? Gate { get; }
    public int Invocations { get; private set; }

    private FakeAsyncPipeline(SystemMtJobState final, string sut, MrRunResult? result, string? failure, bool gated)
    {
        _final = final; _sut = sut; _result = result; _failure = failure;
        Gate = gated ? new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) : null;
    }

    public static FakeAsyncPipeline Succeeds(string mrId, string sut, bool gated = false)
        => new(SystemMtJobState.Succeeded, sut,
               MrRunResult.Passed(mrId, sut, "fake pass"), null, gated);

    public static FakeAsyncPipeline TimesOut(string sut)
        => new(SystemMtJobState.TimedOut, sut, null, "fake timeout", false);

    public static FakeAsyncPipeline ArtifactMissing(string sut)
        => new(SystemMtJobState.ArtifactMissing, sut, null, "fake missing artifact", false);

    public static FakeAsyncPipeline Fails(string sut, string reason)
        => new(SystemMtJobState.Failed, sut, null, reason, false);

    public async Task<JobExecutionOutcome> ExecuteJobAsync(
        Guid jobId, SystemMtJobRequest request,
        IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
    {
        Invocations++;
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "preparing", 10));
        if (Gate is not null) await Gate.Task.WaitAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.RunningSource, "running-source", 50));
        return new JobExecutionOutcome(_final, _sut, _result, _failure);
    }
}
```

> 同 Task 2 注意：`MrRunResult.Passed(...)` 为占位；实现前 `Read MrRunResult.cs` 改成真实构造 API。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~FakeAsyncPipelineTests"`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Jobs/ISystemMtAsyncPipeline.cs \
        MetBench_BLL.Core/SystemMT/Jobs/ISutExecutionBackend.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/FakeAsyncPipeline.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/FakeAsyncPipelineTests.cs
git commit -m "feat(systemmt): add async pipeline + backend seam contracts + fake pipeline"
```

---

### Task 5: SystemMtJobWorker（状态机推进 — 含 timeout / artifact-missing / cancel）

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobWorkerTests.cs`

- [ ] **Step 1: Write the failing test**（AC-4 四条终止路径）

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobWorkerTests.cs
using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtJobWorkerTests
{
    private static (InMemoryJobStore store, Guid id) Seed(string mrId = "mr", string sut = "openmc")
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        store.CreateAsync(new SystemMtJobRecord
        {
            JobId = id, MrId = mrId, SutName = sut,
            State = SystemMtJobState.Queued, CurrentPhase = "queued",
            CreatedAtUtc = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
        }, default).GetAwaiter().GetResult();
        return (store, id);
    }

    [Fact]
    public async Task Success_path_reaches_Succeeded_and_saves_result()
    {
        var (store, id) = Seed("mr-ok");
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.Succeeds("mr-ok", "openmc"));

        await worker.RunJobAsync(id, default);

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Succeeded, rec!.State);
        Assert.NotNull(rec.FinishedAtUtc);
        Assert.NotNull(await store.GetResultAsync(id, default));
    }

    [Fact]
    public async Task Timeout_path_reaches_TimedOut_with_reason()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.TimesOut("openmc"));
        await worker.RunJobAsync(id, default);
        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.TimedOut, rec!.State);
        Assert.False(string.IsNullOrWhiteSpace(rec.FailureReason));
    }

    [Fact]
    public async Task ArtifactMissing_path_does_not_report_Succeeded()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.ArtifactMissing("openmc"));
        await worker.RunJobAsync(id, default);
        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.ArtifactMissing, rec!.State);
        Assert.Null(await store.GetResultAsync(id, default));
    }

    [Fact]
    public async Task Cancellation_reaches_Cancelled()
    {
        var (store, id) = Seed();
        var gated = FakeAsyncPipeline.Succeeds("mr", "openmc", gated: true);
        var worker = new SystemMtJobWorker(store, gated);
        using var cts = new CancellationTokenSource();

        var run = worker.RunJobAsync(id, cts.Token);
        cts.Cancel();               // 在 gate 卡住期间取消
        gated.Gate!.TrySetResult();
        await run;

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Cancelled, rec!.State);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtJobWorkerTests"`
Expected: FAIL（`SystemMtJobWorker` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs
namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 后台 worker：取一个 jobId，调 async pipeline，把进度 + 终止态写 store。
/// 状态机推进是确定性代码（接 CLAUDE.md §1.3）：终止态由 pipeline outcome / 取消 / 异常决定，不交给模型判断。
/// </summary>
public sealed class SystemMtJobWorker
{
    private readonly IJobStore _store;
    private readonly ISystemMtAsyncPipeline _pipeline;
    private readonly Func<DateTime> _utcNow;

    public SystemMtJobWorker(IJobStore store, ISystemMtAsyncPipeline pipeline, Func<DateTime>? utcNow = null)
    {
        _store = store;
        _pipeline = pipeline;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <summary>执行单个 job 全生命周期。异常不外抛 —— 转成 Failed 记录（fail closed，spec §10）。</summary>
    public async Task RunJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var record = await _store.GetAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {jobId} not found in store.");

        var progress = new Progress<SystemMtJobProgress>(p =>
            _ = _store.UpdateStatusAsync(record with
            {
                State = p.State, CurrentPhase = p.Phase,
                ProgressPercent = p.ProgressPercent, UpdatedAtUtc = _utcNow(),
            }, CancellationToken.None));

        try
        {
            var outcome = await _pipeline.ExecuteJobAsync(jobId, ToRequest(record), progress, cancellationToken);

            if (outcome.FinalState == SystemMtJobState.Succeeded && outcome.Result is not null)
                await _store.SaveResultAsync(jobId, outcome.Result, CancellationToken.None);

            await Finalize(record, outcome.FinalState, outcome.FailureReason);
        }
        catch (OperationCanceledException)
        {
            await Finalize(record, SystemMtJobState.Cancelled, "cancellation requested");
        }
        catch (Exception ex)
        {
            await Finalize(record, SystemMtJobState.Failed, ex.Message);
        }
    }

    private static SystemMtJobRequest ToRequest(SystemMtJobRecord r) => new(r.MrId);

    private Task Finalize(SystemMtJobRecord record, SystemMtJobState state, string? reason)
        => _store.UpdateStatusAsync(record with
        {
            State = state,
            FailureReason = state == SystemMtJobState.Succeeded ? null : reason,
            ProgressPercent = state == SystemMtJobState.Succeeded ? 100 : record.ProgressPercent,
            CurrentPhase = state.ToString().ToLowerInvariant(),
            UpdatedAtUtc = _utcNow(),
            FinishedAtUtc = _utcNow(),
        }, CancellationToken.None);
}
```

> `_utcNow` 注入是为可测性（测试可传固定时钟）；prod 默认 `DateTime.UtcNow`。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtJobWorkerTests"`
Expected: PASS（4 条终止路径全过）。

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobWorkerTests.cs
git commit -m "feat(systemmt): add job worker driving terminal state machine"
```

---

### Task 6: ISystemMtJobService + SystemMtJobService（Submit/GetStatus/GetResult/Cancel）

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/ISystemMtJobService.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobService.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobServiceTests.cs`

- [ ] **Step 1: Write the failing test**（AC-2 提交不阻塞 + AC-3 polling 只读 store）

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobServiceTests.cs
using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtJobServiceTests
{
    private static SystemMtJobService Build(IJobStore store, IJobQueue queue)
        => new(store, queue);

    [Fact]
    public async Task SubmitAsync_persists_Queued_and_enqueues_before_worker_runs()
    {
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var svc = Build(store, queue);

        var handle = await svc.SubmitAsync(new SystemMtJobRequest("openmc-q-value"), default);

        Assert.NotEqual(Guid.Empty, handle.JobId);
        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Queued, status!.State);
        // 队列里确有该 id（worker 尚未起）
        Assert.Equal(handle.JobId, await queue.DequeueAsync(default));
    }

    [Fact]
    public async Task GetStatusAsync_reads_store_snapshot_only()
    {
        var store = new InMemoryJobStore();
        var svc = Build(store, new ChannelJobQueue());
        var handle = await svc.SubmitAsync(new SystemMtJobRequest("mr"), default);

        // 直接改 store 模拟 worker 推进；service 读到的就是该快照
        var rec = await store.GetAsync(handle.JobId, default);
        await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.RunningSource }, default);

        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.RunningSource, status!.State);
    }

    [Fact]
    public async Task GetStatusAsync_unknown_job_returns_null()
        => Assert.Null(await Build(new InMemoryJobStore(), new ChannelJobQueue())
            .GetStatusAsync(Guid.NewGuid(), default));

    [Fact]
    public async Task SubmitAsync_blank_mrId_throws()
        => await Assert.ThrowsAsync<ArgumentException>(
            () => Build(new InMemoryJobStore(), new ChannelJobQueue())
                .SubmitAsync(new SystemMtJobRequest("  "), default));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtJobServiceTests"`
Expected: FAIL（`ISystemMtJobService` / `SystemMtJobService` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/ISystemMtJobService.cs
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 异步 System MT 执行 facade（spec §5）。SubmitAsync 立即返回 JobId；状态只能通过
/// GetStatusAsync polling（读 store 快照，spec §4 §7）。§6 type-leakage：签名只含
/// primitives / Guid / 本命名空间 DTO / 既有 MrRunResult。
/// </summary>
public interface ISystemMtJobService
{
    Task<SystemMtJobHandle> SubmitAsync(SystemMtJobRequest request, CancellationToken cancellationToken = default);
    Task<SystemMtJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}
```

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobService.cs
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 默认 job service。Submit 落 Queued + 入队即返回；polling 只读 store。
/// Cancel 在 v1 仅标记意图（co-op 取消由 worker 的 CancellationToken 处理）；
/// CancellationRegistry 注入留作后续，v1 用 store 标记 + worker token 协作。
/// </summary>
public sealed class SystemMtJobService : ISystemMtJobService
{
    private readonly IJobStore _store;
    private readonly IJobQueue _queue;
    private readonly Func<DateTime> _utcNow;

    public SystemMtJobService(IJobStore store, IJobQueue queue, Func<DateTime>? utcNow = null)
    {
        _store = store;
        _queue = queue;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public async Task<SystemMtJobHandle> SubmitAsync(SystemMtJobRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.MrId))
            throw new ArgumentException("MrId must be non-blank.", nameof(request));

        var now = _utcNow();
        var id = Guid.NewGuid();
        await _store.CreateAsync(new SystemMtJobRecord
        {
            JobId = id,
            MrId = request.MrId,
            SutName = string.Empty,            // worker 解析 MR → SUT 后回填
            State = SystemMtJobState.Queued,
            CurrentPhase = "queued",
            ProgressPercent = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        }, cancellationToken);

        await _queue.EnqueueAsync(id, cancellationToken);
        return new SystemMtJobHandle(id, now);
    }

    public async Task<SystemMtJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
        => (await _store.GetAsync(jobId, cancellationToken))?.ToStatus();

    public Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _store.GetResultAsync(jobId, cancellationToken);

    public async Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var rec = await _store.GetAsync(jobId, cancellationToken);
        if (rec is null || rec.State.IsTerminal()) return;
        await _store.UpdateStatusAsync(rec with
        {
            State = SystemMtJobState.Cancelled,
            FailureReason = "cancellation requested",
            CurrentPhase = "cancelled",
            UpdatedAtUtc = _utcNow(),
            FinishedAtUtc = _utcNow(),
        }, cancellationToken);
    }
}
```

> 决策（接 §1.5）：v1 `CancelAsync` 只在 store 标记 `Cancelled`，实际中断正在跑的 worker 由共享 `CancellationToken` 协作完成。「跨进程 / 队列删除已入队但未起的 job」标为待清理项（依赖 durable 队列实现，见 Task 8 末注）。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtJobServiceTests"`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Jobs/ISystemMtJobService.cs \
        MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobService.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobServiceTests.cs
git commit -m "feat(systemmt): add async job service facade (submit/poll/result/cancel)"
```

---

### Task 7: SystemMtAsyncPipeline（v1 委托既有 ISystemMtLauncher，兼容性等价）

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtAsyncPipeline.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtAsyncPipelineCompatTests.cs`

- [ ] **Step 1: Write the failing test**（AC-1 兼容：async pipeline 产出与同步 RunAsync 等价）

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtAsyncPipelineCompatTests.cs
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtAsyncPipelineCompatTests
{
    // 极简 fake launcher：记录被调用的 mrId，返回固定 MrRunResult
    private sealed class StubLauncher : ISystemMtLauncher
    {
        public string? LastMrId;
        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrSummary>>(Array.Empty<MrSummary>());
        public Task<MrRunResult> RunAsync(string mrId, IReadOnlyDictionary<string, string>? ov = null, CancellationToken ct = default)
        {
            LastMrId = mrId;
            return Task.FromResult(MrRunResult.Passed(mrId, "openmc", "ok"));
        }
        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(IReadOnlyList<BatchMrRunRequest> r, IProgress<BatchProgress>? p = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrRunResult>>(Array.Empty<MrRunResult>());
    }

    [Fact]
    public async Task ExecuteJobAsync_delegates_to_launcher_and_maps_success()
    {
        var launcher = new StubLauncher();
        var pipeline = new SystemMtAsyncPipeline(launcher);

        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(), new SystemMtJobRequest("openmc-q-value"), null, default);

        Assert.Equal("openmc-q-value", launcher.LastMrId);
        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.NotNull(outcome.Result);
    }

    [Fact]
    public async Task ExecuteJobAsync_maps_launcher_failure_result_to_Failed()
    {
        // 让 stub 返回 failed result（按 MrRunResult 真实工厂改造）
        var pipeline = new SystemMtAsyncPipeline(new FailingLauncher());
        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(), new SystemMtJobRequest("mr"), null, default);
        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
    }

    private sealed class FailingLauncher : ISystemMtLauncher
    {
        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrSummary>>(Array.Empty<MrSummary>());
        public Task<MrRunResult> RunAsync(string mrId, IReadOnlyDictionary<string, string>? ov = null, CancellationToken ct = default)
            => Task.FromResult(MrRunResult.Failed(mrId, "openmc", "boom"));
        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(IReadOnlyList<BatchMrRunRequest> r, IProgress<BatchProgress>? p = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrRunResult>>(Array.Empty<MrRunResult>());
    }
}
```

> 实现前必读 `MrRunResult.cs`：确认 `Passed` / `Failed` 工厂是否存在，以及哪个字段表达「通过 / 失败」「SUT 名」「失败原因」。把 stub 与 mapping 改成真实 API（这是本 Task 唯一的 codebase-specific 风险点）。

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtAsyncPipelineCompatTests"`
Expected: FAIL（`SystemMtAsyncPipeline` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_BLL.Core/SystemMT/Jobs/SystemMtAsyncPipeline.cs
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// v1 async pipeline：委托既有 ISystemMtLauncher.RunAsync（复用验证过的同步路径，spec §3 原则 1 / §12 兼容）。
/// 把 launcher 返回的 MrRunResult 映射成 JobExecutionOutcome 的终止态。
/// MR 断言失败仍是 MR 结果路径（Succeeded job + failed assertion 记录），不是基础设施 Failed（spec §10）。
/// </summary>
public sealed class SystemMtAsyncPipeline : ISystemMtAsyncPipeline
{
    private readonly ISystemMtLauncher _launcher;

    public SystemMtAsyncPipeline(ISystemMtLauncher launcher) => _launcher = launcher;

    public async Task<JobExecutionOutcome> ExecuteJobAsync(
        Guid jobId, SystemMtJobRequest request,
        IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
    {
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "preparing", 10));
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.RunningSource, "running-source", 40));

        MrRunResult result = await _launcher.RunAsync(request.MrId, request.ParameterOverrides, cancellationToken);

        progress?.Report(new SystemMtJobProgress(SystemMtJobState.Asserting, "asserting", 90));

        // RunAsync 不抛 → 基础设施成功；job 终止态 Succeeded，MR 断言通过/失败由 MrRunResult 自身承载。
        // 若 RunAsync 内部把 SUT 失败表达为 failed-result（非异常），按真实 MrRunResult 字段判定映射到 Failed。
        bool infraOk = ResultIndicatesInfraSuccess(result);
        return infraOk
            ? new JobExecutionOutcome(SystemMtJobState.Succeeded, ResolveSut(result), result, null)
            : new JobExecutionOutcome(SystemMtJobState.Failed, ResolveSut(result), null, ResolveFailure(result));
    }

    // 下面三个 helper 在 Step 3 落地时按 MrRunResult 真实字段实现（先读 MrRunResult.cs）：
    private static bool ResultIndicatesInfraSuccess(MrRunResult r) => /* r.Status != infra-error */ true;
    private static string ResolveSut(MrRunResult r) => /* r.SutName ?? */ string.Empty;
    private static string? ResolveFailure(MrRunResult r) => /* r.ErrorMessage */ null;
}
```

> 三个 helper 含内联占位注释，**必须**在落地时用 `MrRunResult` 真实字段替换（这是允许的「实现时按真实 API 完成」，不是计划占位——签名与契约已确定，仅字段名待对照源文件）。若 `MrRunResult` 没有「基础设施失败」概念（即 launcher 总是抛异常表达基础设施失败、result 只表达 MR pass/fail），则 `ResultIndicatesInfraSuccess` 恒为 true，`Failed` 分支由 worker 的 catch 兜底，删去本类的 Failed 分支并在 PR body 说明该简化。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtAsyncPipelineCompatTests"`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Jobs/SystemMtAsyncPipeline.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtAsyncPipelineCompatTests.cs
git commit -m "feat(systemmt): async pipeline delegates to launcher for v1 compatibility"
```

---

### Task 8: LiteDbJobStore（durable 持久，DAL 侧，物理隔离库文件）

**Files:**
- Create: `MetBench_DAL/LiteDbJobStore.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/LiteDbJobStoreTests.cs`

> 实现前先 `Read MetBench_DAL/LiteDbSystemMtResultRepository.cs`，照搬其隔离 `BsonMapper` + 连接串模式（CLAUDE.md §6：System-MT LiteDB 与 legacy DB 用独立 BsonMapper 互不干扰）。

- [ ] **Step 1: Write the failing test**（AC-5 durable round-trip）

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/LiteDbJobStoreTests.cs
using MetBench_BLL.SystemMT.Jobs;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class LiteDbJobStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"jobstore-{Guid.NewGuid():N}.litedb");

    [Fact]
    public async Task Create_then_Get_roundtrips_all_fields()
    {
        var store = new LiteDbJobStore($"Filename={_dbPath}");
        var id = Guid.NewGuid();
        var record = new SystemMtJobRecord
        {
            JobId = id, MrId = "openmc-q-value", SutName = "openmc",
            State = SystemMtJobState.RunningFollowup, ProgressPercent = 70,
            CurrentPhase = "running-followup", FailureReason = null,
            BackendKind = "local", BackendExternalId = "pid-1234",
            CreatedAtUtc = new DateTime(2026, 6, 3, 1, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 6, 3, 1, 2, 0, DateTimeKind.Utc),
        };
        await store.CreateAsync(record, default);

        var got = await store.GetAsync(id, default);
        Assert.NotNull(got);
        Assert.Equal(record.MrId, got!.MrId);
        Assert.Equal(record.SutName, got.SutName);
        Assert.Equal(record.State, got.State);
        Assert.Equal(record.ProgressPercent, got.ProgressPercent);
        Assert.Equal(record.BackendExternalId, got.BackendExternalId);
    }

    [Fact]
    public async Task UpdateStatus_persists_across_new_handle()
    {
        var conn = $"Filename={_dbPath}";
        var id = Guid.NewGuid();
        var seed = new SystemMtJobRecord { JobId = id, MrId = "mr", SutName = "openmc",
            State = SystemMtJobState.Queued, CurrentPhase = "queued",
            CreatedAtUtc = DateTime.UnixEpoch, UpdatedAtUtc = DateTime.UnixEpoch };
        await new LiteDbJobStore(conn).CreateAsync(seed, default);
        await new LiteDbJobStore(conn).UpdateStatusAsync(seed with { State = SystemMtJobState.Succeeded }, default);

        var got = await new LiteDbJobStore(conn).GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Succeeded, got!.State);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LiteDbJobStoreTests"`
Expected: FAIL（`LiteDbJobStore` 未定义）。

- [ ] **Step 3: Write minimal implementation**

```csharp
// MetBench_DAL/LiteDbJobStore.cs
using LiteDB;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_DAL;

/// <summary>
/// LiteDB 持久 job store。独立 BsonMapper + 独立库文件（默认 SystemMtJobs.Litedb），
/// 与 SystemMT.Litedb / MR.Litedb 物理隔离（CLAUDE.md §6）。结果序列化为 BSON doc 与
/// 记录分集合存放，避免 MrRunResult schema 漂移污染记录集合。
/// </summary>
public sealed class LiteDbJobStore : IJobStore
{
    private readonly string _connectionString;
    private readonly BsonMapper _mapper;

    private const string Records = "JobRecords";
    private const string Results = "JobResults";

    public LiteDbJobStore(string connectionString)
    {
        _connectionString = connectionString;
        _mapper = new BsonMapper();
        _mapper.Entity<SystemMtJobRecord>().Id(r => r.JobId);
    }

    private LiteDatabase Open() => new(_connectionString, _mapper);

    public Task CreateAsync(SystemMtJobRecord record, CancellationToken ct)
    {
        using var db = Open();
        db.GetCollection<SystemMtJobRecord>(Records).Insert(record);
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(SystemMtJobRecord record, CancellationToken ct)
    {
        using var db = Open();
        db.GetCollection<SystemMtJobRecord>(Records).Upsert(record);
        return Task.CompletedTask;
    }

    public Task<SystemMtJobRecord?> GetAsync(Guid jobId, CancellationToken ct)
    {
        using var db = Open();
        return Task.FromResult<SystemMtJobRecord?>(
            db.GetCollection<SystemMtJobRecord>(Records).FindById(jobId));
    }

    public Task SaveResultAsync(Guid jobId, MrRunResult result, CancellationToken ct)
    {
        using var db = Open();
        var col = db.GetCollection(Results);
        var doc = _mapper.ToDocument(result);
        doc["_id"] = jobId;
        col.Upsert(doc);
        return Task.CompletedTask;
    }

    public Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken ct)
    {
        using var db = Open();
        var doc = db.GetCollection(Results).FindById(jobId);
        return Task.FromResult(doc is null ? null : _mapper.ToObject<MrRunResult>(doc));
    }
}
```

> 若 `MrRunResult` 是带必填位置参数的 record，`ToObject` 反序列化可能需要 `_mapper.Entity<MrRunResult>()` 配置或 `[BsonCtor]`；落地时按 LiteDbSystemMtResultRepository 既有做法对齐（它已序列化嵌套 result 类型，照搬其 mapper 配置）。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LiteDbJobStoreTests"`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add MetBench_DAL/LiteDbJobStore.cs \
        MetBench_SystemMT.Tests/SystemMT/Jobs/LiteDbJobStoreTests.cs
git commit -m "feat(dal): add LiteDB-backed durable job store (isolated db file)"
```

> 待清理项（接 §0 决策）：durable 队列（重启后恢复未完成 job）依赖把 `Queued` 记录在 worker 启动时 re-enqueue；本 v1 用进程内 `ChannelJobQueue`，重启丢未起 job。标为后续：worker host 启动时扫描 store 中 `Queued/Preparing` 记录回灌队列。

---

### Task 9: §6 type-leakage 守护测试（AC-7）

**Files:**
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/JobFacadeTypeLeakageTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// MetBench_SystemMT.Tests/SystemMT/Jobs/JobFacadeTypeLeakageTests.cs
using System.Reflection;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class JobFacadeTypeLeakageTests
{
    private static readonly HashSet<Type> Allowed = new()
    {
        typeof(void), typeof(string), typeof(Guid), typeof(bool), typeof(int),
        typeof(CancellationToken),
        typeof(SystemMtJobRequest), typeof(SystemMtJobHandle),
        typeof(SystemMtJobStatus), typeof(SystemMtJobState),
        typeof(MrRunResult),
    };

    [Fact]
    public void ISystemMtJobService_does_not_leak_engine_internal_types()
    {
        foreach (var m in typeof(ISystemMtJobService).GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            AssertAllowed(Unwrap(m.ReturnType), m.Name + " return");
            foreach (var p in m.GetParameters())
                AssertAllowed(Unwrap(p.ParameterType), m.Name + " param " + p.Name);
        }
    }

    private static Type Unwrap(Type t)
    {
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(Nullable<>) ||
                def == typeof(IReadOnlyDictionary<,>) || def == typeof(IReadOnlyList<>))
                return Unwrap(t.GetGenericArguments()[^1]);
        }
        return t;
    }

    private static void AssertAllowed(Type t, string where)
        => Assert.True(Allowed.Contains(t),
            $"{where} exposes disallowed type {t.FullName} through job facade (CLAUDE.md §6).");
}
```

- [ ] **Step 2: Run test to verify it fails or passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~JobFacadeTypeLeakageTests"`
Expected: PASS（若设计正确则直接绿；若 GetResultAsync 返回了 `IReadOnlyDictionary` 等需在 `Unwrap` 覆盖）。先跑确认；红则修 `Unwrap` 覆盖面或修接口签名。

- [ ] **Step 3: 若红，修接口而非放宽白名单**

若断言抓到泄漏，**优先收紧接口签名**（把内部类型替换为 DTO），而非往 `Allowed` 加内部类型。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~JobFacadeTypeLeakageTests"`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add MetBench_SystemMT.Tests/SystemMT/Jobs/JobFacadeTypeLeakageTests.cs
git commit -m "test(systemmt): guard async job facade against engine type leakage"
```

---

### Task 10: 全量回归 + 验收核验（AC-1 / AC-8）

**Files:** 无新增（验收 gate）。

- [ ] **Step 1: 全量测试**

Run: `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj && dotnet test MetBench_SystemMT.Tests`
Expected: 全绿，0 fail；既有 Launcher 端到端测试计数不变（async 层加性）。

- [ ] **Step 2: 逐条对照 §2 验收标准**

逐项核验 AC-1…AC-9，把命令输出贴进 PR body「Tests」节。

- [ ] **Step 3: 执行后回写（CLAUDE.md §11.1 闭环第 4 步）**

更新本计划 frontmatter 状态 → `Completed`；在活跃计划索引登记本计划行；若引入新 Stage / 范围，更 `AGENTS.md` 对应 Stage 交付记录（Stage 粒度）。

- [ ] **Step 4: Commit + PR**

```bash
git add docs/superpowers/plans/2026-06-03-systemmt-async-execution-cloud-plan.md \
        docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
git commit -m "docs(systemmt): mark async-execution cloud plan complete + index"
```

PR body 填满 `docs/superpowers/templates/pr-gate-checklist.md` 7 节，Windows Classification 标 **cloud-only / 无 WPF 改动**，并在「Review」节注明：本 chain 若与 VM 计划合计构成 ≥3 PR 链路，需在链尾按 §12.4 R2 跑 chain-end holistic review。

---

## 4. Self-Review（写计划者自检，已执行）

- **Spec 覆盖**：spec §4 job 层(Task 5/6) / §5 类图(Task 1-8) / §6 状态机(Task 1) / §7 polling 只读 store(Task 6 AC-3) / §8 backend seam(Task 4，非 Local 实现明确 deferred) / §10 错误处理 fail-closed(Task 5) / §11 测试策略 fake-backend(Task 4-7) / §12 范围(本计划 §0) 均有任务对应。§9 manifest `execution` 扩展**未实现** —— 标为待清理项：v1 不读 manifest `execution` 块，所有 job 走 launcher 默认同步路径；catalog 解析延后到接 Docker/remote 后端时。已在此显式声明（接 §6 显式报错，不静默漏项）。
- **占位扫描**：Task 7 三个 helper 与 Task 2/4/7 的 `MrRunResult.Passed/Failed` 是「实现时对照真实源文件确定字段名」，签名与契约已定，附明确指引；不属 TBD/「适当处理」类计划占位。
- **类型一致**：`SystemMtJobState` / `SystemMtJobRecord` / `SystemMtJobStatus` / `SystemMtJobRequest` / `JobExecutionOutcome` / `SystemMtJobProgress` 命名在 Task 1-9 间一致；`IJobStore` 五方法签名（Create/UpdateStatus/Get/SaveResult/GetResult）在 Task 2/5/6/8 一致。

## 5. Execution Handoff

见文末交接说明（与 VM 计划共用）。
