# Cloud Agent Prompt — System MT 异步执行抽象层（契约层）

> **新会话使用方式**：只需发指令 `读取 docs/superpowers/specs/2026-06-03-async-execution-cloud-prompt.md，执行任务`。
> 本文件即完整执行指令：依次完成下面「前置条件检查 → 核心步骤 → 验收标准 → 结论要求」四段，无需额外说明。
>
> 角色：Cloud 轨（Linux / CI 门禁）。冷启动可直接执行。
> 运行环境：`MetBench_BLL.Core` / `MetBench_DAL` / `MetBench_SystemMT.Tests`（全 `net8.0`，Linux 可编译，CI 可门禁）。

## 任务

**读取 `docs/superpowers/plans/2026-06-03-systemmt-async-execution-cloud-plan.md`，按其 Task 1→Task 10 逐任务执行。**

执行方式：用 `superpowers:executing-plans`（批量 + checkpoint）或 `superpowers:subagent-driven-development`（每 Task 一个独立 subagent + 复核）。计划里每个 Task 都是 bite-sized TDD（写失败测试 → 跑红 → 最小实现 → 跑绿 → commit），照做即可。

## 前置条件检查（开工前必须逐条核验，任一不满足即停并报告）

1. `git fetch origin main` 后，确认本地 base 含 `d8b0710`（或更新的 main 头）。命令：`git merge-base --is-ancestor d8b0710 origin/main && echo OK`。
2. 设计 spec 存在：`docs/superpowers/specs/2026-06-03-systemmt-async-execution-polling-design.md`（契约边界来源）。
3. 计划文件存在且可读：`docs/superpowers/plans/2026-06-03-systemmt-async-execution-cloud-plan.md`。
4. 基线测试可跑且现状已知：`dotnet test MetBench_SystemMT.Tests` 当前**全绿**——先跑一次记录基线 pass/skip/fail 计数，作为「兼容零回归」(AC-1) 的对照锚点。**禁止**在未知基线下开工。
5. 当前不在 `main` 上：从 main 切新分支 `claude/async-execution-cloud`（接 CLAUDE.md：默认分支先开分支）。
6. 读 `MetBench_BLL.Core/SystemMT/Launcher/MrRunResult.cs` 真实字段——计划 Task 2/4/7/8 多处依赖它的「通过位 / SUT 名 / 失败原因 / 工厂方法」真实 API，**占位调用名必须替换为真实签名**，不得照抄计划里的 `MrRunResult.Passed/Failed` 占位。

## 核心步骤（计划已给完整代码，此处只列骨架，细节以计划为准）

1. **Task 1-3**：状态机枚举 + 提交/句柄/快照 DTO；durable 记录 + `IJobStore` + `InMemoryJobStore`；`IJobQueue` + `ChannelJobQueue`。
2. **Task 4-5**：`ISystemMtAsyncPipeline` + backend seam + `FakeAsyncPipeline`；`SystemMtJobWorker` 推进状态机（success / timeout / artifact-missing / cancel 四条终止路径）。
3. **Task 6-7**：`ISystemMtJobService` + `SystemMtJobService`（Submit 即返回 + polling 只读 store）；`SystemMtAsyncPipeline` 委托既有 `ISystemMtLauncher.RunAsync`（v1 兼容路径）。
4. **Task 8**：DAL 侧 `LiteDbJobStore`（独立 BsonMapper + 独立库文件 `SystemMtJobs.Litedb`，物理隔离）。
5. **Task 9**：`JobFacadeTypeLeakageTests` 反射守护（§6 不泄漏引擎内部类型）。
6. **Task 10**：全量回归 + 逐条核验验收 + 执行后回写（计划状态、活跃计划索引、AGENTS.md Stage 记录）。

> 边界硬约束（CLAUDE.md §9 + spec §12）：
> - **不动任何 `MetBench_Client/*.xaml*`**（WPF 是 VM 轨）。
> - **不接** `ISutExecutionBackend` 的 Docker/remote/HPC 实现（v1 只定义 seam，标待清理项）。
> - **不改** typed catalog predicate / OpenMC 科学模型 / MR 定义 / 既有同步 launcher 行为。
> - spec §9 的 manifest `execution` 扩展 v1 **不实现**——若发现需要，记为待清理项显式声明，不静默引入。

## 验收标准（必须逐条机械核验，命令输出贴进 PR body「Tests」节）

以计划 §2 的 AC-1…AC-9 为准，关键 9 条：

- **AC-1 兼容零回归**：`dotnet test MetBench_SystemMT.Tests` 全绿；既有 `Launcher/` 端到端测试计数与断言不变（与前置条件 4 记录的基线对照）。
- **AC-2 提交不阻塞**：fake 用 `TaskCompletionSource` 卡住 pipeline 时，`SubmitAsync` 仍立即返回非空 `JobId`，此刻 `GetStatusAsync` 读到 `Queued/Preparing`。
- **AC-3 polling 只读 store**：给会抛异常的 backend，`GetStatusAsync` 仍返回 store last-known state，用 spy 断言 backend 调用次数=0。
- **AC-4 四条终止路径**：worker 测试覆盖 Succeeded（含 `GetResultAsync` 非空）/ TimedOut（reason 非空）/ ArtifactMissing（非 Succeeded、无 result）/ Cancelled。
- **AC-5 durable round-trip**：`SystemMtJobRecord` 全字段写 LiteDB 读回逐字段相等；库文件与 `SystemMT.Litedb`/`MR.Litedb` 物理隔离。
- **AC-6 fail-closed**：未知 backend key / 提交即失败 → `Failed` 且 `FailureReason` 指名原因。
- **AC-7 不泄漏**：`JobFacadeTypeLeakageTests` 绿——facade 签名只落白名单 DTO。
- **AC-8 无 OpenMC 依赖**：上述测试在未装 OpenMC/OpenMOC 的 runner 全绿；CI `dotnet-test.yml` 的 `test` job 通过。
- **AC-9 治理门禁**：PR body 填满 `pr-gate-checklist.md` 7 节；job DTO 单投影，「Tests」节显式声明「single-projection, §12.4 R1 N/A」。

## 结论要求（实事求是，真实数据支撑）

提交结论时**必须**：

1. 给出**真实命令输出**：`dotnet test MetBench_SystemMT.Tests` 的 pass/skip/fail 计数 + wall time（前置基线 vs 完工后两组数字对照），不得写「测试通过」之类无数字断言。
2. 逐条 AC 标 **pass / fail / skip**，每条附触发命令与关键输出行；skip 必须说明原因（接 CLAUDE.md §6 显式报错）。
3. 任何未实现 / 简化 / 待清理项（如 manifest `execution`、durable 队列重启恢复、`CancelAsync` 跨进程语义）必须**显式列出**，不得用「已完成」掩盖。
4. 占位替换情况：明确说明 `MrRunResult` 真实字段是什么、Task 7 的 infra-success 映射最终采用哪条分支（计划给了两条候选）。
5. 若任一 AC 未达成，结论写「未完成 + 阻塞原因」，**禁止**声称完成。
