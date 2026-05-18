# MetBench 项目结构

> **更新时间**: 2026-05-17（commit `9b0a53b6` baseline-2026-05-17 之后）
> **目标读者**: 新加入仓库的开发者 / 验收员 / reviewer。文档全息呈现仓库当前结构 + SUT 测试覆盖 + MetBench 框架测试覆盖。
> **更详细的设计**: [`AGENTS.md`](../AGENTS.md)（roadmap）· [`CLAUDE.md`](../CLAUDE.md)（agent 注意事项）· [`docs/design/`](design/)（架构）

---

## §1 .NET 项目布局（7 个 csproj）

| 项目 | Target | 跑哪里 | 用途 |
|---|---|---|---|
| **`MetBench_BLL.Core/`** | `net8.0` | Linux + Windows + CI | 跨平台 BLL：系统级 MT runner / adapters / persistence contracts / 7 大 v2 namespace（Launcher / Persistence / Reporting / SystemMT.Anomaly / Discovery / Mutation / Coverage / Trend）|
| **`MetBench_BLL/`** | `net8.0` | Linux + Windows + CI | WPF 侧 BLL：v1 方法级 MT 主流程 + Word/Excel/PDF 报表生成器 + LiveCharts 数据 service（无 WPF 依赖） |
| **`MetBench_Domain/`** | `net8.0` | Anywhere | 域实体：v1 方法级 + v2 四级 MR 层级（MetaPattern → MRSchema → MRBinding → MRInstance → Execution） |
| **`MetBench_IDAL/`** | `net8.0` | Anywhere | DAL 接口合约 |
| **`MetBench_DAL/`** | `net8.0` | Anywhere | LiteDB 持久化：v1 run-result + v2 24-collection schema |
| **`MetBench_Client/`** | `net8.0-windows7.0` | **Windows only** | WPF UI 应用，入口点；引 `Wpf.Ui` + `CommunityToolkit.Mvvm` + LiveCharts WPF |
| **`MetBench_SystemMT.Tests/`** | `net8.0` | Anywhere | xUnit + Reqnroll：全部测试（487 [Fact] + 30+ BDD scenarios + 21 UAT BDD + 47 markdown UAT 用例） |

**硬规则**（cloud 与 Windows 端协作）：

- Cloud agents 可改 `MetBench_BLL.Core/` / `MetBench_DAL/` / `MetBench_BLL/` / `MetBench_SystemMT.Tests/` / docs（**全部可在 Linux 编译**）
- Cloud agents **不可改** `MetBench_Client/*.xaml*` 没有显式许可（Linux 不能 build WPF SDK）
- Windows agents **不可改** `MetBench_BLL.Core/SystemMT/*` public types 没先提 cloud-side 设计（CI 会卡）

---

## §2 SUT 清单（4 个）

| SUT | 目录 | 域 | 算法 | Runner | Adapter 数 | Sample | scg.json | 接入 PR |
|---|---|---|---|---|---|---|---|---|
| **OpenMOC** | `SUT/openmoc/` | Neutron transport | Method of Characteristics (deterministic) | `openmoc_runner.py` | 11 input + 1 output | 3 | ✅ | Stage 3 |
| **OpenMC** | `SUT/openmc/` | Neutron transport | Monte Carlo (stochastic) | `openmc_runner.py` | 17 input + 1 output | 1 | ✅ | #57 (W12 F13) |
| **Heat Equation** | `SUT/heat_equation/` | PDE | 1D 有限差分 | `heat_equation.py` | 1 input + 1 output | 1 | ❌ | Stage 4 |
| **Projectile** | `SUT/projectile/` | Ballistics | Closed-form physics | `projectile.py` | 0 + 1 output | 0 | ❌ | Stage 4 demo |

SUT 接入到框架的 hook：
- Python runner（`<sut>_runner.py`）—— stdin/CLI args 入参，stdout JSON 出参
- input adapter（一或多个 `<sut>_input_adapter*.py`）—— 实现 MR transformation 对入参文件的具体改写
- output adapter（`<sut>_output_adapter.py`）—— 把 SUT 自然输出转成统一 metrics JSON
- 可选 `scg.json`—— SCG-Heuristic discoverer 用的因果图（含 nodes + edges）

---

## §3 SUT 测试矩阵（unit + 系统级 BDD）

