# VM Agent Prompt — System MT 异步执行抽象层（WPF 消费侧）

> **新会话使用方式**：只需发指令 `读取 docs/superpowers/specs/2026-06-03-async-execution-vm-prompt.md，执行任务`。
> 本文件即完整执行指令：依次完成下面「前置条件检查 → 核心步骤 → 验收标准 → 结论要求」四段，无需额外说明。
>
> 角色：VM 轨（Windows + VS 2022 + WPF，**不经 CI**）。冷启动可直接执行。
> 运行环境：`MetBench_Client/`（`net8.0-windows7.0`，仅 Windows 可编译/运行/截图）。

## 任务

**读取 `docs/superpowers/plans/2026-06-03-systemmt-async-execution-vm-plan.md`，按其 Task 1→Task 5 逐任务执行。**

执行方式：用 `superpowers:executing-plans` 或手动按 checkbox 逐步做。计划里每个 Task 都给了完整 XAML / C# 代码 + `dotnet build` / `dotnet run` 命令。WPF 不经 CI，每个 Task 的「验证」= VM 上构建通过 + 运行观察。

## 前置条件检查（开工前必须逐条核验，任一不满足即停并报告）

1. **契约已入 main（硬阻塞）**：`git fetch origin main` 后，确认 `MetBench_BLL.Core/SystemMT/Jobs/ISystemMtJobService.cs` 存在于 main。命令：`git cat-file -e origin/main:MetBench_BLL.Core/SystemMT/Jobs/ISystemMtJobService.cs && echo OK`。**不存在 → 立即停**：Cloud 计划未合入，VM 计划被 block，`dotnet build MetBench_Client` 必因缺 `MetBench_BLL.SystemMT.Jobs.*` 失败。
2. 计划文件存在且可读：`docs/superpowers/plans/2026-06-03-systemmt-async-execution-vm-plan.md`。
3. 环境是 **Windows**：`dotnet build MetBench_Client/MetBench_Client.csproj` 能跑（Linux 上必 MSB4019，说明走错环境）。先空跑一次确认当前 main 头能编译通过，作为「改动前可编译」基线。
4. 从含 job 契约的 main 切 VM 分支 `claude/vm-async-execution`；**该分支不含 `MetBench_BLL.Core`/`MetBench_DAL` 生产契约改动**（VM 只动 `MetBench_Client/` + 证据 docs，对齐 AC-V7）。
5. 读 `MetBench_BLL.Core/SystemMT/Launcher/MrRunResult.cs` 真实字段——计划 Task 1 的 `DescribeResult` 依赖它的「通过位 + 摘要」真实 API，不得用 `ToString()` 凑数。
6. 确认 `MetBench_Client` 现有 converters（`InverseBoolConverter` / `NullToCollapsedConverter`）——计划 Task 2 XAML 引用它们，缺则复用既有同义 converter，不新造重复实现（先读再写）。

## 核心步骤（计划已给完整代码，此处只列骨架，细节以计划为准）

1. **Task 1**：`SystemMtAsyncJobViewModel`（提交 + `DispatcherTimer` 定时 polling + 手动刷新 + 取消 + 结果投影）。注意：构造注入 `ISystemMtJobService` + `ISystemMtLauncher`（后者仅用于只读列举 MR id）。
2. **Task 2**：`SystemMtAsyncJobPage.xaml` + code-behind（CLAUDE.md §5 三件套，`INavigableView<SystemMtAsyncJobViewModel>`）。
3. **Task 3**：`SystemMtJobWorkerHostedService`（`BackgroundService`，进程内后台循环 `DequeueAsync` → `SystemMtJobWorker.RunJobAsync`，不碰 dispatcher）。
4. **Task 4**：`App.xaml.cs` DI 注册 + `MainWindowViewModel` 导航项。**关键决策（计划已挑明并选定）**：worker 是 Singleton、launcher 是 Scoped → 用 `IServiceScopeFactory` 在 hosted service 内 per-job `CreateScope()` 取 launcher，**不改既有 launcher 生命周期**。落地按此调整 worker/pipeline 注册，PR body 说明。
5. **Task 5**：`dotnet run --project MetBench_Client` → 导航「异步执行」页 → 逐条走 AC-V2…V8 截图 → 写证据 doc → 执行后回写（计划状态、活跃计划索引、AGENTS.md Stage 记录）。

