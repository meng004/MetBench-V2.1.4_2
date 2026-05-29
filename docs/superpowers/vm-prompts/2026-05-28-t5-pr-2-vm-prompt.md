# VM 提示词 — T5 PR-2: Import cross-program anomaly findings

> **使用方式**：在 Windows VM 中启动 Claude Code 会话后，粘贴本文件全部内容作为 prompt。
> **PR**: meng004/MetBench-V2.1.4_2#TBD (待 cloud agent 开 PR 后填入)
> **分支**: `claude/t5-pr-2-cross-program-import`
> **计划**: [`docs/superpowers/plans/2026-05-28-t5-anomaly-workflow-closure-plan.md`](../plans/2026-05-28-t5-anomaly-workflow-closure-plan.md) §4

---

## 项目背景与硬约束

同 PR-1 提示词。重点：CLAUDE.md §0.5 / §9 / §0。

---

## Step 1 拉最新代码并核对 HEAD

```powershell
git fetch origin
git checkout claude/t5-pr-2-cross-program-import
git pull --ff-only
git log --oneline -3
```

---

## Step 2 编译 + 测试

```powershell
dotnet build MetBench_Client/MetBench_Client.csproj -c Debug --no-restore
# 0 errors

dotnet test MetBench_SystemMT.Tests --no-build
# 期望: 1515 (PR-1 baseline) + 5 facts (CrossProgramAnomalyImportTests) = 1520 / 0 / 12
```

---

## Step 3 跑 import 工具产出 JSON

```powershell
python tools/import_cross_program_anomalies.py `
    --input docs/experiments/cross-program-report.md `
    --output docs/experiments/cross-program-anomalies-2026-05-28.json

# 验证 JSON 存在且非空
type docs/experiments/cross-program-anomalies-2026-05-28.json | Select-String "ScaleModeratorSigmaA"
```

期望：JSON 含至少 1 个 case，其 `transform: "ScaleModeratorSigmaA"`、`delta_k > budget`、`classification: "DISAGREE"`。

---

## Step 4 跑 seed 工具入库到 LiteDB

```powershell
dotnet run --project tools/seed_cross_program_anomalies -- `
    --input docs/experiments/cross-program-anomalies-2026-05-28.json `
    --db MetBench_Client/bin/Debug/net8.0-windows7.0/SystemMT.Litedb
```

期望：控制台输出 `Seeded N anomalies (category=cross-program-disagreement)`。

---

## Step 5 UI 验证（截图存 `docs/superpowers/specs/2026-05-28-t5-pr-2-vm-verification/`）

```powershell
dotnet run --project MetBench_Client
```

按顺序截图：

1. **`01-litedb-after-seed.png`**：用 LiteDB Studio 打开
   `MetBench_Client/bin/Debug/net8.0-windows7.0/SystemMT.Litedb`
   截图 `Anomaly` collection 中 `Category="cross-program-disagreement"` 的 row。展开 row 显示 Severity=critical / Status=investigating / Notes 含 discussion-phase2.md 引用。

2. **`02-anomalylist-cross-program-row.png`**：nav 进 "Anomalies"，DataGrid 显示包含 cross-program-disagreement category 行（与其他 single-point / runner-failure 行区分）。

3. **`03-filter-by-category.png`**：filter 工具栏选择 `Category=cross-program-disagreement`，DataGrid 仅显示该类型行。

4. **`04-commonality-report.png`**：点 "Analyze commonality" 按钮，弹出报告窗口或状态栏显示 `ByCategory: cross-program-disagreement=N`。

5. **`05-row-details.png`**：单击 cross-program-disagreement 行，详情面板（或扩展行）显示 Notes 字段含 `OpenMOC × OpenMC` + `ScaleModeratorSigmaA` + `|Δk| ≈ 49%` 等关键信息。

6. **`06-replay-disabled.png`**：尝试点击 "Replay this anomaly" 按钮。
   - 期望：按钮 disabled 或点击后状态栏报告 "Cross-program anomalies cannot be replayed (no source Execution row)"
   - 如按钮可点且崩溃 → 在 PR comment 报告，**不自行修 backend**

---

## Step 6 VM SHA 回写

```powershell
git status
git add docs/superpowers/specs/2026-05-28-t5-pr-2-vm-verification/ docs/experiments/cross-program-anomalies-2026-05-28.json
git -c commit.gpgsign=false commit -m "docs(t5-pr-2-vm): 6 screenshots + cross-program JSON from VM verification"
git push
```

在 PR body §4 Windows 节追加：

````markdown
✅ VM verification complete at SHA `<git rev-parse HEAD>`:

| Check | Result |
|---|---|
| `dotnet build` | 0 errors |
| `dotnet test` | <P> / 0 / 12 |
| `python tools/import_cross_program_anomalies.py` | JSON 产出 N cases incl. ScaleModeratorSigmaA |
| `dotnet run --project tools/seed_cross_program_anomalies` | Seeded N anomalies |
| UI verification | 6 screenshots at `docs/superpowers/specs/2026-05-28-t5-pr-2-vm-verification/` |
| Cross-program row visible | ✓ Category filter + commonality report 含 cross-program-disagreement |
| Replay disabled for cross-program | ✓ (or graceful error) |
````

---

## 重点验证 — cross-program 入库 acceptance gate

| Gate | 标准 |
|---|---|
| ✓ Step 3 | JSON 含 ScaleModeratorSigmaA |
| ✓ Step 4 | LiteDB Anomaly collection 多了 N row |
| ✓ Step 5.2 | AnomalyListPage 看到 cross-program 行 |
| ✓ Step 5.3 | category filter 工作 |
| ✓ Step 5.6 | Replay 对该 category 不崩溃 |

---

## 异常处理

- import 工具解析失败 → 在 PR comment 报告解析错误，**不自行改 tools/**
- seed 工具崩溃 → 报告控制台 stack trace
- LiteDB row 写入失败 → 检查 db 文件 locked / 权限

---

## 完成后通知 cloud agent

回复 summary 含 VM HEAD SHA / build / test 数字 / JSON cases 数 / seeded Anomaly 数 / 6 截图路径。