| SUT | 单元 / contract test ([Fact] 数) | 系统级 BDD .feature | BDD scenario instances | Launcher 注册 MR id |
|---|---|---|---|---|
| **OpenMOC** | `OpenMocInputAdapterTests` (4) + `OpenMocOutputAdapterTests` (2) + `OpenMocSigmaAInputAdapterTests` (5) + `OpenMocRunnerSmokeTests` (1) + `OpenMocSampleCaseTests` (1) = **13** | `OpenMocPinCellNuSigmaF.feature` · `OpenMocPinCellSigmaA.feature` · `CrossProgramNeutronTransportMrs.feature` (2 outline) | 4（独占 2 + 跨程序 2） | `openmoc-pincell-nu-sigma-f` · `openmoc-pincell-sigma-a` |
| **OpenMC** | `OpenMcInputAdapterTests` (5) + `OpenMcOutputAdapterTests` (5) + `OpenMcRunnerSmokeTests` (1) = **11** | (共用 `CrossProgramNeutronTransportMrs.feature`) | 2（跨程序 examples） | `openmc-pincell-nu-sigma-f` · `openmc-pincell-sigma-a` |
| **Heat Equation** | `HeatEquationInputAdapterTests` (2) + `HeatEquationOutputAdapterTests` (4) = **6** | `HeatEquationAmplitude.feature` | 1 | `heat-equation-amplitude` |
| **Projectile** | (依靠 `CliProgramRunnerTests` 通用覆盖) | `ProjectileRange.feature` | 1 | — (仅 BDD，未 Launcher 注册) |
| **跨 SUT 通用** | `MrTransformationTests` · `InputGeneratorTests` · `GreaterThanAssertionTests` · `LessThanAssertionTests` | `SystemLevelCliMt.feature` · `SystemLevelGeneratedFollowup.feature` | 2 | — |

**SUT 系统级 MR 总数**：
- Launcher 注册：**5** MR-on-SUT 绑定（4 个 neutron + 1 个 heat-eq）
- BDD 描述：**10** scenario instances（4 cross-program + 1 heat-eq + 2 openmoc-only + 1 projectile + 2 system-level demo）

---

## §4 MetBench 框架测试矩阵

按 `MetBench_BLL.Core/` 七大 namespace 分组 + DAL + Pipeline + Pagination：

| 模块 | namespace | 测试目录 | test class 数 | [Fact] 数 |
|---|---|---|---|---|
| **SystemMT** (Launcher / Runner / Adapter) | `MetBench_BLL.SystemMT.*` | `SystemMT/` | 18 | 55 |
| **Discovery** (4 类 Discoverer + Validator + Seed + Pairing) | `MetBench_BLL.Discovery.*` | `V2Discovery/` | 14 | 93 |
| **Anomaly** (List / Commonality / Status / KnownBug) | `MetBench_BLL.SystemMT.Anomaly` | `V2Anomaly/` | 1 | 13 |
| **Coverage** (4 维) | `MetBench_BLL.Coverage` | `V2Coverage/` | 1 | 18 |
| **Trend** (Weekly / WoW / Burst) | `MetBench_BLL.Trend` | `V2Trend/` | 2 | 13 |
| **Reporting** (HTML + 5-scope service) | `MetBench_BLL.Reporting` + `MetBench_BLL.SystemMT.Reporting` | `Reporting/` + `V2Reporting/` | 2 | 17 |
| **Mutation** (campaign × matrix) | `MetBench_BLL.Mutation` | `V2Mutation/` | 1 | 8 |
| **Pipeline** (v2 orchestration + Replay + AssertionEvaluator) | `MetBench_BLL.SystemMT.Pipeline.*` | `V2Pipeline/` | 6 | 48 |
| **RCaseRepro** (论文核心 - F9) | `MetBench_BLL.SystemMT.RCase` | `V2RCaseRepro/` | 1 | 11 |
| **Persistence (LiteDB)** | `MetBench_BLL.SystemMT.Persistence` + `MetBench_DAL` | `SystemMT/Persistence/` + `V2Schema/` | 2 | 7 + 22 |
| **Pagination** (Keyset) | `MetBench_BLL.Paging` + `MetBench_DAL.V2.*` | `V2Pagination/` | 5 | 54 |
| **Schema / Entity** (round-trip + soft-delete + migration) | `MetBench_Domain.V2` | `V2Schema/` | 5 | 9 |
| **Transformations** (v2 IMRTransformation) | `MetBench_BLL.Discovery.Transformations` | `V2Transformations/` | 3 | 20 |
| **ColdStart** | — | `ColdStart/` | 1 | 1 |

