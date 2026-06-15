# SP4 每 SUT/MR WPF 异步页 UI 证据 Implementation Plan

**状态**: 完成待 PR —— 38 MR 异步页全跑：**33 job-Succeeded（UI 证据，含 3 个 openmc 作业 Succeeded 但 MR 违例=异常）+ 5 job-Failed**（openmoc×3 host 无运行时=容器侧、SP1 已覆盖；csv-roundtrip/projectile×2 = 异步页 JSON 解析发现）。证据 `docs/superpowers/specs/2026-06-16-sp4-async-ui-evidence/`（sp4-results.csv + 38 终态截图 + sp4-summary.md）。
**Spec 锚**: `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-acceptance-design.md` §SP4（"每 SUT/MR WPF 异步页 UI 证据"）
**分支**: `sp4-async-ui-evidence`
**前序**: SP1(#364)/SP2(#365)/SP3a(#366)/SP3b(#367) 已合并。

**Goal:** 为运行时 catalog 的每个 MR，在真实 WPF **异步执行页**（`Nav_SystemMtAsyncExecution`）经"选 MR→提交→轮询到终态→4 截图"采集 UI 证据，证明异步执行页对每个已导入 SUT/MR 可用。

**Architecture:**
- 复用 `tools/uia-acceptance` 的**原始 `--mr` 异步模式**（PR #280 已验证）：`--exe <client> --mr <mrId> --case <label> --evidence <dir>` → 启动→导航异步页→选 MR→Submit→轮询 `AsyncState` 到 `Succeeded/Failed/Cancelled`→4 张截图（startup/asyncpage/submitted/terminal）。
- `tools/sp4_run_all.ps1`：批量循环器，对 38 个 MR ID 逐个：前后 `Stop-Process MetBench_Client`，设 `METBENCH_SYSTEM_PYTHON`，调 `--mr`，记录每 MR 终态 + 退出码到 `sp4-results.csv`。后台运行（~25min 墙钟）。
- 38 个 MR ID（异步页 AsyncMrCombo 实测枚举）分两类运行时：
  - **host 可跑（~32，pure-stdlib + scipy，经 METBENCH_SYSTEM_PYTHON）** → 期望 `Succeeded`。
  - **openmoc/openmc（6：openmc-pincell-{nu-sigma-f,particle-count-convergence,sigma-a} + openmoc-pincell-{nu-sigma-f,ray-track-convergence,sigma-a}）** → host 无 venv，T1 运行时预检 fail-closed 到 `Failed` 终态（诚实证据：异步页对缺运行时正确处置；真 Succeeded 需 `metbench-runtime` 容器，但 WPF GUI 不在容器内跑——记为容器侧/已由 SP1 容器内 xUnit 覆盖）。

**Tech Stack:** .NET 8 WPF Release exe、FlaUI/UIA3 `--mr` 模式、PowerShell 批量循环、PrintWindow 截图。

**执行约定：** `METBENCH_SYSTEM_PYTHON`=codex python；每 MR `--timeout-seconds 150`；§4 真实验证、§6 显式报错（Failed 如实记，不掩盖）。CI 门禁不变。

---

## Tasks

### Task 1: 批量循环器 + spec/branch
- [x] 异步页 AsyncMrCombo 枚举 38 个 MR ID。
- [ ] 写 `tools/sp4_run_all.ps1`（前后清进程 + env + 逐 MR `--mr` + 终态 CSV）。
- [ ] 创建分支 `sp4-async-ui-evidence`。

### Task 2: 全量异步页 UI 跑
- [ ] 后台跑 38 MR；采每 MR 4 截图 + 终态。
- [ ] 汇总：host-runnable → Succeeded 计数；openmoc/openmc → Failed(preflight) 如实记。

### Task 3: 证据 + 状态投影 + PR
- [ ] `docs/superpowers/specs/2026-06-16-sp4-async-ui-evidence/`：per-MR 截图 + `sp4-results.csv` + `sp4-summary.md`（38 MR 终态表 + 运行时分类 + 容器侧说明）。
- [ ] current.md + active index + 本 plan 状态；按 7 节 checklist 开 PR（Windows classification=run-and-log）。

---

## 判定原则
- 终态 `Succeeded` = 该 MR 异步页 UI 证据 ✅；`Failed`（openmoc/openmc preflight，host 无 venv）如实标，归类为容器侧（SP1 容器内 xUnit 已真跑通这些运行时）。
- 禁止伪造截图；禁止把 Failed 粉饰为通过。

## PR Gate Classification
- Scope：单一目的——SP4 异步页逐 MR UI 证据。
- Windows：`run-and-log`。代码仅 `tools/` 循环脚本 + 证据；不碰生产/CI。
- 模块 E：单 PR。
