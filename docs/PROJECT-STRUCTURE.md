# MetBench 项目结构

> **结构快照基线**: 2026-05-31（代码测试基线由 `docs/status/current.md` §2 实时维护；已同步 T0-T5 release readiness、client i18n、full-page bilingual UI evidence、usage guide）
> **目标读者**: 新加入仓库的开发者 / 验收员 / reviewer。文档全息呈现仓库当前结构 + SUT 测试覆盖 + MetBench 框架测试覆盖。
> **更详细的设计**: [`AGENTS.md`](../AGENTS.md)（roadmap）· [`CLAUDE.md`](../CLAUDE.md)（agent 注意事项）· [`docs/design/`](design/)（架构）
> **当前状态账本**: [`docs/status/current.md`](status/current.md)。本文件只投影结构与测试矩阵，不重新定义当前主线状态。

---

## §1 .NET 项目布局（10 个主工程/测试/分析器 csproj + 3 个 tools csproj）

| 项目 | Target | 跑哪里 | 用途 |
|---|---|---|---|
| **`MetBench_BLL.Core/`** | `net8.0` | Linux + Windows + CI | 跨平台 BLL：System-MT pipeline / provider-backed launcher / adapters / persistence contracts / reporting / anomaly / discovery / mutation / coverage。2026-05-25 当前主线已切到 `IMrCatalogProvider` + `ManifestMrCatalogProvider`，launcher 生产路径已不再保留 `HardcodedMrCatalogProvider` fallback；Typed Semantic Catalog 正式代码面位于 `SystemMT/Catalog/Typed/`（原 `SystemMT/V12Catalog/`，PR #115 重命名永久化）；pipeline 断言阶段经 PR #118 已切到 `PredicateDispatcher`，W1 `IMrAssertion` 路径在 PR #119 已从生产侧删除。 |
| **`MetBench_BLL/`** | `net8.0` | Linux + Windows + CI | WPF 侧 BLL：v1 方法级 MT 主流程 + Word/Excel/PDF 报表生成器 + LiveCharts 数据 service（无 WPF 依赖） |
| **`MetBench_Domain/`** | `net8.0` | Anywhere | 域实体：v1 方法级 + v2 四级 MR 层级（MetaPattern → MRSchema → MRBinding → MRInstance → Execution） |
| **`MetBench_IDAL/`** | `net8.0` | Anywhere | DAL 接口合约 |
| **`MetBench_DAL/`** | `net8.0` | Anywhere | LiteDB 持久化：v1 run-result + v2 24-collection schema |
| **`MetBench_Client/`** | `net8.0-windows7.0` | **Windows only** | WPF UI 应用，入口点；引 `Wpf.Ui` + `CommunityToolkit.Mvvm` + LiveCharts WPF |
| **`MetBench_UI.Localization/`** | `net8.0` | Linux + Windows + future Avalonia | UI-neutral bilingual localization core：`.resx` / `ResourceManager` / `IAppLocalizationService` / `LocalizedTextProvider`。不得依赖 WPF、WPF-UI、Avalonia 或 Windows-only API；当前 WPF 客户端引用它，未来 Avalonia UI 可复用同一核心。 |
| **`MetBench_SystemMT.Tests/`** | `net8.0` | Anywhere | xUnit + Reqnroll：跨平台事实源测试。当前 release gate 与 i18n baseline 见 `docs/status/current.md` §2；T0-T5 VM full suite 为 **1558 pass / 0 fail / 12 skip**，ClientI18n SystemMT tests 为 **10/10 PASS**。 |
| **`MetBench_Client.Tests/`** | `net8.0-windows7.0` | **Windows only** | WPF/i18n UI-facing xUnit tests（含 `Xunit.StaFact`），覆盖 MainWindow navigation localization、Settings language switcher 等客户端行为。 |
| **`MetBench_Analyzers/`** | `netstandard2.0` | Build/CI analyzer package | Governance Roslyn analyzers（如 METBENCH001 / METBENCH002），用于把跨文件字段流、防漂移等治理规则机械化。 |

Tools projects: `tools/smokeshot/` (Windows UIA / screenshot evidence), `tools/ScaffoldMr/`, `tools/SeedCrossProgramAnomalies/`.

**硬规则**（cloud 与 Windows 端协作）：

- Cloud agents 可改 `MetBench_BLL.Core/` / `MetBench_DAL/` / `MetBench_BLL/` / `MetBench_SystemMT.Tests/` / docs（**全部可在 Linux 编译**）
- Cloud agents **不可改** `MetBench_Client/*.xaml*` 或 `MetBench_Client.Tests/` 没有显式许可（Linux 不能 build WPF SDK）
- Windows agents **不可改** `MetBench_BLL.Core/SystemMT/*` public types 没先提 cloud-side 设计（CI 会卡）

