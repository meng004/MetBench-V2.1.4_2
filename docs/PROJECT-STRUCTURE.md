# MetBench 项目结构

> **结构快照基线**: 2026-05-26（代码测试基线由 `docs/status/current.md` §2 实时维护；T3 PDE-class 覆盖更新至 PR #140）
> **目标读者**: 新加入仓库的开发者 / 验收员 / reviewer。文档全息呈现仓库当前结构 + SUT 测试覆盖 + MetBench 框架测试覆盖。
> **更详细的设计**: [`AGENTS.md`](../AGENTS.md)（roadmap）· [`CLAUDE.md`](../CLAUDE.md)（agent 注意事项）· [`docs/design/`](design/)（架构）
> **当前状态账本**: [`docs/status/current.md`](status/current.md)。本文件只投影结构与测试矩阵，不重新定义当前主线状态。

---

## §1 .NET 项目布局（7 个 csproj）

| 项目 | Target | 跑哪里 | 用途 |
|---|---|---|---|
| **`MetBench_BLL.Core/`** | `net8.0` | Linux + Windows + CI | 跨平台 BLL：System-MT pipeline / provider-backed launcher / adapters / persistence contracts / reporting / anomaly / discovery / mutation / coverage。2026-05-25 当前主线已切到 `IMrCatalogProvider` + `ManifestMrCatalogProvider`，launcher 生产路径已不再保留 `HardcodedMrCatalogProvider` fallback；Typed Semantic Catalog 正式代码面位于 `SystemMT/Catalog/Typed/`（原 `SystemMT/V12Catalog/`，PR #115 重命名永久化）；pipeline 断言阶段经 PR #118 已切到 `PredicateDispatcher`，W1 `IMrAssertion` 路径在 PR #119 已从生产侧删除。 |
| **`MetBench_BLL/`** | `net8.0` | Linux + Windows + CI | WPF 侧 BLL：v1 方法级 MT 主流程 + Word/Excel/PDF 报表生成器 + LiveCharts 数据 service（无 WPF 依赖） |
| **`MetBench_Domain/`** | `net8.0` | Anywhere | 域实体：v1 方法级 + v2 四级 MR 层级（MetaPattern → MRSchema → MRBinding → MRInstance → Execution） |
| **`MetBench_IDAL/`** | `net8.0` | Anywhere | DAL 接口合约 |
| **`MetBench_DAL/`** | `net8.0` | Anywhere | LiteDB 持久化：v1 run-result + v2 24-collection schema |
| **`MetBench_Client/`** | `net8.0-windows7.0` | **Windows only** | WPF UI 应用，入口点；引 `Wpf.Ui` + `CommunityToolkit.Mvvm` + LiveCharts WPF |
| **`MetBench_SystemMT.Tests/`** | `net8.0` | Anywhere | xUnit + Reqnroll：跨平台事实源测试。当前共享精确代码绿基线见下文：`e839214` = **1043 pass / 0 fail / 0 skip**。 |

**硬规则**（cloud 与 Windows 端协作）：

- Cloud agents 可改 `MetBench_BLL.Core/` / `MetBench_DAL/` / `MetBench_BLL/` / `MetBench_SystemMT.Tests/` / docs（**全部可在 Linux 编译**）
- Cloud agents **不可改** `MetBench_Client/*.xaml*` 没有显式许可（Linux 不能 build WPF SDK）
- Windows agents **不可改** `MetBench_BLL.Core/SystemMT/*` public types 没先提 cloud-side 设计（CI 会卡）

---

## §2 SUT 清单（当前 launcher catalog：15 个）