**测试总数对照**（baseline-2026-05-17）：
- 全套 `dotnet test`：**521 Pass / 0 Skip / 0 Fail / 35s wall / 73.02s cumulative**
- UAT BDD filter（`FullyQualifiedName~UAT`）：**48 Pass / 0 Skip**
- BDD smoke（Features filter）：**30 Pass / 1 Skip**

---

## §5 UAT 验收用例（47 个 markdown + 21 个 BDD）

UAT 是测试 **MetBench 框架本身**功能，跟 SUT MR 测试是两个层面：

| 类别 | 用例数 | 平台 | BDD 化（21/47） | markdown 三段式（47/47） |
|---|---|---|---|---|
| **A. 管理 CRUD** | 8 | Windows UI | 0 | ✅ |
| **B. MR 蜕变测试主流程** | 9 | Windows UI + Linux | 0 | ✅ |
| **C. MR 发现 & 验证** | 11 | Linux + Windows | 11 (BDD wrapper) | ✅ |
| **D. R-Case 自动复现** | 2 | Linux | 0 | ✅ |
| **E. 可视化 & 报表** | 7 | Windows UI | 0 | ✅ |
| **F. 持久化 & schema** | 5 | Linux | 5 (BDD wrapper) | ✅ |
| **G. 运营 & 性能** | 5 | Linux | 5 (BDD wrapper) | ✅ |
| **合计** | **47** | | **21** | **47** |

**UAT 文档树**：
- 📋 [`docs/uat/acceptance-rubric.md`](uat/acceptance-rubric.md)：47 用例评分表（验收员逐行打分）
- 📘 [`docs/uat/test-procedures.md`](uat/test-procedures.md)：47 用例三段式手册（初始条件 / 操作步骤 / 断言）
- 🧪 [`MetBench_SystemMT.Tests/Features/Uat/UC-*.feature`](../MetBench_SystemMT.Tests/Features/Uat/)：21 个 BDD wrapper（每用例 1 scenario，反射验证 ≥ N facts + trx baseline 检查）
- 🪟 [`docs/uat/runbooks/windows-uat-round-1.md`](uat/runbooks/windows-uat-round-1.md)：Windows 端 **21 个 WPF UI 用例**（A1-A7 + B1-B9 + E1-E5）1 轮操作手册；其余 5 个 CLI 用例（A8 / D1 / D2 / E6 / E7）已由 cloud baseline 覆盖
- 📊 [`docs/uat/reports/baseline-2026-05-17/`](uat/reports/baseline-2026-05-17/)：当前基线（521/521，可作 release-v2.1.0 reference）
- 🗓 [`docs/uat/reports/dashboard.md`](uat/reports/dashboard.md)：历史轮次趋势

---

## §6 tools/ 脚本（17 个 + 子目录）

| 脚本 | 用途 |
|---|---|
| `ci_perf_baseline.py` | CI 性能门：120s 总预算 + 2000ms 单测 warn |
| `check_openmoc_adjoint.sh` | F11 m_adj 路径 A 被动监控（GitHub Actions 每月 1 号 03:17 UTC 跑） |
| `mutation_study.py` + `mutations.py` | 28 手工 mutation 候选 + 评分编排 |
| `cross_program_mr.py` | 跨程序 MR 对比 |
| `feature_to_db.py` / `db_to_feature.py` | feature ↔ DB 同步迁移 |
| `mr_parameter_sweep.py` | MR 参数扫参 |
| `noether_*.py` (4 个) | 诺特候选 / LLM 过滤 / 对抗样本 |
| `real_bugs_live_repro.py` | 真实 bug 复现编排 |
| `render_dashboard.py` / `render_figures.py` | 实验图表渲染 |
| `build_paper_package.py` | 论文 reproducible tarball |
| `ScaffoldMr/` | MR 脚手架生成器 |
| `smokeshot/` | smoke test 采样工具 |

---

## §7 CI workflows

