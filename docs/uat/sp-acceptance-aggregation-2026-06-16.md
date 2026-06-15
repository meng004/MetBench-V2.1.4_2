# MetBench 大目标验收聚合总报告（SP1–SP5）

> **日期**: 2026-06-16
> **大目标**: 为已导入的全部 SUT / MR / 基础算例 / 变异体，建立**真实可异步运行**的环境，并让 **xUnit + UAT + WPF UI 三层验收**全部通过。
> **拆分**: 5 个子项目 SP1–SP5（spec: `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-acceptance-design.md`）。
> **本报告角色**: SP5 —— 聚合 SP1–SP4 的真实结果，给出总体验收判定。不新增运行；所有数字引自各子项目证据。

---

## 1. 子项目结果一览

| 子项目 | 范围 | PR | 结果 | 证据 |
|---|---|---|---|---|
| **SP1** | 全运行时真实异步跑通（0 skip） | #364 | ✅ 容器内全套 `MetBench_SystemMT.Tests`：**1895 passed / 0 failed / 6 skipped**（运行时类 0 skip；6 skip 为范围外：3 MCP 实时 server、3 外部源 np.trapz BLOCKED）。3 个新 `LauncherAsyncJobRuntimeTests` 异步作业路径真跑通。 | `…/2026-06-13-sp1-all-real-runtime-evidence/` |
| **SP2** | 变异体 T6 真实跑 + kill 矩阵 | #365 | ✅ 对真实 openmoc/openmc 跑全 **48 变异**：347 ran cells / 73 detected。硬性质：**Mut00 恒等 0/29 零误杀**；41 semantic-intent 全有矩阵。跨求解器 Cohen's κ=1.000（匹配对）。如实记 3 类 T6 发现（8 检测盲区、2 疑似过敏、2 目录漂移）。 | `…/2026-06-13-sp2-mutation-real-run-evidence/` |
| **SP3a** | UAT trx 支撑类（22 项） | #366 | ✅ **22/22** trx 支撑类 UAT 用例真实验收全 ✅（host trx + 容器 C11 + CI perf 基线）。 | `…/2026-06-15-sp3a-uat-trx-evidence/` |
| **SP3b** | UAT WPF-UI 类（24 项） | #367 | 部分：**9 ✅ / 14 ⚠️ / 0 ❌ / 1 未跑**；24/24 页导航渲染通过。核心流程真验（A1 创建、A7 元模式、System-MT B2-B5 端到端 PASS、B7 异常、B1 发现 14 候选、E2 覆盖）。**6 项发现：5 修(代码)+1 重归类**。 | `…/2026-06-15-sp3b-uat-wpf-ui-evidence/` |
| **SP4** | 每 SUT/MR WPF 异步页 UI 证据 | #368 | **33 job-Succeeded / 5 job-Failed**（38 MR）。异步页对 33 个 MR 端到端跑到终态（UI 证据）。语义：作业 Succeeded≠MR 通过。 | `…/2026-06-16-sp4-async-ui-evidence/` |

---

## 2. 聚合验收矩阵（按验收层）

| 层 | 覆盖 | 通过 | 备注 |
|---|---|---|---|
| **xUnit（自动化）** | 全套 1901 | 1895 passed / 0 failed | 运行时类（scipy/openmoc/openmc/跨程序/异步作业）容器内 0 skip 0 fail（SP1）。6 skip 范围外。 |
| **T6 变异** | 48 变异 × MR suite | 73 detected / 347 cells | 硬性质通过（零误杀 + 全矩阵 + κ=1.0）。检测盲区如实记（SP2）。 |
| **UAT trx 支撑类** | 22 | 22 ✅ | A8/C1-5/C10-11/D1-2/E6-7/F1-5/G1-2/G4-5（SP3a）。 |
| **UAT WPF-UI 类** | 24 | 9 ✅ / 14 ⚠️ / 1 未跑 | 0 ❌；导航/渲染层 24/24 通过；数据依赖页需上游运行（SP3b）。 |
| **WPF 异步页逐 MR** | 38 | 33 job-Succeeded | 3 openmc 作业 Succeeded 但 MR 违例=异常；openmoc×3 容器侧；2 SUT 异步路径 JSON 解析发现（SP4）。 |

---

## 3. 总体判定

**大目标实质达成（substantially achieved），带已记录的部分项与发现。**