---

## §2 SUT 清单（当前 launcher catalog：16 个 — 15 真实物理 SUT + 1 合成测试 SUT）

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
| **_test-csv** (合成测试 SUT) | `SUT/_test_csv/` | **非物理** — `metbench_io` helper 集成回归 | Pure-stdlib echo runner; uses `SUT/_shared/metbench_io/` csv-row helper | `_test_csv_runner.py` | `catalog.json` + sample CSV | PR-A (#162) |

辅助包：

- `SUT/_shared/metbench_io/` —— pure-stdlib Python 包，单点翻译非 JSON 输入/输出 wire format（当前支持 `csv-row` 与 `plain-text`），框架其余部分不感知 wire format。SUT runner 调用 `read_input(path, fmt=...)` / `write_input(...)`，剩下都是普通 dict。PR-A (#162) 引入。

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
| **OpenMOC** | `OpenMocInputAdapterTests` (4) + `OpenMocOutputAdapterTests` (2) + `OpenMocSigmaAInputAdapterTests` (5) + `OpenMocRunnerSmokeTests` (1) + `OpenMocSampleCaseTests` (1) + `OpenMocCatalogParityTests` (2) = **15** | `OpenMocPinCellNuSigmaF.feature` · `OpenMocPinCellSigmaA.feature` · `CrossProgramNeutronTransportMrs.feature` (1 outline，跨程序复用) | 2（单程序） | `openmoc-pincell-nu-sigma-f` · `openmoc-pincell-sigma-a` |
| **OpenMC** | `OpenMcInputAdapterTests` (5) + `OpenMcOutputAdapterTests` (5) + `OpenMcRunnerSmokeTests` (1) + `OpenMcCatalogParityTests` (2) = **13** | (共用 `CrossProgramNeutronTransportMrs.feature`) | 2（单程序） | `openmc-pincell-nu-sigma-f` · `openmc-pincell-sigma-a` |
| **OpenMOC × OpenMC 跨程序** | `CrossProgramScenarioIdReuseTests` (2) = **2**（守护 cross-program 与 single-program 复用同名 transformation） | `CrossProgramNeutronTransportMrs.feature` (2 outline，含 2 examples × 2 solvers = 4 instance) | 2（agreement，不算独立 MR） | (共享上面 4 个 single-program MR id) |
| **Heat Equation** | `HeatEquationInputAdapterTests` (2) + `HeatEquationOutputAdapterTests` (4) = **6** | `HeatEquationAmplitude.feature` | 1 | `heat-equation-amplitude` |
| **Projectile** | (依靠 `CliProgramRunnerTests` 通用覆盖) | `ProjectileRange.feature` | 1 | — (仅 BDD，未 Launcher 注册) |
| **Poisson 1D** | `LauncherEndToEndPoissonTests`（端到端覆盖两条 MR；pure-stdlib，无 venv 依赖） | — | — | `poisson-source-superposition` · `poisson-mesh-richardson` |
| **Advection 1D** | `LauncherEndToEndAdvectionTests`（端到端覆盖两条 MR；pure-stdlib） | — | — | `advection-amplitude-linearity` · `advection-mesh-conservation` |
| **Wave 1D** | `LauncherEndToEndWaveTests`（端到端覆盖两条 MR；pure-stdlib） | — | — | `wave-amplitude-linearity` · `wave-mesh-energy-convergence` |
| **Burgers 1D** | `LauncherEndToEndBurgersTests`（端到端覆盖两条 MR；pure-stdlib） | — | — | `burgers-amplitude-peak-monotone` · `burgers-mesh-conservation` |
| **SciPy IVP Lotka-Volterra** | `LauncherEndToEndScipyIvpLotkaVolterraTests`（`[SkippableFact]`，SciPy 缺失时 clean-skip 干净跳过）· `ScipyIvpLotkaVolterraParserTests` (3) | — | — | `scipy-ivp-lv-prey-growth-monotone` · `scipy-ivp-lv-step-convergence` |
| **SciPy BVP Poisson 1D** | `LauncherEndToEndScipyBvpPoissonTests`（`[SkippableFact]`，SciPy 缺失时 clean-skip 干净跳过）· `ScipyBvpPoissonParserTests` (3) | — | — | `scipy-bvp-poisson-source-superposition` · `scipy-bvp-poisson-seed-mesh-insensitivity` |
| **_test-csv** (合成测试 SUT) | `LauncherEndToEndTestCsvTests` (1, 端到端打通 `metbench_io` csv-row helper 经未改动 launcher) · `MetBenchIoHelperTests` (11, helper 单元覆盖 csv-row / plain-text round-trip / 未知格式 fail-closed / json passthrough) | — | — | `csv-roundtrip-identity` |
| **跨 SUT 通用** | `MrTransformationTests` · `InputGeneratorTests`（PR #119 `GreaterThanAssertionTests` / `LessThanAssertionTests` 已随 W1 类删除；同语义现由 `Catalog/Typed/BinaryComparisonKernelTests` 覆盖） | `SystemLevelCliMt.feature` · `SystemLevelGeneratedFollowup.feature` | 2 | — |

**Launcher end-to-end 测试（按 SUT）**：`LauncherEndToEndOdeTests`（decay_chain / damped_oscillator / lotka_volterra）· `LauncherEndToEndPoissonTests`（PR #134）· `LauncherEndToEndAdvectionTests`（PR #136）· `LauncherEndToEndWaveTests`（PR #138）· `LauncherEndToEndBurgersTests`（PR #140）· `LauncherEndToEndScipyIvpLotkaVolterraTests`（T3C-IVP，`[SkippableFact]`）· `LauncherEndToEndScipyBvpPoissonTests`（T3C-BVP，`[SkippableFact]`）· `LauncherEndToEndTestCsvTests`（PR-A 合成 _test_csv SUT）。

**SUT 系统级 MR 总数（2026-05-26，post-PR-A）**：
- launcher / manifest catalog：**30** MR-on-SUT 绑定 = 29 真实物理 + 1 合成 (`csv-roundtrip-identity`)
- 覆盖方程：**13** = 12 真实物理 + 1 合成 (`_test_csv`)
- 真实物理 inventory（排除合成 SUT）：**15 SUT / 12 equations / 29 MRs**，与 T3C-BVP 后一致
- 当前结构风险：runtime 已切到 provider-backed catalog，生产 fallback 与 importer 具体类耦合已删除；sample-level evidence 已落第一条可复盘链，但覆盖粒度仍可继续扩展。T3 代表性 PDE-class 覆盖（椭圆 / 一阶线性双曲 / 二阶线性双曲 / 非线性双曲）已通过 PR #134 / #136 / #138 / #140 闭环；T3C-IVP 通过 `scipy-ivp-lotka-volterra` 把 External-solver-pilot 接入路径打通（`LauncherOptions.ScipyPython` + `PythonExecutableKinds.Scipy` + `ManifestMrCatalogProvider` scipy 分支 + `ScipyTestPaths.cs` clean-skip helper，env var `METBENCH_SCIPY_PYTHON`）；T3C-BVP 通过 `scipy-bvp-poisson-1d` 把 BVP/elliptic external-solver 路径打通（复用 T3C-IVP 基础设施，无新框架变更）；PR-1（#157）把 `LauncherOptions.RuntimePythons` 通用化为 manifest-driven 解析（新增运行时家族纯配置即可，不再改 `LauncherOptions` 字段）；PR-A（#162）把 SUT I/O wire format 从 JSON 单独扩展到 csv-row / plain-text（`metbench_io` helper）；PR-B（#161）与 PR-2（#159）分别落地 same-equation cross-method differential runner 与 T4-to-T0 discovery binder；进一步 T3 扩展由 next-SUT decision record 决定（见 `docs/status/current.md` §4 与 active plan index）

---

## §3.1 Boltzmann MR 覆盖明细（PR-Bol-1 同步）

`SUT/openmoc/` 与 `SUT/openmc/` 各承载 **2** 条 single-program Boltzmann MR；跨程序一致性由 `CrossProgramNeutronTransportMrs.feature` 复用同一对 transformation 名实现，不算作独立 MR id。PWR MR analysis 报告（`docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md` 1052–1056 行 + `2026-05-25-v12-pwr-migration-map.md`）定义了完整的 `Bol-Phy-01..05` / `Bol-Alg-01..03` 命名族；下表给出当前可执行 MR 与 PWR Bol-* id 的对应：

| MR id | SUT | program_type | meta_pattern | PWR Bol-* 对应 | 验证状态 |
|---|---|---|---|---|---|
| `openmoc-pincell-nu-sigma-f` | openmoc | Num | Mono | **Bol-Phy-03**（fission production monotonicity，nuΣf↑ → k_eff↑） | Catalog + launcher 验证；runner 端到端 skip-safe（缺 OpenMOC venv 时干净跳过） |
| `openmoc-pincell-sigma-a` | openmoc | Num | Mono | **Bol-Phy-02**（absorption monotonicity，Σa↑ → k_eff↓） | 同上 |
| `openmc-pincell-nu-sigma-f` | openmc | MC | Mono | **Bol-Phy-03** Monte Carlo 对应 | 同上（缺 OpenMC venv 时干净跳过） |
| `openmc-pincell-sigma-a` | openmc | MC | Mono | **Bol-Phy-02** Monte Carlo 对应 | 同上 |

补充说明：

1. 跨程序一致性 (`OpenMOC ↔ OpenMC`) 由 `MetBench_SystemMT.Tests/Features/CrossProgramNeutronTransportMrs.feature` 通过两个 scenario outline 验证（`ScaleNuSigmaF` / `ScaleFuelSigmaA`，每 outline × 2 solver = 4 instance）。Cross-program agreement 是**验证维度**，复用上表 4 个 single-program MR id，不增加新的 MR id；catalog/launcher 计数仍按 single-program 维度统计。
2. `Bol-Alg-01`（MOC ray/track convergence）和 `Bol-Alg-02`（MC particle count convergence）**未在 PR-Bol-1 实施**；登记为下一批工作 PR-Bol-2 / PR-Bol-3，见 `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` §1。`Bol-Phy-01 / Bol-Phy-04 / Bol-Phy-05 / Bol-Alg-03` 仍只在 v1.2 typed-catalog 设计示例里出现，尚无可执行 MR 绑定。
3. PWR Bol-* 对应关系仅作文档级 traceability 注记，**未**写入 catalog JSON 字段（`pwrMrId` 等暂不引入，避免触发 runtime semantics 变更）。

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
| **Differential Runner** (T1 §2.1 element 3 same-equation cross-method differential，PR-B #161) | `MetBench_BLL.SystemMT.Differential.*` | `SystemMT/Differential/` | 1 | 28 |
| **Discovery → Catalog Binder** (T4-to-T0 fail-closed bridge，PR-2 #159) | `MetBench_BLL.SystemMT.Catalog.Binding.*` | `SystemMT/Catalog/Binding/` | 1 | 24 |
| **Runtime Environment Resolver** (manifest-driven `python_executable_kind`，PR-1 #157) | `MetBench_BLL.SystemMT.Launcher` (`LauncherOptions.RuntimePythons` + `RuntimeEnvironmentResolutionException`) | `SystemMT/Launcher/RuntimeEnvironmentResolverTests.cs` | 1 | 10 |
| **metbench_io Python helper** (T1 §2.1 element 2 non-JSON wire format，PR-A #162) | `SUT/_shared/metbench_io/` (Python) | `SystemMT/Shared/MetBenchIoHelperTests.cs` | 1 | 11 |
| **ColdStart** | — | `ColdStart/` | 1 | 1 |

**测试总数对照**：
- 当前 release-readiness 绿基线：`docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/vm-summary.md` 记录 **22/22** required filtered commands PASS；`dotnet test MetBench_SystemMT.Tests` full suite **1558 pass / 0 fail / 12 env-gated OpenMOC/OpenMC skips**；Windows `dotnet build MetBench.sln` **0 errors**；T0-T5 screenshot matrix **21/21 PASS**。
- 当前 client i18n 绿基线：`docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/vm-summary.md` 记录 `MetBench_SystemMT.Tests` ClientI18n **10/10 PASS**、`MetBench_Client.Tests` ClientI18n **3/3 PASS**、base UIA screenshots **9/9 PASS**；后续 `vm-status.jsonl` 追加 full-page bilingual screenshots for System-MT/catalog/legacy/function pages and runtime status strings.
- v1.2 迁移 / gate 当前真相层：**44 MR + 4 Property** 已进入 typed catalog 工件、golden fixtures 与 coverage gate
- 历史参考基线：`453e369`（PR #160 docs gate）= 1196 pass；`2f997dd`（PR #161 PR-B differential runner）= 1196 pass；`66eb297`（PR #162 PR-A I/O adapter）= 1209 pass。更早：`5d4dcc7`（PR #119）= **1048 pass / 0 fail / 8 skip / 1056 total**；`e839214`（PR #110）= **1043 pass / 0 fail / 0 skip**（PR-B/C/D 前）；`373bb59` = **961 / 0 / 8 / 969**；`763e067`（PR #93）= **965 / 0 / 0**
- 当前 Windows WPF 证据：T0-T5 release smoke 和 client i18n evidence 均记录 `dotnet build MetBench.sln` / WPF build **0 errors**，并由 `tools/smokeshot/` 产出 UIA/PrintWindow 截图证据。
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
- [ ] 在 manifest/runtime-env registry 中声明 runtime key；不要再为每个新依赖族新增 `LauncherOptions.<sut>Python` 字段（执行 `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md` 前，新增 runtime family 必须先停下来补该能力）
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
