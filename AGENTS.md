<claude-mem-context>
# Memory Context

# [MetBench-V2.1.4_2] recent context, 2026-05-07 9:37pm GMT+8

No previous sessions found.
</claude-mem-context>

# MetBench System-level MT Roadmap

This project is being extended from method/unit-level metamorphic testing (MT) to
system/acceptance-level MT. The staged plan below is the current working
baseline for architecture and implementation decisions.

> 制订 / 维护 `docs/superpowers/plans/` 下的计划，遵循 [`CLAUDE.md`](CLAUDE.md)
> 「计划工作流」闭环：读 AGENTS.md → 读相关 plan → 读 CLAUDE.md 约定 → 写 plan →
> 回写 AGENTS.md（若改动路线图）。验收准则同见该节。

## Overall Direction

MetBench will support Gherkin-based MR specifications, Reqnroll-based BDD
execution, CLI-based system-under-test invocation, C# business orchestration,
WPF user interaction, and Python-based input/output file adapters for scientific
computing programs.

Responsibility boundaries:

- WPF is the UI layer.
- C# BLL is the business orchestration layer.
- Reqnroll is the BDD execution layer.
- CLI runners invoke external programs under test.
- Python adapters handle program-specific input/output file conversion and
  parsing.
- Python adapters must not own the test workflow; workflow control remains in
  C# and Reqnroll.

## Stage 1: System-level MT Representation and BDD Execution

Goal:

Extend MetBench from method/unit-level MT to system/acceptance-level MT. This
stage must establish the minimal closed loop:

Gherkin MR scenario -> Reqnroll execution -> CLI program invocation -> output
file parsing -> MR assertion.

Main content:

- Add Gherkin/BDD feature representation for system-level MR scenarios.
- Use Reqnroll as the BDD execution framework.
- Keep WPF as the UI entry point and C# BLL as the orchestration layer.
- Add a system-level MT task model that coexists with the current method-level
  MT model.
- Support CLI invocation of programs under test.
- Support the first closed loop with file input and file output.
- Use Python adapters only for input/output file conversion and parsing.

Acceptance criteria:

- At least one `.feature` file describes a system-level MR test scenario.
- Reqnroll executes the feature and calls C# step definitions.
- C# starts an external program or example program through a CLI command.
- The system reads source and follow-up output files.
- At least one MR assertion produces a pass/fail result.
- Existing method-level MT behavior remains unaffected.

## Stage 2: Input Data Generation and Follow-up Input Derivation

Goal:

Add automatic or semi-automatic source-to-follow-up input generation on top of
the Stage 1 execution loop.

Main content:

- Define the input transformation model for system-level MRs.
- Generate follow-up input files from existing source input files.
- Define an input generation plugin interface.
- Use Python for concrete input file rewriting, template filling, numeric
  perturbation, and structure-preserving copy operations.
- Reserve an extension point for future Randoop integration, without making
  Randoop mandatory in this stage.
- Record generation artifacts: source input, follow-up input, transformation
  rule, parameter summary, and logs.

Acceptance criteria:

- Given a source input file and MR transformation configuration, the system
  generates a follow-up input file.
- Generation artifacts are traceable and persisted.
- Stage 1 BDD execution can consume the generated follow-up input file.
- At least one numeric transformation is supported, such as scaling,
  translation, replacement, or range perturbation.
- Input generation failures produce explicit errors.

## Stage 3: OpenMOC Single-program Application

Goal:

Apply the Stage 1 and Stage 2 mechanisms to a real scientific computing
program, starting with OpenMOC, to validate system-level MT for neutron
transport software.

Main content:

- Define OpenMOC program configuration: CLI command, working directory, input
  path, and output path.
- Implement `openmoc_adapter.py`.
- Define an OpenMOC input intermediate representation that keeps only MR-relevant
  variables and values.
- Map the intermediate representation back to OpenMOC input files.
- Parse OpenMOC output files and extract values required by MR assertions.
- Write at least one or two OpenMOC system-level MR feature files.

Acceptance criteria:

- MetBench/Reqnroll starts OpenMOC for a source case.
- The system prepares or generates a follow-up case and starts a second run.
- OpenMOC output files are parsed for key result values.
- At least one OpenMOC MR executes end to end and returns pass/fail.
- OpenMOC-specific logic is isolated in the adapter.
- Changing the OpenMOC adapter does not require changing the generic Reqnroll
  feature execution framework.