- **"真实可异步运行环境"**：✅ 达成。SP1 证明全运行时家族在容器内真实异步跑通（0 skip 0 fail），异步作业路径（service→worker→pipeline→launcher）端到端验证。
- **"全部通过验收"**：**分层判定**——
  - xUnit 层：✅ 全过（0 fail）。
  - T6 变异层：✅ 硬性质过；检测盲区是 T6 科研发现（指向最小 MR 完备子集），非环境缺陷。
  - UAT trx 层：✅ 22/22。
  - UAT WPF-UI 层：**部分**（9 ✅ / 14 ⚠️ / 1 未跑 / 0 ❌）——核心流程真验，⚠️ 多为数据依赖页需上游运行（非产品不可用）+ 自动化夹具局限（无 AutomationId 网格选择）。
  - WPF 异步页逐 MR：33/38 job-Succeeded（UI 证据）；5 Failed 已归因（3 容器侧、2 异步路径发现、3 openmc 异常）。

**结论**：环境与自动化层（SP1/SP2/SP3a）= 干净通过；UI 层（SP3b/SP4）= 核心通过 + 诚实部分项 + 发现已记录并多数修复。无 🔴 Blocker 级 ❌ 悬留。

---

## 4. 合并发现清单（SP3b + SP4，去重）

**已修（SP3b PR #367 内，重建+重验）：**
1. A3 删除确认框文案误写「是否修改」→「是否删除」。
2. B6 按钮拼写「Eecute MT」→「Execute MT」。
3. E5 Dashboard 无导航入口 → 加 `Nav_Dashboard`（页面内容仍 stub，见剩余项）。
4. WPF System-MT 需 python 在 PATH → 加 `METBENCH_SYSTEM_PYTHON` env override。
5. Discovery `noether_candidates.py` 未部署到 bin → csproj 部署（修复后发现产 14 候选）。

**待跟进（follow-up）：**
6. ~~SP4 发现：`csv-roundtrip-identity`、`projectile-scale-v0` 经 WPF 异步页 `System.Text.Json` 解析 SUT 输出失败（非单-JSON 输出）~~ **→ 已修**（PR fix-async-json-parse）：根因是 `SystemMtExecutionRecorder.BuildSampleTraces` 无条件 `JsonDocument.Parse` 非-JSON sample（recorder 写证据时，非 launcher 核心）；改为 best-effort try/catch 降级空 traces。单元回归 + 端到端复跑两 MR 现均 Succeeded（SP4 33/38→35/38）。详见 `docs/superpowers/specs/2026-06-16-finding6-async-json-parse-fix/fix-summary.md`。
7. SP4：3 openmc MR 作业 Succeeded 但 **MR 断言失败=异常**（与 T5 已知 OpenMOC×OpenMC 跨程序分歧一致）；host-openmc 结果可信度 vs 容器 openmc 待裁。
8. SP3b：Dashboard 页内容仍是占位 stub（≥4 cards 未实现）；~~Domain/MR 删除确认有同款「是否修改」latent typo（A3 同类，报告范围外）~~ **→ 已核实并修复**（复核三个管理 VM：删除确认 typo 实际只在 Domain，由 PR #377 改为「是否删除该记录?」、#378 还原被 PowerShell 重写误剥的 UTF-8 BOM；MR MRManagementViewModel L594 删除确认本就是「是否删除」无 typo；Application 已于 #367 修复 — A3 类 typo 现已全清零）；C8 AutoDetectMR 未跑（已查可行性：上传/确认框/进度条 UI 流程 + 检测脚本 MetBench_Python/AutoMRDetector/auto_mr_detector-3.py 均在仓库；但真跑通依赖**遗留方法级 AutoMR python 栈**——AutoMRAlgorithm 经 FindFirstPythonInPath 扫 PATH 取含 "Python" 的目录，且检测流水线需 sympy/numpy，当前可用运行时缺 sympy；此为与 SP1-SP5 system-MT 运行时相互独立的遗留环境项，deferred）；若干数据依赖 ⚠️ 页需上游运行（异常/发现/变异 campaign 已具工具支持）。
9. T6（SP2）：8 个 semantic 变异无 MR 检出（检测盲区，指向最小 MR 完备子集搜索——已记录的 deferred T6 工作）。
10. openmoc/openmc 的 WPF 异步页**逐 MR UI 证据容器内不可行**（WPF GUI ∉ 容器）；其运行时正确性已由 SP1 容器内 xUnit 覆盖。

---

## 5. 剩余项与建议

- UI 层 ⚠️/未跑项与 follow-up #6-#8 建议各开 scoped 修复/补跑 PR（工具 `tools/uia-acceptance` + `sp4_run_all.ps1` 已支持复跑）。
- T6 检测盲区（#9）进入"最小 MR 完备子集"研究线（已 deferred 跟踪）。
- 外部源 np.trapz / NumPy 2.4.6 不兼容（SP1 6-skip 之一）仍 BLOCKED，需外部源修复。

**SP1–SP5 子项目链至此闭环**：环境真实异步可运行 + 三层验收已聚合判定，结论与全部证据可逐项追溯至各子项目 evidence 目录。
