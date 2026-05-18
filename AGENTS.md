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

## Stage 8: MR 库 — 5 方程 × 4 程序类型 × 5 MP 矩阵覆盖（启动 2026-05-18，v2.2 主线）

**上游对接**: P-series 研究纲领 [Cmrlibrary.md 5D schema + 57 种子 MR + 三层验证] + [PWR_MR_Analysis.md 27 PWR 新增 MR] + NOETHER 8 元模式（→ 5 MP 映射见 [`docs/GLOSSARY.md`](docs/GLOSSARY.md) §5）。MetBench 升级为 P-series MR 库的**可执行存储 + MT 执行载体**。

**术语规范**：见 [`docs/GLOSSARY.md`](docs/GLOSSARY.md)（5 MP 定义+实例 / BDD 术语+实例 / 5 方程 / 程序类型 / 5D 索引 / 内部命名）。

**核心范围**:
- 反应堆物理 **5 个核心方程**：boltzmann / diffusion / bateman / fourier / NS（英文全称，前缀 `E_`）
- 程序类型 4 类正交：**Num / MC / Surr / PINN**
- MR 元模式 **5 类 MP**：MP_inv / MP_mono / MP_conv / MP_traj / MP_part（NOETHER 8 ↔ 5 MP 映射含 m_cmp 拆分：严格相等→MP_inv / 偏序→MP_part）
- MR schema：5D 索引（Equation / ProgramType / MetaPattern / SourceLevel / FailureCorrelation）通过 **BDD `.feature` + Gherkin tags + LiteDB sync** 落地（沿用现有约定，不引 YAML mirror）
- 推导：方程算子 → 适用 MP 选取 → 参数扫描 → meta-prompt → LLM → MT 执行 → 高支持入库 / 低支持反例 / discard
- Deliverable：**三元组（程序集 / MR 集 / 测试用例集）+ 17 cells 覆盖矩阵**

**暂缓**（独立模块，Stage 9+ 候）:
- **BNCT 硼中子放疗**：plan 内保留章节，Stage 8 不实施（80% 重叠 boltzmann + 程序大多商业/申请/停维）
- **故障注入 V3**：独立模块挂起；Stage 8 MR 库只做 V1+V2
- **论文 writeup**：暂不绑定（user 指令：先做实验，发现 bug 再考虑）

### Goal 1: 元模式驱动 meta-prompt MR 识别引擎

把 5 MP 升级成 LLM-driven 自动 MR 识别引擎。详见 [meta-prompt-mr-discovery-brainstorming.md](docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-brainstorming.md) + [...-plan.md](docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-plan.md)。**保留**，作为 Goal 2 工具基础。

### Goal 2: 17 cells × 5 MP 矩阵 + 84 MR 母集落地

按 5D schema 在 MetBench 内构建 MR 库，覆盖 **5 方程 × 4 程序类型 = 20 cells**（3 个 D₂ MC cell 本质不适用 → **17 实际可填**）。

**程序候选（cloud-friendly 评估）**：
- OpenMOC + OpenMC ✅ 已装（boltzmann + bateman）
- 4 home-grown（diffusion nodal + Bateman ODE + 1D Fourier + 1D subchannel）替代 PARCS / ORIGEN / FRAPCON / RELAP5 (商业 / 学术申请，cloud 不可获取)
- D₃ Surr 用 scikit-learn GP（不依赖 PyTorch / 论文 release）
- D₄ PINN 留 Stage 9

**MR 母集**：57 Cmrlibrary 种子 + 27 PWR_MR_Analysis 新增 = **84 条候选 MR**，按 5 MP × 5 方程 cell 分类。

**完整研究工作流**（per cell）：

```
方程算子 algebraic property
  → 适用 5 MP 选取
  → 输入参数扫描
  → meta-prompt 构造
  → LLM 识别 MR (多家 consensus)
  → MetBench 执行 MT
  → 三分支:
      ├─ 高支持 → 入库 (.feature + LiteDB)
      ├─ 低支持 + MP 数学应成立 → 反例归档（不刻意造）
      └─ MP 数学不成立 → discard
```

### Stage 8 时间盒（5-6 周）

- W13 (2026-05-18 ~ 25): Phase 8.0 5D tag schema + Phase 8.1 meta-prompt 引擎
- W14: Phase 8.2 现有 4 SUT 5D tag 升级 + **8.2.5 端到端 workflow 验证**
- W15-W18: Phase 8.3 4 home-grown cells (Bateman + Fourier + nodal diffusion + subchannel)
- W19: Phase 8.4 D₃/D₄ 横切试点 (Surr + MC depletion) + 8.5 cells 覆盖 dashboard

**ship 验收**：≥ 12 cells 不空白（17 实际可填）+ ≥ 15 MR 入库 + 全套 `dotnet test` 0 fail。

**不阻塞 v2.1 发版**。v2.1 发版后立即启动。

详细计划见 [stage8-expanded-mr-library-brainstorming.md](docs/superpowers/plans/2026-05-18-stage8-expanded-mr-library-brainstorming.md) + [...-plan.md](docs/superpowers/plans/2026-05-18-stage8-expanded-mr-library-plan.md)。