## Stage 4: Platform Enhancements and Reporting

Goal:

After system-level MT has been validated on a real program, add platform-level
features such as result management, reporting, visualization, batch execution,
and multi-program extension.

Main content:

- Add WPF support for configuring, launching, and monitoring system-level MT
  tasks.
- Persist feature files, input artifacts, output artifacts, assertion results,
  and logs.
- Generate system-level MT reports.
- Support batch execution of multiple features or scenarios.
- Add a second program adapter, such as OpenMC, to validate the cross-program
  path.
- Gradually support running the same MR on different programs through an
  intermediate representation and adapter pattern.
- Reports should distinguish source/follow-up inputs, execution commands,
  output summaries, MR assertion evidence, and pass/fail results.

Acceptance criteria:

- Users can launch system-level MT tasks from WPF.
- Each run result is persisted and can be reviewed later.
- At least one report format is generated, such as HTML, Word, PDF, or Excel.
- Multiple BDD scenarios can be executed in batch.
- At least a design or prototype exists for a second program adapter.
- Reports clearly show the MR, source case, follow-up case, actual outputs, and
  pass/fail evidence.

## Stage 5: Phase-3 Tallies + Temperature + Visualization (delivered 2026-05-13)

See `docs/superpowers/plans/2026-05-13-stage5-phase3-tallies-and-temperature.md`
and `docs/superpowers/plans/2026-05-13-stage5-phase3-visualization.md`. Adds
MR02-tally / MR03-tally / MR-T scenarios and dashboard.html visualisation;
binds R-Case-2/3/5 to live cells via OpenMC-side parser hooks.

## Stage 6: v2 development P1-P8 (delivered 2026-05-13)

See `docs/superpowers/plans/2026-05-13-v2-development-plan.md` (8-week plan).
All cloud-side P1-P8 work shipped on `claude/continue-phase-2-AdZ6f`:

| Phase | Cloud deliverable | VM-deferred |
|-------|-------------------|-------------|
| P1 | 23 collections + entity model + DbConfig | — |
| P2 | 21 IDAL interfaces + 22 LiteDB repos | — |
| P3 | `IMRTransformation` + 6 Python parsers + PathResolver | — |
| P4 | FluentAssertions MT extensions + `SystemMtPipeline` + `ReplayService` | — |
| P5 | Reqnroll v2 steps + feature↔DB sync tools + migrations | — |
| P6 | `AnomalyService` + `CommonalityReport` | Anomaly viewer page |
| P7 | `Discovery` (IMRDiscoverer + 3 Validator + `ValidationService`) + `MutationCampaignService` | Discovery / Mutation pages |
| P8 | `CoverageService` + `TrendAnalysisService` + `SystemMtReportService` (5 scope) + paper-package | Coverage / Trend dashboards, e2e demo |

Cloud-side test footprint (as of P8 ship): **321 xUnit pass / 2 skip / 0 fail
+ 27 Python pass**. WPF pages and end-to-end smoke against real
OpenMOC/OpenMC SUTs are VM-side responsibilities (Stage 6 VM follow-up).

Acceptance criteria (v2 ship):

- Every BLL.Core service has TDD coverage with fake repository injection.
- Every Python helper has contract tests guarding stdout JSON shape.
- `tools/build_paper_package.py` produces a reproducible tarball.
- All cloud-side P PRs merged onto the integration branch; CI green.

## Stage 7: W11-W12 (delivered 2026-05-17)

Post v2.1.0-rc1 工作：consolidate v2.1 发版 + 论文核心补充 + 框架命名清理。

