<claude-mem-context>
# Memory Context

# [MetBench-V2.1.4_2] recent context, 2026-05-07 9:37pm GMT+8

No previous sessions found.
</claude-mem-context>

# MetBench System-level MT Roadmap

This project is being extended from method/unit-level metamorphic testing (MT) to
system/acceptance-level MT. The staged plan below is the current working
baseline for architecture and implementation decisions.

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

Cloud-side 测试态（baseline-2026-05-17）：**521 xUnit pass / 0 skip / 0 fail / 35s wall / 73.02s cumulative**。

剩余前置（v2.1.0 发版）：Windows 端 UAT round-1 跑通 **21 个 WPF UI 用例**（A1-A7 + B1-B9 + E1-E5；其余 5 个 A8/D1/D2/E6/E7 是 CLI，cloud baseline-2026-05-17 已覆盖） → dashboard `PASS` → tag `release-v2.1.0`。

## Stage 8: 元模式驱动 MR 识别 + 反应堆物理 5 大方程 SUT 覆盖（启动 2026-05-18，v2.2 候）

post v2.1 发版后的两个并行工作线，论文 contribution 加分。**v2.2 主线**，详见同期 RFC + plan 文档。

### Goal 1: 基于元模式的结构化 meta-prompt MR 识别引擎

**目标**：把 8 个 NOETHER MetaPattern 从"数据库 seed 行"升级成"可驱动 LLM 识别 MR 的 prompt 模板"。给定一个 SUT 的输入文件 schema + 参数说明 + 数学物理方程上下文，自动：

1. 解析 SUT 输入 → 抽参数名 + 类型
2. 依据方程性质（守恒律 / 对称 / 单调性 / 收敛性 / 跨实现一致 …）选匹配的 MetaPattern 子集
3. 用 MetaPattern 对应的**结构化 meta-prompt 模板** + 该 SUT 参数填充 → 生成 SUT-specific MR 识别 prompt
4. 调 LLM（复用现有 `OpenAiCompatibleLlmGateway` + `MultiLlmConsensusValidator`）
5. 解析 LLM 响应为 MR candidate + confidence → 入 CandidateRepository

**论文价值**：把 metamorphic testing 框架的"凭经验写 prompt" 提升为"基于元模式的自动 prompt 生成"，可重复 + 可比较，是 metamorphic testing 自动化研究的真实贡献。

**deliverables**:
- 8 个 MetaPattern 各自的 meta-prompt 模板（结构化，含 placeholder）
- `SutParameterExtractor` / `MetaPromptBuilder` / `LlmMrIdentifier` 三个 service
- 端到端 demo：amax.py SUT → 至少 1 个识别出的 MR candidate
- TDD 覆盖（fake gateway + 真实 LLM gateway sanity test）

**详细计划**: [docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-brainstorming.md](docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-brainstorming.md) → [...-plan.md](docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-plan.md)

### Goal 2: 反应堆物理 5 大方程 SUT 覆盖

**目标**：当前 4 SUT 偏中子物理（OpenMOC + OpenMC neutron transport），其余 heat_equation + projectile 是 demo。要让框架覆盖**反应堆物理工程实践中真实关心的 5 大方程**：

1. **中子输运**（Boltzmann transport equation） — 已有 OpenMOC + OpenMC ✅
2. **燃耗 / 核素演化**（Bateman equation） — 待接 SUT
3. **燃料热传导**（fuel pin heat conduction） — 待接 SUT
4. **冷却剂热工水力**（thermal-hydraulics, 1D 子通道） — 待接 SUT
5. **反应堆动力学**（point-kinetics / space-time kinetics） — 待接 SUT

对每个待接方程：
- 调研 2-3 个开源候选程序（github / pip 可获取，cloud Linux 友好）
- 用 Stage 8 Goal 1 的 meta-prompt 引擎自动生成 MR 候选
- 选 ≥ 1 个 MR 落地为 SUT scenario，跑通 MT 流程
- 录入 MetBench LiteDB + UAT BDD 加 scenario

**论文价值**：从"演示 metamorphic testing 适用于多种 numerical solver" → "覆盖反应堆物理工程**完整**方程栈"，论据强度大幅提升。

**deliverables (5 阶段)**:
- Phase 8.2.1 — Bateman / 燃耗：用 OpenMC depletion 模块（已有 binary），1 个 MR
- Phase 8.2.2 — Fuel heat conduction：home-grown Python 1D 径向求解器（无外部依赖）+ 1 MR
- Phase 8.2.3 — Thermal-hydraulics 子通道：home-grown Python 1D channel 或 PyNE 候选
- Phase 8.2.4 — Point-kinetics：home-grown Python ODE（最简单）
- Phase 8.2.5 — paper writeup: "5 equations coverage" 实证

**详细计划**: [docs/superpowers/plans/2026-05-18-reactor-physics-five-equations-brainstorming.md](docs/superpowers/plans/2026-05-18-reactor-physics-five-equations-brainstorming.md) → [...-plan.md](docs/superpowers/plans/2026-05-18-reactor-physics-five-equations-plan.md)

### Stage 8 时间盒

- W13 (2026-05-18 ~ 25): Goal 1 设计 + 实施（meta-prompt 引擎跑通 amax demo）
- W14-W16 (2026-05-26 ~ 06-15): Goal 2 五阶段轮转接 SUT
- W17 (2026-06-16+): 论文 writeup + UAT round-2

**不阻塞 v2.1 发版**。v2.1 发版后立即启动。
