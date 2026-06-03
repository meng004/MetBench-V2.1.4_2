# VM Agent Prompt — async 链尾 review VM 侧 cleanup（P1-VM / P3 / P4）

> **新会话使用方式**：在 **Windows VM** 上发指令 `读取 docs/superpowers/specs/2026-06-03-async-chain-end-vm-cleanup-prompt.md，执行任务`。
> 本文件即完整执行指令：依次完成「前置条件检查 → 核心步骤 → 验收标准 → 结论要求」四段。
>
> 角色：VM 轨（Windows + VS 2022 + WPF，**不经 CI**）。背景：async 链(#276–#280)链尾整体 review 发现 5 项;P1/P2 已由 Cloud PR #285 修;本提示词修 VM 侧 **P3/P4** + 接线 **P1 的 VM 半**。

## 前置条件检查（任一不满足即停并报告）

1. **Cloud 修复已入 main**：`git fetch origin main` 后确认 `IJobCancellationRegistry` 存在 —— `git cat-file -e origin/main:MetBench_BLL.Core/SystemMT/Jobs/IJobCancellationRegistry.cs && echo OK`。不存在 → 停（PR #285 未合，无法接线）。
2. **环境是 Windows**：`dotnet build MetBench_Client/MetBench_Client.csproj` 能跑(Linux 必 MSB4019)。先空跑当前 main 确认基线可编译。
3. 从 main 切分支 `claude/async-chain-end-vm-cleanup`；只动 `MetBench_Client/`(对齐 §9，契约零改动)。

## 核心步骤

### P1-VM：把 per-job 取消接线（让「取消」真正中断在跑的 SUT）

> Cloud 已提供 `IJobCancellationRegistry`/`JobCancellationRegistry`;`SystemMtJobService` ctor 已能可选注入它（MEDI 注册后自动注入）。VM 只需：注册 registry + 让 hosted service 把它传给 worker。

1. **`MetBench_Client/App.xaml.cs`** —— 在 async job 注册块（`IJobQueue`/`IJobStore`/`ISystemMtJobService` 附近，约 L189-196）加一行：
   ```csharp
   services.AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>();
   ```
   （`ISystemMtJobService` 用的是 `AddSingleton<ISystemMtJobService, SystemMtJobService>()`，registry 注册后 MEDI 会自动注入到 service 的可选参；service 侧无需再改。）确认 using `MetBench_BLL.SystemMT.Jobs` 已在。

2. **`MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs`** —— hosted service 是**手动 `new SystemMtJobWorker(...)`**，需显式传 registry：
   - 构造函数注入 `IJobCancellationRegistry cancellation`，存字段 `_cancellation`。
   - 把 `new SystemMtJobWorker(_store, pipeline)` 改为 `new SystemMtJobWorker(_store, pipeline, _cancellation)`。

### P3：轮询异常不再崩 dispatcher（`SystemMtAsyncJobViewModel.cs`）

3. `Tick` 处理器、`OnNavigatedTo`、`PollOnceAsync` 都是 async-void/async，未捕获异常会**崩 WPF dispatcher**。给 `PollOnceAsync` 和 `OnNavigatedTo` 各包 try/catch：
   - `PollOnceAsync`：catch 到异常时停表、`IsRunning=false`、把错误显示到 `FailureReason`（如 `FailureReason = $"轮询出错: {ex.Message}"`），**不再抛出**。
   - `OnNavigatedTo`/`LoadMrIdsAsync`：catch 到异常时给 `AvailableMrIds` 留空 + 在某个可见字段提示（如 `FailureReason = $"加载 MR 列表失败: {ex.Message}"`），不抛。

### P4：轮询重入守卫（`SystemMtAsyncJobViewModel.cs`）

4. 若一次 `GetStatusAsync` ≥ 1s，下一 `Tick` 会在前一个 `PollOnceAsync` 未完时再进 → 重复 LoadResult / 旧 job 结果覆盖新。加 `private bool _isPolling;` 守卫，与 P3 合并：
   ```csharp
   private async Task PollOnceAsync()
   {
       if (_isPolling) return;
       if (_currentJobId is not { } id) return;
       _isPolling = true;
       try
       {
           // …原有 PollOnceAsync 主体（GetStatusAsync → 投影 → 终止则停表+LoadResult）…
       }
       catch (Exception ex)
       {
           _pollTimer.Stop();
           IsRunning = false;
           FailureReason = $"轮询出错: {ex.Message}";
       }
       finally
       {
           _isPolling = false;
       }
   }
   ```

5.（可选，低优）VM 实现 `IDisposable` 或在 `OnNavigatedFrom` 确保停表；当前停表已足够（stopped DispatcherTimer 可被 GC）。如顺手可加，不强制。

6. **构建 + 运行验证**：`dotnet build MetBench_Client/MetBench_Client.csproj` 0 error → `dotnet run` 验证取消、轮询、异常路径。

## 验收标准

- **AC-1 编译**：`dotnet build MetBench_Client/MetBench_Client.csproj` **0 error**（贴构建输出尾部截图）。
- **AC-2 契约零改动**：`git diff origin/main -- MetBench_BLL.Core MetBench_DAL` 为空。
- **AC-3 取消真中断**：提交一个**够长**的 job（如临时让某 SUT 慢一点，或用 OpenMC venv 路径），点「取消」→ 观察 SUT 进程**实际停止**（不是只翻状态）+ 状态 `Cancelled` + 结果区无孤儿结果。证据：取消前后截图 + （可选）进程消失证据。
- **AC-4 轮询异常不崩**：人为制造一次 polling 异常（如取消运行中临时让 store 不可读，或注入异常路径），UI **显示错误信息**且**不崩**（窗口仍可交互）。证据：错误显示截图。
- **AC-5 重入安全**：快速连续触发刷新 + 定时 tick 重叠时，结果区不被旧 job 数据覆盖。证据：状态序列截图或 PollLog。
- **AC-6 既有 async 页不回归**：AC-V2/V3/V5（提交即返回、轮询推进、成功结果）仍正常。证据：截图。

## 结论要求（实事求是，真实数据支撑）

1. 贴 `dotnet build MetBench_Client` 真实 error/warning 计数，不写「构建通过」式无数字断言。
2. 逐条 AC 标 pass/fail，附**本次运行真截图文件名**（归档 `docs/superpowers/specs/2026-06-03-async-chain-end-vm-cleanup-verification/`）。
3. **AC-3 是关键**：必须给出取消**真的中断了 SUT**的证据（进程停止 / 时间戳），不能只证状态翻成 Cancelled —— 否则等于没修 P1。
4. 若某项仅编译通过未运行验证、或某 SUT 不便制造长任务而用了替代手段，**显式说明**（接 §0 / §6）。
5. AC-1 未过 → 结论写「VM cleanup 未完成 + 阻塞原因」，禁止声称已验证。

## 完成后

- 编译 + 验收通过 → 推 `claude/async-chain-end-vm-cleanup` → 开 PR targeting main（标 Windows-only，CI 不编译 WPF）。
- 本提示词对应 Cloud PR #285 的 VM 半;两者合入后 async 链尾 review 的 P1-P4 全部闭合（P5 已 documented-defer）。