| Theme | 交付 | PR |
|-------|------|-----|
| **W11.2 Multi-LLM consensus 真实跑通** | DeepSeek + OpenAI + Claude 60/60 calls，consensus accuracy 100%，mean κ = 0.925；唯一非 unanimous (`MR-sin-full-period`) 是 LLM 间"数学 vs 浮点严格等"口径分歧，strict majority 正确吸收。infrastructure 验证完整。 | #57 |
| **W12 F13 OpenMC 第 3 SUT 接入** | cmake 源码 build → `/opt/openmc` + Python bindings → `/opt/openmc-venv`；`SUT/openmc/{runner, adapters, sample, scg.json}` 完整；`OpenMcRunnerSmokeTests` 1/1 + cross-program BDD 4/4 (OpenMOC × OpenMC × {ScaleNuSigmaF, ScaleFuelSigmaA}) | #57 |
| **W12 F11 m_adj 路径 A 启动** | `tools/check_openmoc_adjoint.sh` + `.github/workflows/f11-monthly-monitor.yml`（cron `17 3 1 * *` UTC）+ status doc。匹中自动开 issue → 团队评估 patch | #61 |
| **scenario → MR 命名彻底统一** | launcher 层 65 处改名（`ScenarioDescriptor` → `MrSummary`、`ScenarioRunResult` → `MrRunResult` 等）+ persistence 层 `ScenarioName` → `MrName` + LiteDB 自动 schema migration | #58 · #62 |
| **UAT 双轨** | 47 用例 markdown 三段式（初始条件/操作步骤/断言）`docs/uat/test-procedures.md` 1014 行 + 21 用例 BDD wrapper `MetBench_SystemMT.Tests/Features/Uat/UC-*.feature` + Windows round-1 完整 runbook（21 WPF UI 用例 + 5 cloud-covered cross-ref）`docs/uat/runbooks/windows-uat-round-1.md` | #59 · #60 · #63 · #65 · #67 |
| **flake 根治 + baseline 刷新** | `DbConfig.Instance` 跨 class 竞态用 `[Collection("DbConfigGlobal")]` 注解 6 个类根治；baseline-2026-05-17 reference 521/521 0 skip 0 fail 35s wall | #64 |
| **PROJECT-STRUCTURE.md** | 项目结构 / 4 SUT 测试矩阵 / MetBench 框架测试覆盖 / UAT 双轨 一目了然 | #66 |

Cloud-side 测试态（baseline-2026-05-17）：**521 xUnit pass / 0 skip / 0 fail / 35s wall / 73.02s cumulative**。Post-Stage 7 head `9b89f9b` 处 cloud 复跑：**536 pass / 3 skip(冷启动 OpenMC import gate flake, warm 后全 pass) / 0 fail / 44.9s wall**。

**v2.1.0 发版前置全部清零（2026-05-19）**：

- Windows UAT round-1 (commit `0c0cd24`, 2026-05-18, limeng on Parallels Win11) — **CONDITIONAL PASS** 11/26，找到 3 个 Major bug (UC-A2/A5/B7)。
- 3 个 Major fix 落地：PR #71 / #72 / #75（UpdateService excludeSelf + Entity.ToString + SystemMtMrLauncher 接 AnomalyService）。
- Windows UAT round-2 (commit `9b89f9b`, 2026-05-19, limeng on Parallels Win11 ARM) — **PASS 5/5**：UC-A2 / UC-A5 / UC-B7 + 加跑 UC-B8 / UC-B9 全过。Round-2 过程命中 cross-track bug（ObjectId↔Guid 不兼容），PR #77 inline 做结构性修（`SystemMtResultRecord.Id: string→Guid` + 一次性 idempotent migration + 3 个回归测试）。
- 配套 infra：PR #73（Docker SUT `metbench-sut` + `metbench-runtime` all-in-container）+ PR #74（VM 任务书）。

**v2.1.0 已 tag**（`release-v2.1.0` @ `9b89f9b`，2026-05-19）。

**v2.1.1 hotfix**（tag `release-v2.1.1` @ `7a6e228`）：post-release Windows TZ 触发 LiteDB v5 默认反序列化把 `DateTime` 还原为 `Kind=Local`，CST=UTC+8 主机 `Ticks` 偏移 8 小时，间接破坏 2 个 `KeysetPaginationTests`（Linux CI 跑 UTC 漏报）。PR #79 在 `LiteDbSystemMtResultRepository` 连接串加 `UTC_DATE=true` pragma 修。

后续工作进入 Stage 8（v2.2 主线）。

## Stage 8: MR 库 + 平台基线扩展（启动 2026-05-18，v2.2 主线）

> **定位调整（2026-05-21）**：项目定位由「反应堆物理基准平台」放宽为**通用
> System-MT 平台与基线** —— 凡求解显式数学物理方程的程序皆可作 SUT，按 ODE / PDE
> 选代表性方程，反应堆物理 5 方程为优先锚定子集。详见 [`CLAUDE.md`](CLAUDE.md) §1。

