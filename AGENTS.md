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

## Stage 8: MR 库 — 多专业域 × 多程序类型矩阵覆盖（启动 2026-05-18 / 取代旧 5-equation scope，v2.2 主线）

**上游对接**: P-series 研究纲领的 [Cmrlibrary.md 5 维 schema](.)（外部上传，未入仓）+ NOETHER 8 元模式 + 12 网格选定。MetBench 升级为 P-series MR 库的**可执行存储 + MT 执行载体**。

**核心扩展**（vs 旧 Stage 8 narrow scope）:
- 程序类型 4 类正交：**数值模拟 / 概率（MC）/ ML 代理 / PINNs**
- 专业域 5 类：中子输运 / 中子扩散 / 燃耗 / 热工 + **新增 BNCT（硼中子放疗）**
- MR schema 升级：5 维索引（方程 × 程序类型 × 元模式 × 来源层次 × 故障关联）
- 推导：每 cell（方程 × 程序类型 × 域）→ 8 元模式 → 似然 MR 候选 → V1/V2/V3 三层验证 → 入库
- Deliverable 框架：**三元组（程序集 / MR 集 / 测试用例集）per cell**

### Goal 1: 元模式驱动 meta-prompt MR 识别引擎（carryover from old scope）

把 8 NOETHER MetaPattern 升级成 LLM-driven 自动 MR 识别引擎。详见 [meta-prompt-mr-discovery-brainstorming.md](docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-brainstorming.md) + [...-plan.md](docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-plan.md)。**保留不变**，作为 Goal 2 推导矩阵的工具基础。

### Goal 2: 多专业域 × 多程序类型 MR 库矩阵（**取代** 旧 "5 reactor equations" scope）

按 Cmrlibrary.md 5 维 schema 在 MetBench 内构建 MR 库，覆盖：

**方程维 (D₁，按 Cmrlibrary 编码)**:
- **A**: Boltzmann 中子输运（OpenMC / OpenMOC / MCNP / NEWT）
- **B**: 中子扩散（PARCS / NESTLE / OpenNodal）
- **C**: Bateman 燃耗（ORIGEN / OpenMC depletion / PyNE）
- **D**: Fourier 热传导（FRAPCON / BISON 简化 / home-grown）
- **E**: Navier-Stokes 简化系统级（RELAP5 / CTF / OpenFOAM 子集）
- **F**: 蒙特卡洛专有 MR（OpenMC / MCNP 共享）
- **G**: ML 代理 / PINN 专有（DeepONet / R²-PINN / FNO）
- **H**（新增）: **BNCT 剂量学**（SERA / TOPAS / MCNP-BNCT / NCTPlan）

**程序类型维 (D₂)**: D1 数值确定性 / D2 蒙特卡洛 / D3 代理模型 / D4 PINN

**元模式维 (D₃)**: P₁ 不变性 / P₂ 单调性 / P₃ 仿射收敛 / P₄ 退化极限 / P₅ 一致性 + m_adj + m_rev + P₉ 候选

每 cell 工作流（**单一 cell deliverable**）:
1. **方程归类** → 该 cell 对应数学物理方程明确
2. **程序候选搜索** → ≥2 个开源 / 公开程序（github / pip / 学术 release）
3. **元模式 × 方程推导** → 8 元模式逐一过 → 该方程下的似然 likely MR 列表（含适用域 + 关系类型 + 容差量级）
4. **录入 MetBench**: MR YAML + LiteDB 入库 + 5D 索引完整
5. **MT 执行**: 至少 1 个程序 × 至少 1 个 MR 跑通 source + followup + assertion
6. **三层验证 (V1/V2/V3)**: 数学可推导性 + 程序执行 + 故障注入检出力
7. **测试用例归档**: source + followup + 输入变换脚本 + 容差 + 适用域声明 + 自动化等级

**三元组终态 per cell**: 一组程序 + 一组 MR（YAML 入库）+ 一组测试用例（BDD + trx baseline）。

**论文价值**:
- 取代 "5 个 reactor 方程演示" 的薄证据 → "8 方程 × 4 程序类型 ≈ 30 cells 矩阵覆盖"
- 直接喂给 P-series（P1 经验审计 / P2 IST SMS 度量）作为实证基础
- BNCT 加入扩域到放射肿瘤 / 医学物理边缘，论文新颖性

### Goal 3 (新增): BNCT 硼中子放疗专属 cell

BNCT 是医学物理领域，与反应堆物理共享 Boltzmann 输运求解器但有专属物理:
- **附加方程**: 剂量学（DRBE 等）+ 生物效应（LQ 模型 / RBE）
- **专属 MR 来源**: 剂量-反应单调（剂量 ↑ → 杀伤率 ↑）/ 几何对称（球肿瘤模型）/ 跨实现一致（TOPAS vs MCNP-BNCT）
- **候选程序**: SERA（开源？）/ TOPAS（开源 Geant4 wrapper）/ NCTPlan（学术）/ MCNP-BNCT

BNCT 既扩域又复用 Boltzmann 元模式，是 Stage 8 矩阵的"压力测试" cell。

### Deliverable 框架

| Level | 产物 |
|---|---|
| **Cell-level** | 程序候选清单 + likely MR 表 + 至少 1 个端到端 MT 测试用例 + 5D 元数据 YAML |
| **Stage-level** | MR 库（LiteDB collection + YAML mirror）+ MR catalog .md（per cell 一节）+ 三层验证报告 |
| **Paper-level** | "多专业域 MR 库 + 元模式驱动 MR 自动识别" 实证章节（喂 P1 / P2） |

### Stage 8 时间盒（重新校准）

- W13 (2026-05-18 ~ 25): Goal 1 meta-prompt 引擎 + Cmrlibrary 5D schema 在 MetBench 落地（**Phase 8.0**）
- W14-15: 中子输运 + 扩散 + 燃耗 cell 推导 + 录入（Phase 8.A-C）
- W16-17: 热传导 + 热工 + BNCT cell（Phase 8.D-H）
- W18-19: ML 代理 + PINN 程序类型横切（Phase 8.D₃₋₄）
- W20: 论文 writeup + UAT round-N

**不阻塞 v2.1 发版**。v2.1 发版后立即启动。

详细推导矩阵 + cell 工作流见 [stage8-expanded-mr-library-brainstorming.md](docs/superpowers/plans/2026-05-18-stage8-expanded-mr-library-brainstorming.md) + [...-plan.md](docs/superpowers/plans/2026-05-18-stage8-expanded-mr-library-plan.md)。