| SUT | 目录 | 域 | 算法 / 程序类型 | Runner | Sample / catalog | 接入 PR |
|---|---|---|---|---|---|---|
| **OpenMOC** | `SUT/openmoc/` | Neutron transport | Method of Characteristics | `openmoc_runner.py` | `catalog.json` + sample | Stage 3 / Stage 8 |
| **OpenMC** | `SUT/openmc/` | Neutron transport | Monte Carlo | `openmc_runner.py` | `catalog.json` + sample | #57 / Stage 8 |
| **Heat Equation** | `SUT/heat_equation/` | PDE (parabolic) | 1D finite difference | `heat_equation.py` | `catalog.json` + sample | Stage 4 / Stage 8 |
| **Projectile** | `SUT/projectile/` | Ballistics | Closed-form physics | `projectile.py` | `catalog.json` + sample | G-09 |
| **Decay Chain** | `SUT/decay_chain/` | ODE | Bateman chain | `decay_chain_runner.py` | `catalog.json` + sample | Stage 8 P1 |
| **Damped Oscillator** | `SUT/damped_oscillator/` | ODE | Linear ODE | `damped_oscillator_runner.py` | `catalog.json` + sample | Stage 8 P1 |
| **Lotka-Volterra** | `SUT/lotka_volterra/` | ODE | Predator-prey ODE | `lotka_volterra_runner.py` | `catalog.json` + sample | Stage 8 P1 |
| **Subchannel 1D** | `SUT/subchannel_1d/` | PDE / NS surrogate | 1D subchannel | `subchannel_1d_runner.py` | `catalog.json` + sample | Stage 8 P3 |
| **Diffusion 1D** | `SUT/diffusion_1d/` | PDE (parabolic) | 1D diffusion FD | `diffusion_1d_runner.py` | `catalog.json` + sample | Stage 8 P4 |
| **Poisson 1D** | `SUT/poisson_1d/` | PDE (elliptic) | Pure-stdlib Thomas tridiagonal | `poisson_1d_runner.py` | `catalog.json` + sample | PR #134 |
| **Advection 1D** | `SUT/advection_1d/` | PDE (first-order linear hyperbolic) | Pure-stdlib first-order upwind FD + periodic BC | `advection_1d_runner.py` | `catalog.json` + sample | PR #136 |
| **Wave 1D** | `SUT/wave_1d/` | PDE (second-order linear hyperbolic) | Pure-stdlib second-order leapfrog FD + Dirichlet BC | `wave_1d_runner.py` | `catalog.json` + sample | PR #138 |
| **Burgers 1D** | `SUT/burgers_1d/` | PDE (nonlinear hyperbolic) | Pure-stdlib conservative Lax-Friedrichs flux differencing + periodic BC | `burgers_1d_runner.py` | `catalog.json` + sample | PR #140 |
| **SciPy IVP Lotka-Volterra** | `SUT/scipy_ivp_lotka_volterra/` | ODE (Lotka-Volterra, predator-prey nonlinear) | **External library**: SciPy `solve_ivp` adaptive RK45 (rtol=1e-9 / atol=1e-12) | `scipy_ivp_lotka_volterra.py` | `catalog.json` + sample | T3C-IVP |
| **SciPy BVP Poisson 1D** | `SUT/scipy_bvp_poisson_1d/` | PDE (elliptic Poisson `-u''=f`, Dirichlet BC) | **External library**: SciPy `solve_bvp` adaptive BVP (tol=1e-9) | `scipy_bvp_poisson_1d.py` | `catalog.json` + sample | T3C-BVP |

SUT 接入到框架的 hook：
- Python runner（`<sut>_runner.py`）—— stdin/CLI args 入参，stdout JSON 出参
- input adapter（一或多个 `<sut>_input_adapter*.py`）—— 实现 MR transformation 对入参文件的具体改写
- output adapter（`<sut>_output_adapter.py`）—— 把 SUT 自然输出转成统一 metrics JSON
- `catalog.json`—— 2026-05-25 当前 manifest-backed catalog 的事实源；WPF 默认通过 `ManifestMrCatalogProvider` 读取
- 可选 `scg.json`—— SCG-Heuristic discoverer 用的因果图（含 nodes + edges）

---

## §3 SUT 测试矩阵（unit + 系统级 BDD）