> 边界硬约束（CLAUDE.md §9）：
> - **不修改任何 `MetBench_BLL.Core/SystemMT/*` public 类型**。若发现契约缺字段（如 `SystemMtJobStatus` 少 `SutName`），**不在 VM 侧改契约**——记为「需 Cloud 侧补充」反馈，按 §9 提 Cloud-side 变更。
> - 既有同步执行页保留不动；异步页是新增并行入口。
> - polling 与提交全部 `async Task`，**禁止** `.Result` / `.Wait()`（不阻塞 dispatcher）。

## 验收标准（VM 不经 CI，靠可复现证据；逐条出证）

以计划 §2 的 AC-V1…AC-V8 为准，证据落 `docs/superpowers/specs/2026-06-03-async-execution-vm-verification/`：

- **AC-V1 编译**：`dotnet build MetBench_Client/MetBench_Client.csproj` 0 error（贴构建输出尾部截图）。
- **AC-V2 提交即返回**：点提交后 UI **立即**显示非空 `JobId` + `Queued`，窗口仍可交互（提交瞬间截图）。
- **AC-V3 polling 推进**：状态自动 `Queued→Preparing→RunningSource→…→终止`，进度条同步（≥3 个不同状态连续截图，或 PollLog 截图）。
- **AC-V4 手动刷新**：点刷新立即触发一次 `GetStatusAsync` 并更新 UI（刷新前后截图）。
- **AC-V5 终止展示结果**：`Succeeded` 显示 `MrRunResult` 摘要；失败态显示 `FailureReason`（成功 + 一种失败各一张截图）。
- **AC-V6 不阻塞 dispatcher**：`grep -rn "\.Result\|\.Wait()" MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs MetBench_Client/Hosting/` 为空 + 提交时窗口可交互录屏。
- **AC-V7 契约零改动**：`git diff origin/main -- MetBench_BLL.Core MetBench_DAL` 输出为空。
- **AC-V8 取消**：对运行中 job 点取消 → `Cancelled`、停表（取消前后截图）。

> 建议：先用一个**短/确定性本地 MR**（秒级走到终止态）验证状态机闭环，再用长耗时 SUT 复测。

## Push cadence（对齐 exchange protocol，每 checkpoint commit+push）

1. 前置/head 校验通过 → push。
2. Windows 构建通过（AC-V1）→ push。
3. 提交 + polling 闭环（AC-V2/V3/V4）→ push。
4. 成功 + 失败 + 取消三态截图（AC-V5/V8）→ push。
5. AC-V6/V7 自查 + 最终证据 README → push，开 PR targeting `main`。

## 结论要求（实事求是，真实数据支撑）

提交结论时**必须**：

1. 给出**真实构建输出**：`dotnet build MetBench_Client` 的 error/warning 计数 + 实际产物路径，不写「构建通过」之类无数字断言。
2. 逐条 AC-V 标 **pass / fail / blocked**，每条附**真实截图文件名**（在 `2026-06-03-async-execution-vm-verification/` 内）+ 一句实际观测；截图必须是本次运行真截的，不得复用/伪造。
3. AC-V3 必须附**实际观测到的状态序列**（如 `Queued→Preparing→RunningSource→Asserting→Succeeded` 的真实时间戳），不得只写「状态会推进」。
4. AC-V7 必须贴 `git diff origin/main -- MetBench_BLL.Core MetBench_DAL` 的真实输出（应为空）；非空即违规，必须报告。
5. 任何未跑通 / 仅部分验证 / 用 fake 替代真 SUT 的情况，**显式说明**，不得用「已验证」笼统掩盖（CLAUDE.md §0 不允许只说不做、§6 显式报错）。
6. 若 AC-V1（编译）未过，结论写「VM 验证未开始 + 阻塞原因」，**禁止**声称 UI 已验证。