**高层目标**：在 MetBench 内构建可执行 MR 库 —— 覆盖代表性 ODE / PDE 方程 × 程序
类型（Num / MC / Surr / PINN）矩阵，产出三元组（程序集 / MR 集 / 测试用例集）+ 覆盖
矩阵。上游对接 P-series 研究纲领（Cmrlibrary 57 种子 MR + PWR_MR_Analysis 27 MR +
NOETHER 元模式）。术语见 [`docs/GLOSSARY.md`](docs/GLOSSARY.md)。

**两个 Goal**：
- **Goal 1** — 元模式驱动 meta-prompt MR 识别引擎（功能分层中 T4 的一条技术路线）。
- **Goal 2** — cells × 元模式矩阵 + 84 条候选 MR 母集落地（T3 覆盖）。

**交付状态**：v2.1.0/.1/.2 已发布；polish 批次 Anomaly severity/category 分级已并入
main（PR #83）。Stage 8 主线：地基 5D tag schema（Phase 8.0）待落地；代表性 SUT 接入
计划 **P1 已交付**（decay_chain / damped_oscillator / lotka_volterra 三个 ODE SUT +
launcher catalog，2026-05-22）。MR/程序元信息持久化计划 **P-A + P-C + P-B 全交付**
（2026-05-22）—— P-A：`ApproxEqual` 等式断言 + `EqualityThresholds`；P-C：方程 / MR
元信息 schema（`EquationMetadata` / `MrMetadata` + `LiteDbSystemMtMetadataRepository`
+ 5 方程 8 MR seed catalog + 漂移守卫）；P-B：运行记录扩样本点级输入配对
（`InputSamplePoint` / `InputCaseReader` + `SystemMtResultRecord.InputSamples`）。该计划的
**缩放等式 assertion**（`flw≈k·src`，需扩 `IMrAssertion` 签名，升 P1 的 3 条齐次 MR 由
MP_mono 到 MP_inv）由 DP-2 转入本 Stage MR 库工作。

**暂缓**（Stage 9+ 候）：BNCT 硼中子放疗、故障注入 V3、论文 writeup。

**Stage 8 主线之外待完善**（2026-05-22 从原 `CLAUDE.md` §4 迁入）：

- **变异模块增强** —— 语义变异与语法 / 句法变异的分型生成、等价变异体识别、最小
  MR 完备子集搜寻。Stage 8 将产出 84 候选 MR，需客观证明其检错能力并剔除冗余；
  等价变异体若不识别会人为压低杀死率、污染有效性结论；最小完备子集让 MT 以最少
  MR 达到同等检错力、降低执行成本。
- **5 个 UAT UI 缺口** —— Dashboard 导航入口、HTML 报告内嵌查看等。部分后端能力
  已实现但 UI 上不可见，价值未释放（backlog：`docs/superpowers/plans/2026-05-21-uat-ui-gaps-backlog.md`）。
- **DP-3 配置绑定** —— severity 阈值的 `appsettings` 绑定（WPF 侧）未接，现回退默认值。
- **F11 m_adj 路径、第 5 个 SUT** —— 受外部依赖（OpenMOC 伴随模式、商业程序获取）
  阻塞，被动监控中（见上方 W12 F11 与 [F13 RFC](docs/superpowers/plans/2026-05-17-f13-third-sut-rfc.md)）。

**详细计划**（实施细节、phase 分解、工时、决策点以这些文档为准）：
- [下一阶段开发计划](docs/superpowers/plans/2026-05-21-next-stage-development-plan.md) —— 按 T0–T6 的总排期（**当前**）
- [代表性 SUT 接入计划](docs/superpowers/plans/2026-05-21-representative-sut-onboarding-plan.md) —— SUT 选型已放宽、home-grown 取消（**当前**）
- [MR/程序元信息持久化计划](docs/superpowers/plans/2026-05-22-mr-program-metadata-persistence-plan.md) —— P-A/P-C/P-B 核心三 phase 全交付
- 程序选型：[`docs/t3-program-selection.md`](docs/t3-program-selection.md)
- [meta-prompt MR 识别引擎计划](docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-plan.md)
- [Stage 8 MR 库原始详细计划](docs/superpowers/plans/2026-05-18-stage8-expanded-mr-library-plan.md) —— 定位放宽前所写，其「5 方程 + home-grown」部分以上述「当前」文档为准

> 本节原含「5 方程 × 4 程序类型 + 4 home-grown」的详细 phase 拆解；因定位放宽，
> 细节已迁移并更新至上述 plans —— AGENTS.md 只保留路线图层面的高层描述与指针。
