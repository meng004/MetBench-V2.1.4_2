# T5 异常工作流收口计划 — Cloud + VM

> **Date**: 2026-05-28
> **Status**: Draft — 待 PR-0 docs gate 合入后激活
> **Branch (cloud)**: `claude/t5-anomaly-workflow-closure-plan`
> **Driver**: PR #229 T 计划评估 — T5 仅剩两个真实缺口：(1) F4 orphan sweeper (从 T1 PR-4 延期 / scoped plan 已落 `2026-05-28-t5-anomaly-cleanup-scoped-plan.md`)；(2) 跨程序疑似缺陷（OpenMOC × OpenMC `ScaleModeratorSigmaA` |Δk|=49% 超预算）至今仅存活在 `docs/experiments/cross-program-report.md`，未作为 Anomaly row 入库流转。
> **VM 提示词**：本 plan 不在响应里贴 VM prompt；VM 端 Claude Code 读取 [`docs/superpowers/vm-prompts/`](../vm-prompts/) 目录下对应 PR 的提示词文件后执行。

---

## §1 目标 (Goal)

把 T5 异常工作流推到 **100% Controlled with chain-end review closed**。范围：

| T5 子项 | 现状（PR #231 后） | 本计划目标 |
|---|---|---|
| 异常查询 / 过滤 / 状态机 / 共性分析 | ✅ Controlled (AnomalyService + AnomalyListPage) | 不动 |
| 回放（version × MR × input 三元组） | ✅ Controlled (ReplayContextBuilder + ReplayResultPage) | 不动 |
| KnownBug 链接 | ✅ Controlled (AnomalyService.LinkToKnownBug + Anomaly.LinkedKnownBugId) | 不动 |
| **Orphan sweeper** (PR-4 F4 deferred) | ❌ 不存在 | **新增** `IAnomalyOrphanSweeper` 后端 + AnomalyListPage 加 "Sweep orphans" 按钮 |
| **跨程序疑似缺陷 → Anomaly DB 入库** | ❌ 仅 markdown 报告 | **新增** import 工具 + 把 `ScaleModeratorSigmaA` case 作为 confirmed Anomaly 入库 |
| Status 状态机转换规则 | ⚠️ 当前 `TransitionStatus` 无验证（任意 string） | **可选** 加 transition rules 验证（评估后决定） |

**100% 验收准则**：