| SUT | 单元 / contract test ([Fact] 数) | 系统级 BDD .feature | BDD scenario instances | Launcher 注册 MR id |
|---|---|---|---|---|
| **OpenMOC** | `OpenMocInputAdapterTests` (4) + `OpenMocOutputAdapterTests` (2) + `OpenMocSigmaAInputAdapterTests` (5) + `OpenMocRunnerSmokeTests` (1) + `OpenMocSampleCaseTests` (1) = **13** | `OpenMocPinCellNuSigmaF.feature` · `OpenMocPinCellSigmaA.feature` · `CrossProgramNeutronTransportMrs.feature` (2 outline) | 4（独占 2 + 跨程序 2） | `openmoc-pincell-nu-sigma-f` · `openmoc-pincell-sigma-a` |
| **OpenMC** | `OpenMcInputAdapterTests` (5) + `OpenMcOutputAdapterTests` (5) + `OpenMcRunnerSmokeTests` (1) = **11** | (共用 `CrossProgramNeutronTransportMrs.feature`) | 2（跨程序 examples） | `openmc-pincell-nu-sigma-f` · `openmc-pincell-sigma-a` |
| **Heat Equation** | `HeatEquationInputAdapterTests` (2) + `HeatEquationOutputAdapterTests` (4) = **6** | `HeatEquationAmplitude.feature` | 1 | `heat-equation-amplitude` |
| **Projectile** | (依靠 `CliProgramRunnerTests` 通用覆盖) | `ProjectileRange.feature` | 1 | — (仅 BDD，未 Launcher 注册) |
| **Poisson 1D** | `LauncherEndToEndPoissonTests`（端到端覆盖两条 MR；pure-stdlib，无 venv 依赖） | — | — | `poisson-source-superposition` · `poisson-mesh-richardson` |
| **Advection 1D** | `LauncherEndToEndAdvectionTests`（端到端覆盖两条 MR；pure-stdlib） | — | — | `advection-amplitude-linearity` · `advection-mesh-conservation` |
| **Wave 1D** | `LauncherEndToEndWaveTests`（端到端覆盖两条 MR；pure-stdlib） | — | — | `wave-amplitude-linearity` · `wave-mesh-energy-convergence` |
| **Burgers 1D** | `LauncherEndToEndBurgersTests`（端到端覆盖两条 MR；pure-stdlib） | — | — | `burgers-amplitude-peak-monotone` · `burgers-mesh-conservation` |
| **SciPy IVP Lotka-Volterra** | `LauncherEndToEndScipyIvpLotkaVolterraTests`（`[SkippableFact]`，SciPy 缺失时 clean-skip 干净跳过）· `ScipyIvpLotkaVolterraParserTests` (3) | — | — | `scipy-ivp-lv-prey-growth-monotone` · `scipy-ivp-lv-step-convergence` |
| **SciPy BVP Poisson 1D** | `LauncherEndToEndScipyBvpPoissonTests`（`[SkippableFact]`，SciPy 缺失时 clean-skip 干净跳过）· `ScipyBvpPoissonParserTests` (3) | — | — | `scipy-bvp-poisson-source-superposition` · `scipy-bvp-poisson-seed-mesh-insensitivity` |
| **跨 SUT 通用** | `MrTransformationTests` · `InputGeneratorTests`（PR #119 `GreaterThanAssertionTests` / `LessThanAssertionTests` 已随 W1 类删除；同语义现由 `Catalog/Typed/BinaryComparisonKernelTests` 覆盖） | `SystemLevelCliMt.feature` · `SystemLevelGeneratedFollowup.feature` | 2 | — |

**Launcher end-to-end 测试（按 SUT）**：`LauncherEndToEndOdeTests`（decay_chain / damped_oscillator / lotka_volterra）· `LauncherEndToEndPoissonTests`（PR #134）· `LauncherEndToEndAdvectionTests`（PR #136）· `LauncherEndToEndWaveTests`（PR #138）· `LauncherEndToEndBurgersTests`（PR #140）· `LauncherEndToEndScipyIvpLotkaVolterraTests`（T3C-IVP，`[SkippableFact]`）· `LauncherEndToEndScipyBvpPoissonTests`（T3C-BVP，`[SkippableFact]`）。

**SUT 系统级 MR 总数（2026-05-26，post-T3C-BVP）**：
- launcher / manifest catalog：**29** MR-on-SUT 绑定
- 覆盖方程：**12**
- 当前结构风险：runtime 已切到 provider-backed catalog，生产 fallback 与 importer 具体类耦合已删除；sample-level evidence 已落第一条可复盘链，但覆盖粒度仍可继续扩展。T3 代表性 PDE-class 覆盖（椭圆 / 一阶线性双曲 / 二阶线性双曲 / 非线性双曲）已通过 PR #134 / #136 / #138 / #140 闭环；T3C-IVP 通过 `scipy-ivp-lotka-volterra` 把 External-solver-pilot 接入路径打通（`LauncherOptions.ScipyPython` + `PythonExecutableKinds.Scipy` + `ManifestMrCatalogProvider` scipy 分支 + `ScipyTestPaths.cs` clean-skip helper，env var `METBENCH_SCIPY_PYTHON`）；T3C-BVP 通过 `scipy-bvp-poisson-1d` 把 BVP/elliptic external-solver 路径打通（复用 T3C-IVP 基础设施，无新框架变更）；进一步 T3 扩展由 next-SUT decision record 决定（见 `docs/status/current.md` §4 与 active plan index）

---

## §4 MetBench 框架测试矩阵

按 `MetBench_BLL.Core/` 七大 namespace 分组 + DAL + Pipeline + Pagination：

