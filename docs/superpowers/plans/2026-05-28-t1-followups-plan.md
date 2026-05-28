# T1 非 MR CRUD 链路 — 4 项 follow-up 计划

> **Date**: 2026-05-28
> **Status**: Active — 与 PR-5 (#229) 同日落地，处理 T1 链路收口后的 4 项 follow-up
> **Branch**: `claude/t1-followups-plan`
> **Driver**: PR #229 squash commit 末尾列出的 4 项建议；CLAUDE.md §12.4 R2 强制要求 ≥3-PR 链路必须做 chain-end holistic review，T1 非 MR CRUD 是 6-PR 链路

---

## §1 目标 (Goal)

把 T1 非 MR CRUD 6-PR 链路（PR #219 / #221 / #223 / #225 / #224 / #229）合后建议的 4 项 follow-up 推到 Controlled / Decided / Deferred 三态之一，按各自的可执行性差异化处理。

---

## §2 4 项 follow-up 处理路径

| # | 项 | 类型 | 落地路径 | 目标态 |
|---|---|---|---|---|
| **F1** | Chain-end holistic review | Process（必做，CLAUDE.md §12.4 R2） | Fresh-session Explore agent 跑 cumulative diff `origin/main~6..HEAD`；落 review spec | Controlled — 0 finding 或 cleanup PR 闭环 |
| **F2** | MessageBox grep / analyzer 规则 | Code（governance）| 在 `.github/workflows/dotnet-test.yml` `governance` job 加 G12 规则；test 文件落 fact 验规则触发 | Controlled — grep 守护 + fact 通过 |
| **F3** | Save target bin/Debug UX | Decision spec | 写 `docs/superpowers/specs/2026-05-28-sutroot-bin-debug-decision.md` 决定"接受 / 修改"；不动代码 | Decided — 决策落档 |
| **F4** | Anomaly orphans for T5 | Scoped plan for T5 | 写 `docs/superpowers/plans/2026-05-28-t5-anomaly-cleanup-scoped-plan.md` 给 T5 预留接入面 | Deferred — 范围明示，等 T5 启动 |

---

## §3 F1 · Chain-end holistic review

### §3.1 执行

按 CLAUDE.md §12.4 R2 与 [`chain-end-review-checklist.md`](../templates/chain-end-review-checklist.md)：

1. Cloud agent 起 fresh-session **Explore agent**，prompt 含：
   - 6-PR 累计 diff 范围 `origin/main~6..origin/main` (从 `aa4d11e` 往前 6 个 commit)
   - 6 PR 列表 + plan 路径 + R1-R4 元规则检查清单
   - 重点：cross-PR 一致性、retrospective drift、parity test 缺漏、契约 / fact 不配对
2. Agent 产出 review spec 至 `docs/superpowers/specs/2026-05-28-t1-non-mr-crud-chain-post-merge-review.md`
3. 按 finding 类别处理：
   - Cat-A 单 PR 可见：归类、记录、（如有）开 cleanup PR
   - Cat-B 跨 PR / retrospective：codify 为新 grep / analyzer / spec（与 F2 / F3 / F4 合流）

### §3.2 验收

- review spec 文件存在且签 ID
- 0 Cat-A finding 或 cleanup PR 已开
- Cat-B finding 已转 §12.5 模块 B/C 守卫一行

---

## §4 F2 · MessageBox grep 规则（G12）

### §4.1 背景

PR-2 (#223 Equation) / PR-3 (#225 SampleCase) / PR-4 (#224 ExecHistory) 三次同款 `MessageBox` 二义性 CS0104 bug：文件 `using System.Windows;` + `using Wpf.Ui.Controls;` 同时含，`MessageBox` / `MessageBoxButton` / `MessageBoxResult` 二义。Linux CI 不编 WPF 看不见。

VM agent 在 PR-3 body 明示建议 codify 为 grep rule / analyzer。

### §4.2 实现

在 `.github/workflows/dotnet-test.yml` `governance` job 末尾加 G12 advisory grep：

```bash
# G12 — WPF MessageBox namespace ambiguity guard
# Catches `MessageBox` / `MessageBoxButton` / `MessageBoxResult` /
# `MessageBoxImage` usage in MetBench_Client/ files that also import
# both System.Windows and Wpf.Ui.Controls — the Wpf.Ui.Controls.MessageBox
# overload shadows the System.Windows one and breaks compile on Windows.
violations="$(grep -rlE '\busing System\.Windows;' MetBench_Client/ 2>/dev/null | \
  xargs -I{} grep -lE '\busing Wpf\.Ui\.Controls;' {} 2>/dev/null | \
  xargs -I{} grep -lE '\b(MessageBox|MessageBoxButton|MessageBoxResult|MessageBoxImage)\b(?!\s*\.)' {} 2>/dev/null | \
  xargs -I{} grep -L 'System\.Windows\.MessageBox' {} 2>/dev/null || true)"
if [ -n "$violations" ]; then
  echo "::warning::G12 — unqualified MessageBox under dual namespace import:"
  echo "$violations" | sed 's/^/  /'
fi
```

`grep -L` 反向：列出**没有**显式 `System.Windows.MessageBox` 限定的文件，但同时 `using System.Windows;` + `using Wpf.Ui.Controls;` + `MessageBox` 字面出现。advisory 不阻塞。

### §4.3 fact

新增 `MetBench_SystemMT.Tests/Governance/MessageBoxAmbiguityGrepTests.cs`（如 governance 目录不存在则创建）：

```csharp
[Fact]
public void Grep_pattern_catches_unqualified_messagebox_in_dual_using_file_synthetic()
{
    // synthetic file 在 tmp 目录写 dual-using + unqualified MessageBox
    // 跑 grep 字符串模式（不走 workflow），断言匹配返回 1
}

[Fact]
public void Grep_pattern_skips_fully_qualified_messagebox()
{
    // synthetic file 用 System.Windows.MessageBox 全限定
    // 断言匹配返回 0
}
```

### §4.4 验收

- workflow 文件可正确解析（yaml lint）
- 2 facts 全绿
- 跑一次 PR — `governance` job 不 fail（warning 可有）
- 实际仓库 `MetBench_Client/` 现状 grep → 0 命中（因 3 PR 已修）

---

## §5 F3 · Save target bin/Debug UX 决策 spec

### §5.1 现象（已观察）

`MetBench_Client/App.xaml.cs:134-141` 注册 `LauncherOptions`：

```csharp
SutRoot: Path.Combine(
    Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
    "SUT")
```

`Assembly.GetEntryAssembly().Location` 在 `dotnet run` 下 = `bin/Debug/net8.0-windows7.0/MetBench_Client.dll`，所以 `SutRoot` = `bin/Debug/net8.0-windows7.0/SUT/`，是项目构建时拷贝的副本。

UI 端的 SUT / Equation / SampleCase / MR catalog 编辑全部写到 bin/Debug，**源码 SUT/ 目录保持 git-clean**。

### §5.2 选项

| 选项 | 优 | 劣 |
|---|---|---|
| **A 接受现状** | 生产部署 (.exe + SUT) 时 SutRoot 就是 .exe 同级；行为正确 | dev 体验差，用户改完看不到 git diff |
| **B dev 模式探测，回写源 SUT/** | dev 流程顺畅 | 探测启发式（如 path 含 bin/Debug）易脆；可能误判 publish 输出 |
| **C 添加 `--sut-root` 命令行 / 配置覆盖** | 显式、可测 | 加表面 + UI 需暴露 |

### §5.3 决策（待 spec 落定）

倾向 **A 接受 + 文档化**：在 `SystemMtSutCatalogPage.xaml` / 其他 catalog 页加 footer 提示「保存写入到 SutRoot，dev 模式下为 bin/Debug 副本；提交需 `xcopy bin/Debug/SUT/<name>/catalog.json SUT/<name>/`」。

Spec 文件 `docs/superpowers/specs/2026-05-28-sutroot-bin-debug-decision.md` 落档：现象 / 三选项利弊 / 选 A 的理由 / 文档化点的具体位置。

### §5.4 验收

- spec 文件存在并签决策
- 不动 BLL.Core / DAL / WPF 任何代码（VM-track 后续按 footer 提示视情况实现）

---

## §6 F4 · Anomaly orphans for T5 scoped plan

### §6.1 背景

PR-4 (#224) `IExecutionHistoryEditor.DeleteAsync` 顺序：
1. `IExecutionEvidenceRepository.DeleteByExecutionIdAsync(executionId)`
2. `ISystemMtResultRepository.DeleteAsync(executionId.ToString())`

但 `IAnomalyService` 在 `SystemMtLauncher.RecordAnomalyIfFailedAsync` 路径写入的 Anomaly 行**不被级联删除**。删 Execution 后对应 Anomaly 行变成悬空孤儿。

### §6.2 T5 接入面（spec 写明）

T5 anomaly cleanup workflow 接入时需：
- `IAnomalyRepository` 加 `DeleteByExecutionIdAsync(Guid executionId)`（已存在与否 grep 确认）
- `IExecutionHistoryEditor.DeleteAsync` 顺序扩展为 3 步：Evidence → Result → Anomaly
- 失败模式 ExecutionHistoryDeleteResult 扩 4-segment：`Deleted / EvidenceOnly / ResultOnly / Failed`
- 或者 T5 提供独立 "orphan anomaly sweep" service：定期扫 Anomaly + 对照 Execution 表删 orphan
- 选哪条由 T5 plan 决定

### §6.3 验收

- spec 文件 `docs/superpowers/plans/2026-05-28-t5-anomaly-cleanup-scoped-plan.md` 落档
- 内容含 接入面定义 + 两条候选路线 + 待定 T5 选型问题清单

---

## §7 PR 顺序

| PR | 内容 | 类型 |
|---|---|---|
| **PR-FUP-0** (this) | docs 计划（本文件）落地 | cloud, docs-only |
| **PR-FUP-1** | F2 (MessageBox G12 grep + facts) + F3 (UX decision spec) + F4 (T5 anomaly cleanup scoped plan) 一并落地 | cloud, code + docs |
| **PR-FUP-2** | F1 chain-end review spec + (如有) cleanup findings | cloud, docs (+ code if findings需) |

PR-FUP-0 与 PR-FUP-1 可同一 PR 合并，因为本 plan + 实现内容互补；PR-FUP-2 因需 fresh-session 独立 agent 产出，分开。

---

## §8 完成后状态（docs/status/current.md 写入）

> | T1 非 MR CRUD 链路 follow-ups | Controlled — 4 项闭环 | F1 chain-end review 通过 PR-FUP-2 `docs/superpowers/specs/2026-05-28-t1-non-mr-crud-chain-post-merge-review.md` 落档（X Cat-A / Y Cat-B findings）；F2 MessageBox G12 grep 通过 PR-FUP-1 落地，`.github/workflows/dotnet-test.yml` governance job 新增 G12 advisory + 2 facts；F3 SutRoot bin/Debug UX 决策为"接受 + 文档化"，spec `docs/superpowers/specs/2026-05-28-sutroot-bin-debug-decision.md`；F4 T5 anomaly cleanup 接入面 spec `docs/superpowers/plans/2026-05-28-t5-anomaly-cleanup-scoped-plan.md`。|

---

## §9 闭环验收（对照 CLAUDE.md §11.2）

- [ ] 所列事实已对当前分支核实：`Assembly.GetEntryAssembly().Location` 路径行为已观察 / `IAnomalyService.RecordAnomalyAsync` 在 launcher fail 路径调用 / governance job grep 块结构已确认
- [ ] `AGENTS.md` / 本 plan / `CLAUDE.md` 三者无内容复制：仅指针互引
- [ ] PR-FUP 全部合后 `docs/status/current.md` + active plan index 同步更新