1. `IAnomalyOrphanSweeper` 后端服务实现 + 6+ facts 守护
2. AnomalyListPage "Sweep orphans" 按钮 + ViewModel command；VM 截图验证可触发清理
3. `tools/import_cross_program_anomalies.py` 把 cross-program-report 中超预算 case 导入 Anomaly DB；至少 ScaleModeratorSigmaA case 入库且可在 AnomalyListPage 看见
4. `docs/status/current.md` §3 新增 "T5 异常工作流" 行 Controlled；旧 "OpenMOC × OpenMC 已检出疑似缺陷待确认" 悬挂语清除
5. PR-FUP-2 (#231 chain-end review 模式) F2-CatB `LegacyResultRecordParityTests.cs` 实施纳入本计划 backlog（可选）

**不在 scope**：
- 不动 `AnomalyService` 已有方法签名（仅追加新方法）
- 不重写 `AnomalyListPage` / `ReplayResultPage`（仅追加按钮 / 列）
- 不引入新 LiteDB collection
- T6 变异 / T4 LLM 端点配置
- Status 状态机有限自动机规则化（如 transition 验证）—— 评估后**不在本计划，单独 PR**

---

## §2 PR 顺序（4 个 PR）

| PR | 标题 | 类型 | 验证位置 | 依赖 | VM prompt 路径 |
|---|---|---|---|---|---|
| **PR-0** | `docs(plan): gate T5 anomaly workflow closure` | cloud, docs-only | Linux CI | — | N/A |
| **PR-1** | `feat(t5): add orphan sweeper backend + UI button` | cloud + VM verify | Linux CI + Windows VM | PR-0 | `docs/superpowers/vm-prompts/2026-05-28-t5-pr-1-vm-prompt.md` |
| **PR-2** | `feat(t5): import cross-program anomaly findings to LiteDB` | cloud + VM verify | Linux CI + Windows VM | PR-0 | `docs/superpowers/vm-prompts/2026-05-28-t5-pr-2-vm-prompt.md` |
| **PR-3** | `docs(status): refresh ledger after T5 closure chain` | cloud, docs-only | Linux CI | PR-1 + PR-2 | N/A |

PR-1 与 PR-2 PR-0 合后并行；PR-3 等 PR-1 + PR-2 合后。

---

## §3 PR-1 · Orphan sweeper 后端 + UI button

### Cloud 后端（按 F4 scoped plan Route B）

新文件：

- `MetBench_BLL.Core/SystemMT/Anomaly/IAnomalyOrphanSweeper.cs`：单方法接口
  ```csharp
  Task<AnomalyOrphanSweepResult> SweepAsync(CancellationToken ct = default);
  ```
- `MetBench_BLL.Core/SystemMT/Anomaly/AnomalyOrphanSweeper.cs`：实现
  ```csharp
  public AnomalyOrphanSweeper(IAnomalyRepository anomalies, ISystemMtResultRepository results)
  ```
  逻辑：枚举 `anomalies.GetAll()`；对每个 `Anomaly.ResultId` 调 `results.GetAsync(resultId)`；null → 标 orphan candidate；调 `anomalies.Remove(anomaly)` 删除；统计 `(SweptCount, RetainedCount, FailedCount)`
- `MetBench_BLL.Core/SystemMT/Anomaly/AnomalyOrphanSweepResult.cs`：3-segment record
- `MetBench_SystemMT.Tests/SystemMT/Anomaly/AnomalyOrphanSweeperTests.cs`：6 facts
  - happy: 3 orphan + 2 non-orphan → SweptCount=3 / RetainedCount=2 / Failed=0
  - 全 non-orphan: SweptCount=0 / RetainedCount=N
  - 全 orphan: SweptCount=N / RetainedCount=0
  - repo throw: Failed 计数 + 不中断后续
  - null repo: ArgumentNullException
  - Cancellation: 中途取消正确响应

### Cloud WPF 源（VM 验证）

- `MetBench_Client/ViewModels/AnomalyListViewModel.cs` 追加：
  ```csharp
  [RelayCommand]
  private async Task SweepOrphansAsync() {
      var result = await _orphanSweeper.SweepAsync();
      StatusMessage = $"Swept {result.SweptCount}, retained {result.RetainedCount}, failed {result.FailedCount}.";
      await ReloadAsync();
  }
  ```
- `MetBench_Client/Views/Pages/AnomalyListPage.xaml`：filter 工具栏旁追加 `ui:Button Content="Sweep orphans" Command="{Binding ViewModel.SweepOrphansCommand}"`
- `MetBench_Client/App.xaml.cs`：`services.AddScoped<IAnomalyOrphanSweeper, AnomalyOrphanSweeper>();`

### VM prompt

由 VM agent 读取 `docs/superpowers/vm-prompts/2026-05-28-t5-pr-1-vm-prompt.md` 后执行，含：
- pull + build + test
- 触发 "Sweep orphans" 按钮（可先通过删除一个 Execution 制造 orphan）
- 截图 LiteDB 前后 Anomaly collection 状态
- 写回 PR body Windows 节

---

## §4 PR-2 · 跨程序疑似缺陷导入

### Cloud 工具

- `tools/import_cross_program_anomalies.py` — Python 工具：
  - 输入：`docs/experiments/cross-program-report.md` 中表格 + `tools/cross_program_mr.py` 输出 JSON（如存在）
  - 解析 DISAGREE 行（`|Δk|` > budget）
  - 输出：JSON 文件 `docs/experiments/cross-program-anomalies-2026-05-28.json` 含每个超预算 case 的 (sut, mr_id, source_value, follow_up_value, delta_k, budget, classification)
- `MetBench_SystemMT.Tests/Anomaly/CrossProgramAnomalyImportTests.cs`：5 facts，用 fixture markdown 验证 import 工具产出 JSON 结构正确

### Cloud 一次性入库（admin script）

- `tools/seed_cross_program_anomalies.cs` —— 一次性 .NET console 工具（可选；或文档化手动 LiteDB Studio insert）
  - 读上述 JSON
  - 对每个 case：通过 `IAnomalyService.RecordAnomalyAsync(mrName, resultId: 占位 Guid, severity: "major"/"critical", category: "cross-program-disagreement", typedVerificationSummary: 详情)` 写入 Anomaly
  - 立即 `TransitionStatus → "investigating"` 标已知

### 数据决策

- ScaleModeratorSigmaA case 入库的 severity: **"critical"**（|Δk|=49% 远超预算）
- category: **"cross-program-disagreement"**
- 其他超预算 case（如有）按相同规则入库
- Anomaly.Notes = 引用 `docs/experiments/discussion-phase2.md` 的根因分析

### VM prompt

由 VM agent 读取 `docs/superpowers/vm-prompts/2026-05-28-t5-pr-2-vm-prompt.md` 后执行，含：
- pull + build + test
- 跑 seed 工具
- 打开 AnomalyListPage 验证 cross-program-disagreement category Anomaly 可见
- 截图 + 写回 PR body

---

## §5 PR-3 · Ledger refresh

合 PR-1 + PR-2 后：

- `docs/status/current.md` §3 新增 "T5 异常工作流 (Orphan sweeper + cross-program defect 入库)" 行 Controlled
- 删除 / 修订当前 §3 中 "OpenMOC × OpenMC 已检出一例疑似缺陷待确认" 悬挂语
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` 标本 plan Completed

---

## §6 Cloud vs Windows-side 切分

| 工作项 | Linux Cloud 可做+验 | Windows VM 必须验 |
|---|---|---|
| `IAnomalyOrphanSweeper` 后端 + 6 facts | ✅ Linux CI hard `test` 全绿 | — |
| `AnomalyListViewModel.SweepOrphansAsync` + DI + XAML | ✅ 写 WPF 源码 | ❌ Linux 不编 WPF；必须在 VM `dotnet build` 0 errors |
| WPF UI 触发 sweep 实测 | ❌ | ✅ VM 跑 + 截图 + LiteDB Studio 对照 |
| `tools/import_cross_program_anomalies.py` + 5 facts | ✅ Linux CI 全绿 | — |
| `tools/seed_cross_program_anomalies.cs` 实跑入库 | ⚠️ Linux 可跑但 SystemMT.Litedb 在 VM 上 | ✅ VM 实跑 + 验 Anomaly 可见 |

---

## §7 风险

| ID | 风险 | 缓解 |
|---|---|---|
| **R1** | Sweep 误删未来需保留的 orphan（如临时回放残留） | 加 `Anomaly.RecordedAtUtc < (Now - 7 days)` 时间窗保护；默认非破坏性（dryRun 标志） |
| **R2** | cross-program report 解析正则脆弱（report 格式可能更新） | fixture 测试覆盖；非确定性失败时人工核对 |
| **R3** | seed 工具用占位 ResultId 导致回放失败 | category="cross-program-disagreement" 显式语义；UI 在该 category 行禁用 Replay 按钮 |
| **R4** | VM 验证依赖 LiteDB Studio 第三方工具 | 在 prompt 中明示 `dotnet ef` 或 `litedb-tool` 命令行替代 |
| **R5** | PR-1 / PR-2 改 `AnomalyListPage.xaml` + `App.xaml.cs` 冲突 | 同 T1 R5：每 PR 只追加自身行；冲突时按合入顺序 rebase |

---

## §8 完成后状态（PR-3 写入 `docs/status/current.md` §3）

> | T5 异常工作流 | Controlled — orphan sweeper + cross-program 疑似缺陷入库 | PR #{N1} (`feat(t5): orphan sweeper backend + UI button`)、PR #{N2} (`feat(t5): import cross-program anomaly findings`)、PR #{N3} (ledger refresh)。`IAnomalyOrphanSweeper` 提供 Route B (per F4 scoped plan) 独立 sweep service，UI 按钮触发；ScaleModeratorSigmaA case (|Δk|=49% 超 budget) 入 Anomaly DB 作 `category="cross-program-disagreement" severity="critical" status="investigating"` 流转。CLAUDE.md §2.2 T5 所列范围（查询 / 过滤 / 状态机 / 共性分析 / 回放 / 三元组绑定 + 孤立清理）现已全部 Controlled。|

---

## §9 闭环验收（对照 CLAUDE.md §11.2）

- [ ] 所列事实已对当前分支核实：`IAnomalyRepository` 5 方法 / `AnomalyService` 公开方法表 / `ReplayContextBuilder.Build` 输入输出 / `cross-program-report.md` ScaleModeratorSigmaA 数据
- [ ] `AGENTS.md` / 本 plan / `CLAUDE.md` 三者无内容复制：仅指针互引
- [ ] PR 全合后 `docs/status/current.md` + active plan index 同步更新
- [ ] VM prompts 已 push 到 `docs/superpowers/vm-prompts/` 且 VM agent 可直接读取执行