| 模块 | namespace | 测试目录 | test class 数 | [Fact] 数 |
|---|---|---|---|---|
| **SystemMT** (Launcher / Runner / Adapter) | `MetBench_BLL.SystemMT.*` | `SystemMT/` | 18 | 55 |
| **Discovery** (4 类 Discoverer + Validator + Seed + Pairing) | `MetBench_BLL.Discovery.*` | `V2Discovery/` | 14 | 93 |
| **Anomaly** (List / Commonality / Status / KnownBug) | `MetBench_BLL.SystemMT.Anomaly` | `V2Anomaly/` | 1 | 13 |
| **Coverage** (4 维) | `MetBench_BLL.Coverage` | `V2Coverage/` | 1 | 18 |
| **Reporting** (HTML + 5-scope service) | `MetBench_BLL.Reporting` + `MetBench_BLL.SystemMT.Reporting` | `Reporting/` + `V2Reporting/` | 2 | 17 |
| **Mutation** (campaign × matrix) | `MetBench_BLL.Mutation` | `V2Mutation/` | 1 | 8 |
| **Pipeline** (v2 orchestration + Replay + typed predicate dispatch；PR #118 起断言阶段已切到 `Catalog/Typed/Runtime/PredicateDispatcher`，`AssertionEvaluator` 不在生产路径，仅 V2Pipeline 单测保留) | `MetBench_BLL.SystemMT.Pipeline.*` | `V2Pipeline/` | 6 | 48 |
| **RCaseRepro** (论文核心 - F9) | `MetBench_BLL.SystemMT.RCase` | `V2RCaseRepro/` | 1 | 11 |
| **Persistence (LiteDB)** | `MetBench_BLL.SystemMT.Persistence` + `MetBench_DAL` | `SystemMT/Persistence/` + `V2Schema/` | 2 | 7 + 22 |
| **Pagination** (Keyset) | `MetBench_BLL.Paging` + `MetBench_DAL.V2.*` | `V2Pagination/` | 5 | 54 |
| **Schema / Entity** (round-trip + soft-delete + migration) | `MetBench_Domain.V2` | `V2Schema/` | 5 | 9 |
| **Transformations** (v2 IMRTransformation) | `MetBench_BLL.Discovery.Transformations` | `V2Transformations/` | 3 | 20 |
| **Typed Semantic Catalog** (typed semantic model + validator + verifier runtime + migration helpers，v1.2 PR-0..PR-10 + review-fix + PR-B/C/D 收敛) | `MetBench_BLL.SystemMT.Catalog.Typed.*` | `SystemMT/Catalog/Typed/` | 41 | 102 |
| **ColdStart** | — | `ColdStart/` | 1 | 1 |

**测试总数对照**：
- 当前共享精确 Linux / cloud 绿基线：提交 `5d4dcc7`（PR #119）= **1048 pass / 0 fail / 8 skip / 1056 total**（8 skip 为 OpenMOC / OpenMC 集成测试，未安装 Python venv 时干净跳过，与回归无关）
- v1.2 迁移 / gate 当前真相层：**44 MR + 4 Property** 已进入 typed catalog 工件、golden fixtures 与 coverage gate
- 历史参考基线：`e839214`（PR #110）= **1043 pass / 0 fail / 0 skip**（PR-B/C/D 前）；`373bb59` = **961 / 0 / 8 / 969**；`763e067`（PR #93）= **965 / 0 / 0**
- 当前 Windows WPF 已知旧基线：2026-05-24 在 Parallels Win11 上 `dotnet build MetBench_Client/MetBench_Client.csproj` **0 编译错误**，约 `17.47s`；本轮最新代码回执待补
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
| **`dotnet-test.yml`** | push main / PR | `ubuntu-24.04` + .NET 8 + cross-platform tests + perf baseline gate（120s 预算） |
| **`f11-monthly-monitor.yml`** | cron `17 3 1 * *` + manual dispatch | F11 m_adj 路径 A — 拉 `mit-crpg/OpenMOC` 近 90 天 commit，grep "adjoint"；命中自动开 issue（labels `f11-monitor` + `rfc-followup`） |

WPF 的 `MetBench_Client/` 因 SDK targets 限制 **不在 Linux CI 编译**；视觉 + 运行时验证由 Windows VM 端开发者负责。

---

## §8 关键命名约定（v2.1 已统一）

| 概念 | 现在用 | 已废弃 |
|---|---|---|
| MR 在 launcher boundary 的 UI 投影 | `MrSummary` | ~~`ScenarioDescriptor`~~（PR #58 改名） |
| 单次 MR 跑动结果 | `MrRunResult` | ~~`ScenarioRunResult`~~（PR #58） |
| Launcher 接口 | `ISystemMtLauncher` | ~~`ISystemMtMrLauncher`~~ → ~~`ISystemMtScenarioLauncher`~~（PR #58 之后继续收敛） |
| Launcher 实现 | `SystemMtLauncher` | ~~`SystemMtMrLauncher`~~ → ~~`SystemMtScenarioLauncher`~~（PR #58 之后继续收敛） |
| Batch 单元 | `BatchMrRunRequest` | ~~`BatchScenarioRequest`~~（PR #58） |
| MR 标识符 | `MrId` | ~~`ScenarioId`~~（PR #58） |
| Persistence 字段 | `MrName` | ~~`ScenarioName`~~（PR #62 schema migration） |

**核心理由**：消除与 BDD Gherkin `Scenario` 撞名的混淆。Launcher 与 persistence 层的 "scenario" 词根全部消除，统一 MR 术语。LiteDB 自动 schema migration 兼容老 `.Litedb` 文件（PR #62）。

---

## §9 当前运行时注意点

- `SystemMtLauncher` 已从硬编码蓝图切到 provider-backed catalog，构造函数现要求显式注入 `IMrCatalogProvider`，生产路径不再静默 fallback。
- `SystemMtExecutionRecorder` 已写入 `ExecutionEvidence`、`V3MrIdRef` 与目标字段级 `SampleTraces`（source / transformed / output triples）；更细粒度的多变量 trace 仍可后续扩展。
- `LauncherCatalogV2Importer` 已通过 `ISystemMtCatalogReader` 读取 runnable catalog，`App.xaml.cs` 不再依赖 `SystemMtLauncher` 具体类强转。
- `MetBench_BLL.Core/SystemMT/Catalog/Typed/`（原 `SystemMT/V12Catalog/`，PR #115 重命名永久化）已合入 PR-0..PR-10，并由 PR #110 完成 retrospective review-fix：typed schema / anti-legacy lint / fail-closed validator / scalar / applicability / convergence / sequence / field / derived / statistical / cross-method / property / exponential-growth runtime 与 typed migration + coverage gate 均为主线事实。PR #118 进一步把 System MT pipeline 断言阶段切到 `Catalog/Typed/Runtime/PredicateDispatcher`；PR #119 在 `Architecture/SemanticCatalogBoundaryTests.cs` 加守卫并删除 W1 `IMrAssertion` / `ApproxEqualAssertion` / `GreaterThanAssertion` / `LessThanAssertion` / `SystemMtRunner` / `EqualityThresholds`。
- `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/` 是把 legacy assertion-type-code（`less` / `greater` / `approx` + `flw = k * src`）映射到 typed predicate 的唯一生产入口（`LegacyAssertionPredicateMapper` / `TypedSpecFactory` / `TypedVerificationContextFactory`）；其它生产路径不得直接构造 `BinaryComparisonPredicate` / `ScaledEqualityPredicate` 从字符串。
- inventory 口径以仓库 migration 资产与 gate 为准：当前主线事实是 **44 MR + 4 Property**，不要再沿用旧的“43 MR + 4 Property”汇总说法。
- `.codegraph/` 是本地图谱索引产物，不属于仓库正式架构的一部分，也不应纳入结构文档或版本化事实源。

## §10 接入新 SUT 的 checklist

当要继续加新 SUT（如 SU2 / FEniCS / 新的 neutron solver），按 [F13 RFC](superpowers/plans/2026-05-17-f13-third-sut-rfc.md) §6 走，并以当前 manifest/provider 路径为准：

- [ ] `.claude/web-setup.sh` 加该 SUT 的 venv / binary 安装段
- [ ] `LauncherOptions.<sut>Python` 字段 + DI 默认值
- [ ] `SUT/<sut>/catalog.json` 补齐 runnable catalog entry；如需新字段，同步 `MrCatalogEntry` / `ManifestMrCatalogProvider`
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
| **第 5 SUT** (SU2 / FEniCS) | 🟡 未启动 | 等 reviewer 反馈和顶层路线决策决定要否补跨域 SUT |
| **WPF UI 验收** | 🟢 v2.1.0 round-1 已完成 | 后续 WPF/UI 变更按 PR Gate Windows 分类补 build / run-and-log / UI-visible 回执 |
| **`HandyControl` → `Microsoft.Xaml.Behaviors.Wpf`** | 🟡 旧代码兼容 | v2.2+ refactor 跟 UI 整改一起做 |
| **6 个 `Service` 拼写修正废弃别名** | 🟢 v2.2 删除 | `[Obsolete]` 已标 1 版 |

---

最后结构快照：代码测试基线 `e839214`。下次主要结构变更（接新 SUT / 扩展 sample-level evidence 粒度 / 推进下一阶段 assertion 语义与配置接线 / 改命名约定）后更新本文件。