| Workflow | trigger | 内容 |
|---|---|---|
| **`dotnet-test.yml`** | push main / PR | `ubuntu-24.04` + .NET 8 + 521 facts + perf baseline gate（120s 预算） |
| **`f11-monthly-monitor.yml`** | cron `17 3 1 * *` + manual dispatch | F11 m_adj 路径 A — 拉 `mit-crpg/OpenMOC` 近 90 天 commit，grep "adjoint"；命中自动开 issue（labels `f11-monitor` + `rfc-followup`） |

WPF 的 `MetBench_Client/` 因 SDK targets 限制 **不在 Linux CI 编译**；视觉 + 运行时验证由 Windows VM 端开发者负责。

---

## §8 关键命名约定（v2.1 已统一）

| 概念 | 现在用 | 已废弃 |
|---|---|---|
| MR 在 launcher boundary 的 UI 投影 | `MrSummary` | ~~`ScenarioDescriptor`~~（PR #58 改名） |
| 单次 MR 跑动结果 | `MrRunResult` | ~~`ScenarioRunResult`~~（PR #58） |
| Launcher 接口 | `ISystemMtMrLauncher` | ~~`ISystemMtScenarioLauncher`~~（PR #58） |
| Launcher 实现 | `SystemMtMrLauncher` | ~~`SystemMtScenarioLauncher`~~（PR #58） |
| Batch 单元 | `BatchMrRunRequest` | ~~`BatchScenarioRequest`~~（PR #58） |
| MR 标识符 | `MrId` | ~~`ScenarioId`~~（PR #58） |
| Persistence 字段 | `MrName` | ~~`ScenarioName`~~（PR #62 schema migration） |

**核心理由**：消除与 BDD Gherkin `Scenario` 撞名的混淆。Launcher 与 persistence 层的 "scenario" 词根全部消除，统一 MR 术语。LiteDB 自动 schema migration 兼容老 `.Litedb` 文件（PR #62）。

---

## §9 接入新 SUT 的 checklist

当要加第 5 个 SUT（如 SU2 / FEniCS / 第 4 个 neutron solver），按 [F13 RFC](superpowers/plans/2026-05-17-f13-third-sut-rfc.md) §6 走：

- [ ] `.claude/web-setup.sh` 加该 SUT 的 venv / binary 安装段
- [ ] `LauncherOptions.<sut>Python` 字段 + DI 默认值
- [ ] `MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.BuildMrCatalog()` 加 yield `MrBlueprint`
- [ ] `SUT/<sut>/` 目录含 runner + adapters + sample + 可选 scg.json
- [ ] `<sut>RunnerSmokeTests.cs` 写 ≥ 1 个 [SkippableFact]
- [ ] 现有跨程序 feature 中 `Examples:` 加该 SUT 行（如适用）
- [ ] `docs/uat/acceptance-rubric.md` Part C 加一行 UC-C12+
- [ ] `docs/uat/test-procedures.md` 加对应三段式
- [ ] `MetBench_SystemMT.Tests/Features/Uat/UC-Cxx-<sut>.feature` 加 BDD wrapper
- [ ] CI 跑过 → baseline 刷新
- [ ] **本文件** §2 §3 §5 表格相应行追加

---

## §10 v2.1 → v2.2 路线参考

| 项 | 状态 | 计划 |
|---|---|---|
| **m_adj** (adjoint MR 族) | 🟢 路径 A 被动监控在线 | 等 OpenMOC 上游 adjoint export → 评估 patch → MetaPattern Status `out-of-scope` → `active` |
| **第 5 SUT** (SU2 / FEniCS) | 🟡 未启动 | 等 v2.1 发版 + reviewer 反馈决定要否补跨域 SUT |
| **WPF UI 验收** | 🟡 round-1 跑动中 | Windows 端按 `windows-uat-round-1.md` 跑通 → dashboard `PASS` → tag `release-v2.1.0` |
| **`HandyControl` → `Microsoft.Xaml.Behaviors.Wpf`** | 🟡 旧代码兼容 | v2.2+ refactor 跟 UI 整改一起做 |
| **6 个 `Service` 拼写修正废弃别名** | 🟢 v2.2 删除 | `[Obsolete]` 已标 1 版 |

---

最后更新：commit `9b0a53b6`，基线 `baseline-2026-05-17`。下次主要结构变更（接新 SUT / 删旧 namespace / 改命名约定）后更新本文件。
