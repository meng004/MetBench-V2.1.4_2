# UAT Dashboard — 历次轮次趋势

> 每轮 UAT merge 后，下发人在本表追加一行；趋势用于决策 release / regression / improvement priority。

## 总览

| 轮次 | 日期 | commit | 测试员 | 平台 | Pass% | Blocker | Major | Minor | 总评 | 报告 |
|------|------|--------|--------|------|-------|---------|-------|-------|------|------|
| baseline-1 | 2026-05-16 | `97863ea` | dev | Linux | 100% | 0 | 0 | 0 | reference | [`baseline-2026-05-16/`](baseline-2026-05-16/) |
| baseline-2 | 2026-05-17 | `45a145f` | dev | Linux + OpenMC | **100%** | 0 | 0 | **0** | reference (post W11-W12, 100% pass) | [`baseline-2026-05-17/`](baseline-2026-05-17/) |
| round-1 | 2026-05-18 | `0c0cd24` | limeng | Windows 11 (Parallels) UI | **42%** (11/26 incl. 5 cloud) | 0 | 3 | 10 | **CONDITIONAL PASS** | [`round-1-limeng-2026-05-18/`](round-1-limeng-2026-05-18/) |
| round-2 | 2026-05-19 | `9b89f9b` | limeng | Windows 11 ARM (Parallels) UI | **100%** (5/5) | 0 | 0 | 0 | **PASS** | [`round-2-windows-2026-05-19-limeng/`](round-2-windows-2026-05-19-limeng/) |

## 趋势分析约定

下发人在每轮 merge 后 5 分钟更新本表 + 在下方写 2-3 句 commentary：

- **regression**：相邻轮次某类用例从 ✅ 变 ❌ → 标 🔴
- **new coverage**：本轮新增 UC → 标 🟢
- **flakiness**：同一 UC 多轮内偶尔 ❌ → 标 🟡

## Commentary（追加式，倒序）

### 2026-05-16 baseline-1
开发侧自跑作为 reference：458 facts pass / 0 fail / 22.35s cumulative。OpenMOC venv OK。全部 7 类的 CLI 用例都跑通。

### 2026-05-17 baseline-2
Post W11-W12 (8 PR land): W11.2 Multi-LLM 真实跑通 + W12 F13 OpenMC 接入 + scenario→MR 改名 + UAT BDD 21 用例 + LiteDB schema migration + UAT 三段式重写 + W12 F11 monitor。新增 OpenMC venv (0.15.3 master)。**DbConfig.Instance 跨 class flake 修复** (`[Collection("DbConfigGlobal")]` 加到 6 个 class) + UAT BDD 指向新 baseline → **521/521 全 Pass / 0 Skip / 0 Fail / 35s wall / 73.02s cumulative**。历史首次 100% 完全清空 skip 列表。

### 2026-05-18 round-1 Windows (limeng)

Claude Sonnet 全自动 UIA 驱动跑 21 WPF UI 用例 + 引用 5 cloud-covered = 26/26 覆盖。**6 ✅ PASS** (A1/A3/A4/A7/B2/B4-5) + **10 ⚠️ Partial/N/A** (A6/B1部分/B3/B6-9/E1-3) + **3 ❌ FAIL** (A2/A5/B1). 发现 **2 个真实 bug**: (1) `ApplicationService.UpdateService` IsDuplicate 未排除自身 → 同名 update 误判; (2) `Application`/`ApplicationEx` 缺 `ToString()` override → ComboBox 显示类名 block UC-A5/B1 选择. 另 2 处 typo (`Desciption`/`Eecute MT`) + 多处 runbook ↔ UI 不对齐 (System MT 新 UI 不分两步无图表, SystemMt ↔ Anomaly/Trends/Coverage 接线 gap). WPF 冷启动 2.68s ✅, heat_equation Run 3.4s ✅. Round-2 待 fix bug 后复跑。

### 2026-05-19 round-2 Windows (limeng)

3 个 round-1 Major bug 全部 WPF UI 端到端验证通过：UC-A2 description-only Update + rename-to-unique 都返回"修改记录 成功！"；UC-A5 ApplicationEx ComboBox 显示业务 Name（MR Mgmt + Discovery 双 sibling check 通过）；UC-B7 factor=0.5 失败 run → Anomaly 行自动创建 (Severity=minor, Status=new, Category=single-point)。加跑 UC-B8 多选 Analyze commonality + UC-B9 Replay anomaly 同步通过。

UC-B7 初次跑命中 **cross-track bug**: 生产 `LiteDbSystemMtResultRepository.SaveAsync` 返回 BSON ObjectId 字符串（24 hex），PR #75 的 `AnomalyService.RecordAnomalyAsync` 要 Guid 字符串 —— PR #75 单元测试用 stub 返回 `Guid.NewGuid().ToString()` 屏蔽了此不兼容。issue #76 一行诊断 + 同 PR #77 做结构性 fix：`SystemMtResultRecord.Id` 从 `string` 改 `Guid`（与 v2 其他 entity 一致）+ LiteDB `autoId: true` 自动生成 + ObjectId→Guid 一次性 idempotent migration + 3 个回归测试。

**release-v2.1.0 决策矩阵满足**: round-1 CONDITIONAL PASS → fix PR #71/#72/#75 merged → round-2 cross-track bug PR #77 inline 修 + 全部 5 UC ALL-PASS。可发版，待 tag。

### _待写_

### 2026-05-24 baseline-solidification
本轮不是新的 UAT 轮次，而是对当前工作树做基线固化核查。最初完整 `dotnet test MetBench_SystemMT.Tests --no-restore` 暴露 1 个回归：`OpenMocOutputAdapterTests.ParseAsync_returns_keff_iterations_and_converged`，根因是 Python `Path.resolve()` 把 `/var/...` 展开成 `/private/var/...`。修复 `SUT/openmoc/openmoc_output_adapter.py` 后，完整 `dotnet test MetBench_SystemMT.Tests --no-restore --logger "trx;LogFileName=baseline-2026-05-24-current.trx"` 返回 **961 pass / 0 fail / 8 skip / 969 total**；测试工件已固化为 [`round-3-limeng-2026-05-24/baseline-2026-05-24-current.trx`](/Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/docs/uat/reports/round-3-limeng-2026-05-24/baseline-2026-05-24-current.trx)。因此当前工作树已恢复为绿色，但在提交前，仓库文档仍保留 `763e067` 作为“最新已提交可审计精确绿基线”，并把 2026-05-24 结果记为“当前工作树绿结果”。

---

## Release 决策矩阵

依据本表 + 评价表 acceptance-rubric 的"Release 通过准则"：

| 条件 | 决策 |
|------|------|
| 连续 2 轮 PASS + 0 Blocker | **可发版** |
| 1 轮 CONDITIONAL PASS + 全部 Major 已有 fix PR | 待 fix merge 后再验 1 轮 |
| 任意轮 FAIL | 修复回归至少 1 轮 ALL-PASS 才能发版 |

## Tag 约定

每次发版 / 重大里程碑 → 给仓库打 tag：

```
uat-v2.1.0-round-1     # 第 1 轮 UAT 跑通的 commit
uat-v2.1.0-round-2
release-v2.1.0         # 发版基线（dashboard 决策"可发版"后打）
```

```bash
# 下发人在 round merge 后立刻打
git tag -a uat-v2.1.0-round-N -m "UAT round N: <PASS/CONDITIONAL/FAIL>"
git push origin uat-v2.1.0-round-N
```
