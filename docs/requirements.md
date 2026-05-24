# MetBench 需求-功能-代码-测试 追溯矩阵

> **受控开发模式（2026-05-23 启用）**：本表是 MetBench 当前**唯一**的需求-代码-测试映射事实源。
> 一切新增 / 修改须先在本表登记功能编号（`F-Tn-xx` / `F-MR-Pn`），未登记禁止新增代码。
> 每次只处理一个功能编号；改动前需说明：(1) 涉及编号、(2) 修改文件、(3) 新增测试、(4) 不动模块。
> 没有测试对应的功能不算完成；没有功能编号对应的代码不允许随意新增。

## 0. 编号规则与文档边界

| 维度 | 来源 | 说明 |
|---|---|---|
| **需求** | `CLAUDE.md` §2（T0–T6 核心功能模型）+ `AGENTS.md` Stage 1–8 路线图 + `docs/superpowers/plans/` 单次实施计划 | 三层指针，互不复制 |
| **功能编号** | 本表（唯一） | 形如 `F-T0-01`（按 T 分层）或 `F-MR-Pn`（MR 协议层横切） |
| **实现文件** | 代码仓库相对路径 + 关键类名 | 多文件用换行分隔 |
| **测试文件** | `MetBench_SystemMT.Tests/` 下相对路径 | 单元 + BDD + UAT |
| **测试结果** | `dotnet test MetBench_SystemMT.Tests` 最近一次基线 | `pass/skip/fail` 或缺口说明 |

**基线**：2026-05-23（Stage 8 S8-P5c + 5-angle review 3 commits 修复后），`dotnet test MetBench_SystemMT.Tests` = **876 pass / 0 fail**（OpenMC 跨程序场景在无 OpenMC 环境下首跑 skip / 二跑 warm 后 0 skip）。基线累计：848 - 6 (mutmut) - 13 (Trend) + 6 (G-02) + 2×4 (S8-P1..P4) + 4 (S8-P5a) + 9 (S8-P5b) + 7 (S8-P5c) + 5 (review-fix-1：Tolerance/EquationKey/MapProgram) + 7 (enum pinning) + 1 (DeleteMr binding guard) = 876。MR 库 17 / 8 方程；V3 5D-tag schema 三层 + 数据链修复（Importer 写 EquationKey + Migration 通过 MRBinding lookup SUT）。

## 1. T0 · 核心 —— 系统级 MT 流程

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T0-01 | CLAUDE.md §2 T0；AGENTS Stage 1 | System-MT pipeline：源输入→变换→执行→断言 | `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`<br>`Pipeline/PipelineContext.cs`<br>`Pipeline/PipelineOutcome.cs`<br>`Pipeline/PipelineStatus.cs` | `V2Pipeline/SystemMtPipelineTests.cs`<br>`Steps/SystemMtPipelineV2Steps.cs` | ✅ pass |
| F-T0-02 | CLAUDE.md §6；AGENTS Stage 6 P4 | Launcher facade（`ISystemMtLauncher` 单一入口） | `SystemMT/Launcher/SystemMtLauncher.cs`<br>`Launcher/ISystemMtLauncher.cs`<br>`Launcher/MrSummary.cs` / `MrRunResult.cs`<br>`Launcher/BatchMrRunRequest.cs` / `BatchProgress.cs` | `Launcher/SystemMtLauncherTests.cs`<br>`Launcher/SystemMtLauncherBatchTests.cs`<br>`Launcher/LauncherEndToEndOdeTests.cs` | ✅ pass |
| F-T0-03 | AGENTS Stage 6 P1/P2；CLAUDE.md §6 | LiteDB 持久化（系统级独立 DB） | `MetBench_DAL/LiteDbSystemMtResultRepository.cs`<br>`SystemMT/Persistence/ISystemMtResultRepository.cs`<br>`Persistence/SystemMtResultRecord.cs` | `Persistence/LiteDbSystemMtResultRepositoryTests.cs`<br>`V2Schema/V2EntityRoundtripTests.cs` | ✅ pass |
| F-T0-04 | AGENTS Stage 6 P4 | 执行记录 + Replay | `SystemMT/Pipeline/SystemMtExecutionRecorder.cs`<br>`Pipeline/ReplayService.cs`<br>`Pipeline/ReplayContextBuilder.cs` | `V2Pipeline/SystemMtExecutionRecorderTests.cs`<br>`V2Pipeline/ReplayServiceTests.cs`<br>`V2Pipeline/ReplayContextBuilderTests.cs` | ✅ pass |
| F-T0-05 | AGENTS Stage 1 acceptance | BDD steps（Reqnroll）执行 MR 场景 | `MetBench_SystemMT.Tests/Features/*.feature`（HeatEquation / OpenMocPinCell / SystemLevelCliMt / ProjectileRange / CrossProgram / SystemLevelGeneratedFollowup）<br>`Steps/*.cs`（同名 step bindings） | （同列实现文件） | ✅ pass |

## 2. T1 · 直接支撑

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T1-01 | CLAUDE.md §2 T1；AGENTS Stage 1 | CLI runner：SUT 进程调用 + 超时 + 退出码 + 工作目录 | `SystemMT/CliProgramRunner.cs`<br>`Pipeline/IProcessExecutor.cs`<br>`Pipeline/DefaultProcessExecutor.cs`<br>`SystemMT/CliRunResult.cs` | `SystemMT/CliProgramRunnerTests.cs`<br>`V2Pipeline/DefaultProcessExecutorSmokeTests.cs` | ✅ pass |
| F-T1-02 | CLAUDE.md §2 T1；AGENTS Stage 3 | I/O 文件适配（Python adapter） | `SystemMT/PythonInputAdapter.cs`<br>`SystemMT/PythonOutputAdapter.cs`<br>`SystemMT/InputCaseReader.cs` / `InputGenerator.cs` / `InputSamplePoint.cs`<br>SUT/openmoc/openmc/heat_equation/projectile/ 下的 `*_input_parser.py` / `*_output_parser.py` | `SystemMT/PythonInputAdapterTests.cs`<br>`SystemMT/PythonOutputAdapterTests.cs`<br>`SystemMT/OpenMocInputAdapterTests.cs` / `OpenMocOutputAdapterTests.cs` / `OpenMocSigmaAInputAdapterTests.cs`<br>`SystemMT/OpenMcInputAdapterTests.cs` / `OpenMcOutputAdapterTests.cs`<br>`SystemMT/HeatEquationInputAdapterTests.cs` / `HeatEquationOutputAdapterTests.cs`<br>`SystemMT/DampedOscillatorParserTests.cs` / `DecayChainParserTests.cs` / `LotkaVolterraParserTests.cs`<br>`SystemMT/InputCaseReaderTests.cs` / `InputGeneratorTests.cs` | ✅ pass |
| F-T1-03 | CLAUDE.md §2 T1；AGENTS Stage 7 W12 | 同源异构差分测试（OpenMOC × OpenMC） | `Features/CrossProgramNeutronTransportMrs.feature`<br>`Steps/CrossProgramSteps.cs` | （同列） | ✅ pass（OpenMC 缺失时 3 场景 skip） |
| F-T1-04 | CLAUDE.md §2 T1；AGENTS Stage 6 P5 | CRUD（程序 / 方程 / MR / 算例 / 测试过程；含 method-level MR CRUD，G-06 补齐） | `SystemMT/Catalog/SystemMtCatalogService.cs`<br>`Catalog/MethodMtCatalogService.cs`（CRUD: Create/Get/Find/List/Update/Delete + Kind 强制 + MetaPatternCode 拒绝）<br>`Metadata/EquationMetadata.cs` / `MrMetadata.cs` / `SystemMtMetadataCatalog.cs` / `EquationFunctionRecipe.cs` / `EquationFunctionDescriptor.cs`<br>`Metadata/ISystemMtMetadataRepository.cs`<br>`MetBench_DAL/LiteDbSystemMtMetadataRepository.cs` | `Catalog/SystemMtCatalogServiceTests.cs`<br>`Catalog/P3CatalogExtensionTests.cs`<br>`Metadata/LiteDbSystemMtMetadataRepositoryTests.cs`<br>`Metadata/SystemMtMetadataCatalogTests.cs`<br>`MethodMT/MethodMtCatalogCrudTests.cs`（9 测试） | ✅ pass |
| F-T1-05 | CLAUDE.md §2 T1；AGENTS Stage 4 | WPF 客户端（操作入口 + 页面导航） | `MetBench_Client/` 全部（`net8.0-windows7.0`） | ⚠ **无云端测试**（WPF SDK Linux 不可编译） | ☐ **缺口**：UAT runbook 走 Windows 手动验证（`docs/uat/runbooks/windows-uat-round-1.md`） |
| F-T1-06 | AGENTS Stage 6 P5 | Feature ↔ DB 同步工具与迁移 | `SystemMT/Launcher/LauncherCatalogV2Importer.cs` | `Launcher/LauncherCatalogV2ImporterTests.cs`<br>`V2Schema/V2SoftDeleteAndMigrationTests.cs` / `V2DbConfigRegistrationTests.cs` / `V2IndexConstraintTests.cs` / `V2RepositoryDIBindingTests.cs` / `V1CompatibilityTests.cs` | ✅ pass |

## 3. T2 · 可视化与报表

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T2-01 | AGENTS Stage 4 acceptance；CLAUDE.md §6 | HTML 报告渲染器 | `SystemMT/Reporting/HtmlSystemMtResultReportRenderer.cs`<br>`Reporting/ISystemMtResultReportRenderer.cs` | `Reporting/HtmlSystemMtResultReportRendererTests.cs` | ✅ pass |
| F-T2-02 | AGENTS Stage 6 P8 | 5-scope 报告生成（Word / Excel / PDF） | `MetBench_BLL.Core/Reporting/SystemMtReportService.cs`<br>`MetBench_BLL/` 下的 Word/Excel/PDF 生成器（cross-platform 部分） | `V2Reporting/SystemMtReportServiceTests.cs` | ✅ pass |
| F-T2-03 | CLAUDE.md §3 表注 | 跨平台 LiveCharts 数据层（`MTVisualizationService`） | `MetBench_BLL/MTVisualizationService.cs`<br>+ 支撑类 `CsvDataReader.cs` / `ColumnDefinition.cs` / `PlotType.cs` / `Visualization/SeriesBuilder.cs` | `Bll/MtVisualizationServiceTests.cs`（6 测试） | ✅ pass |

## 4. T3 · 覆盖（代表性方程 × 程序类型）

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T3-01 | AGENTS Stage 6 P8 | CoverageService 4 维报告 | `MetBench_BLL.Core/Coverage/CoverageService.cs`<br>`Coverage/CoverageReport.cs` | `V2Coverage/CoverageServiceTests.cs`<br>`V2Coverage/FakeCoverageRepositories.cs` | ✅ pass |
| F-T3-02 | AGENTS Stage 8 | 代表性 SUT 接入（共 9 SUT / 8 方程：decay_chain / damped_oscillator / lotka_volterra / heat_equation / projectile / openmoc / openmc / subchannel_1d / **diffusion_1d**；**全部进入 launcher catalog + metadata catalog**）。**S8-P1..P4（2026-05-23）扩 MR 库**：S8-P1 Bateman 2 MR；S8-P2 Fourier 2 MR；S8-P3 1D subchannel SUT + navier-stokes + 2 MR；S8-P4 1D diffusion SUT + diffusion 方程 + 2 MR（`diffusion-source-linearity` m_mono + `diffusion-mesh-richardson` m_conv）。共 17 MR / 8 方程 | `SUT/decay_chain/`（含 `bateman` 方程实现）<br>`SUT/damped_oscillator/`<br>`SUT/lotka_volterra/`<br>`SUT/heat_equation/`<br>`SUT/projectile/`（+ `sample/standard.txt`）<br>`SUT/openmoc/`<br>`SUT/openmc/` | （上述各 SUT 的 Parser / Adapter / Smoke / Sample 测试，见 F-T1-02）<br>+ `Launcher/SystemMtLauncherTests.ListAvailableAsync_{projectile,bateman_mass_conservation,bateman_timestep_cauchy}_descriptor_has_expected_metadata` | ✅ pass |
| F-T3-03 | `docs/t3-program-selection.md` | 反应堆物理 5 方程锚定（boltzmann / diffusion / bateman / fourier / NS） | bateman: `Equations/Bateman/BatemanAnalyticSolution.cs`（L2）<br>boltzmann: 通过 OpenMOC/OpenMC SUT（无独立 L2）<br>fourier: 通过 heat_equation SUT<br>diffusion / NS: **未落地** | bateman: `SystemMT/Equations/BatemanP4Tests.cs` | ⚠ **缺口**：diffusion + NS 方程的 L2 / SUT 未落地 |

## 5. T4 · MR 识别

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T4-01 | CLAUDE.md §2 T4；AGENTS Stage 6 P7 | IMRDiscoverer 框架 + 三技术路线（meta-prompt / LLM-native / SCG-heuristic） | `MetBench_BLL.Core/Discovery/IMRDiscoverer.cs`<br>`Discovery/MetaPatternDiscoverer.cs`<br>`Discovery/LlmNativeDiscoverer.cs`<br>`Discovery/ScgHeuristicDiscoverer.cs`<br>`Discovery/DiscoveryService.cs`<br>`Discovery/CandidateMrProposal.cs` / `MetaPatternSeed.cs` / `DiscoveryMethodSeed.cs` / `MrFeatureGenerator.cs` / `JsonFileScgGraphBuilder.cs` / `RuleBasedScgPatternMiner.cs` | `V2Discovery/DiscoveryServiceTests.cs`<br>`V2Discovery/DiscovererParsingTests.cs`<br>`V2Discovery/MetaPatternDiscovererIntegrationTests.cs`<br>`V2Discovery/ScgHeuristicDiscovererTests.cs`<br>`V2Discovery/JsonFileScgGraphBuilderTests.cs`<br>`V2Discovery/MrFeatureGeneratorTests.cs`<br>`V2Discovery/DiscoveryMethodSeedTests.cs` | ✅ pass |
| F-T4-02 | AGENTS Stage 6 P7 | ValidationService + 3 Validator（Empirical / Theoretical-LLM / Adversarial-Mutmut） | `Discovery/ValidationService.cs`<br>`Discovery/Validators/EmpiricalValidator.cs` / `EmpiricalRepoSampler.cs`<br>`Discovery/Validators/TheoreticalLlmValidator.cs`<br>`Discovery/Validators/AdversarialMutmutValidator.cs` / `AdversarialCampaignSampler.cs`<br>`Discovery/Validators/IMRValidator.cs`<br>`Discovery/MrSchemaValidationService.cs`<br>`Discovery/NullLlmGateway.cs` / `OpenAiCompatibleLlmGateway.cs` / `ILlmGateway.cs` | `V2Discovery/ValidationServiceTests.cs`<br>`V2Discovery/ValidatorTests.cs`<br>`V2Discovery/RealSamplerTests.cs`<br>`V2Discovery/MrSchemaValidationServiceTests.cs`<br>`V2Discovery/OpenAiCompatibleLlmGatewayTests.cs` | ✅ pass |
| F-T4-03 | AGENTS Stage 7 W11.2 | Multi-LLM 共识（DeepSeek + OpenAI + Claude） | `Discovery/MultiLlmConsensusValidator.cs` | `V2Discovery/MultiLlmConsensusValidatorTests.cs`<br>`Experiments/MultiLlmRealExperiment.cs`（env-gated） | ✅ pass（live run env-gated） |
| F-T4-04 | AGENTS Stage 8 | MR pairing（"程序集 × MR 集"配对） | `Discovery/MRPairingService.cs` | `V2Discovery/MRPairingServiceTests.cs` | ✅ pass |

## 6. T5 · 异常

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T5-01 | AGENTS Stage 6 P6 | AnomalyService + CommonalityReport | `MetBench_BLL.Core/SystemMT/Anomaly/AnomalyService.cs`<br>`Anomaly/IAnomalyService.cs` / `AnomalyFilter.cs` / `CommonalityReport.cs` | `V2Anomaly/AnomalyServiceTests.cs`<br>`V2Anomaly/AnomalyCreationOnFailureTests.cs` | ✅ pass |
| F-T5-02 | AGENTS Stage 8（PR #83） | AnomalyClassifier + severity / category 分级 | `Anomaly/AnomalyClassifier.cs`<br>`Anomaly/AnomalySeverityThresholds.cs` | `V2Anomaly/AnomalyClassifierTests.cs` | ✅ pass |

## 7. T6 · 变异

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T6-01 | AGENTS Stage 6 P7；CLAUDE.md §2 T6 | MutationCampaignService（campaign matrix + 杀死率 / 存活率） | `MetBench_BLL.Core/Mutation/MutationCampaignService.cs`<br>`Mutation/MutationCampaignSpec.cs` / `MutationCampaignSummary.cs` | `V2Mutation/MutationCampaignServiceTests.cs`<br>`V2Mutation/FakeMutationRepositories.cs` | ✅ pass |
| F-T6-02 | CLAUDE.md §2 T6 backlog | 语义变异 + 等价变异体识别 + 最小 MR 完备子集 | **未落地** | **无** | ☐ **缺口**：Stage 8 backlog（CLAUDE.md §2 / AGENTS Stage 8 "主线之外待完善"） |

## 8. F-MR-* · MR 协议层 + 方程函数容器（横切，P0–P7）

> 详见 `docs/superpowers/plans/2026-05-23-mr-architecture-implementation-plan.md`。本节为本表与该计划的双向索引。

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-MR-P0 | mr-architecture.md §8 序列 1 | Schema 入位（`EquationKey` / `ValueShape` + Recipe 实体 + IDAL + LiteDb） | `MetBench_Domain/MetamorphicRelation.cs`（+EquationKey/ValueShape）<br>`MetBench_BLL.Core/SystemMT/Metadata/EquationFunctionRecipe.cs`<br>`Metadata/EquationFunctionDescriptor.cs`<br>`Metadata/ISystemMtMetadataRepository.cs`（+Recipe CRUD）<br>`MetBench_DAL/LiteDbSystemMtMetadataRepository.cs` | `SystemMT/Metadata/MrArchitectureSchemaP0Tests.cs` | ✅ 13/13 pass |
| F-MR-P1 | mr-architecture.md §5 L0 列表 | 17 个 L0 数学基元注册 | `SystemMT/Transformations/Math/{MathUnary, MathBinary, MathLinComb, MathPow, MathAggregate, Linspace, MathTransformHelper}.cs`<br>`Transformations/TransformationRegistry.cs`（17 entries） | `SystemMT/Transformations/MathPrimitivesTests.cs`<br>`V2Transformations/IMRTransformationTests.cs` | ✅ 74/74 pass |
| F-MR-P2 | mr-architecture.md §5 §7 | IEquationFunction + Registry + Recipe 执行器 + Resolver（决策 B） | `MetBench_BLL.Core/Equations/IEquationFunction.cs`<br>`Equations/EquationFunctionRegistry.cs`<br>`Equations/RecipeBasedEquationFunction.cs`<br>`Equations/TransformationResolver.cs`<br>`Equations/UnknownTransformationException.cs` | `SystemMT/Equations/EquationFunctionP2Tests.cs` | ✅ 18/18 pass |
| F-MR-P3 | mr-architecture.md §8 序列 3 | Catalog 扩展（系统级 Recipe CRUD + 方法级 catalog） | `SystemMT/Catalog/SystemMtCatalogService.cs`（+ Recipe CRUD + 校验链）<br>`Catalog/MethodMtCatalogService.cs`（Kind=method-level 强制 + MetaPattern 拒绝） | `SystemMT/Catalog/P3CatalogExtensionTests.cs` | ✅ 11/11 pass |
| F-MR-P4 | mr-architecture.md §8 序列 4 | Bateman 样板：L2 解析解 + L1 Recipe + launcher 端到端 | `Equations/Bateman/BatemanAnalyticSolution.cs`<br>`SystemMT/Pipeline/SystemMtPipeline.cs`（用 EquationFunctionRegistry + TransformationResolver）<br>`Pipeline/PipelineContext.cs`（+EquationKey / +EquationFunctionRegistry） | `SystemMT/Equations/BatemanP4Tests.cs` | ✅ 7/7 pass |
| F-MR-P5 | mr-architecture.md §8 序列 5 | method 侧对称（MethodTransformationRegistry + MethodAssertionEvaluator） | `MetBench_BLL/MethodMT/Transformations/MethodTransformationRegistry.cs`<br>`MethodMT/Assertions/MethodAssertionEvaluator.cs` | `MethodMT/MethodMtRegistryP5Tests.cs` | ✅ 19/19 pass |
| F-MR-P6 | mr-architecture.md §8 序列 6 | BDD steps 全量切 W2 pipeline | `Steps/HeatEquationAmplitudeSteps.cs`<br>`Steps/OpenMocPinCellNuSigmaFSteps.cs`<br>`Steps/OpenMocPinCellSigmaASteps.cs`<br>`Steps/CrossProgramSteps.cs`<br>`Steps/SystemLevelCliMtSteps.cs`<br>`Steps/SystemLevelGeneratedFollowupSteps.cs`<br>`Steps/ProjectileRangeSteps.cs`<br>+ TestAssets/SUT 下新增 `example_cli_input_parser.py` / `example_cli_output_parser.py` / `projectile_input_parser.py` / `projectile_output_parser.py` | （同列 step 文件本身即 BDD 测试） | ✅ 全 BDD 场景 pass |
| F-MR-P7 | mr-architecture.md §8 序列 7 | legacy LaTeX→SymPy 路径标 `[Obsolete]` + 文档同步 | `MetBench_BLL/Latextosympy.cs`（`[Obsolete]`）<br>`MetBench_BLL/Latextosympy_Await.cs`（`[Obsolete]`）<br>`AGENTS.md`（指针）<br>`docs/design/mr-architecture.md` §8（✅ 标注） | ⚠ 无新增测试（per phase 验收：仅 grep + 文档同步） | ✅ grep 命中 + 文档同步 |

## 9. 横切基础设施（不归入 T0–T6，但本表登记以便受控）

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-INFRA-01 | AGENTS Stage 6 P1 | DbConfig + 23 collections 实体 | `MetBench_Domain/*`<br>`MetBench_DAL/*`<br>DbConfig 初始化逻辑 | `V2Schema/DbConfigTests.cs`<br>`V2Schema/MetaPatternEntityTests.cs`<br>`V2Schema/MRBindingStatusTests.cs`<br>`V2Schema/V2EntityRoundtripTests.cs`<br>`V2Schema/V2IndexConstraintTests.cs`<br>`V2Schema/V2RepositoryDIBindingTests.cs`<br>`V2Schema/V2SoftDeleteAndMigrationTests.cs`<br>`V2Schema/V2DbConfigRegistrationTests.cs`<br>`V2Schema/V1CompatibilityTests.cs`<br>`V2Schema/UatRound1BugFixTests.cs` | ✅ pass |
| F-INFRA-02 | AGENTS Stage 7 W12 | Keyset pagination（分页） | `MetBench_BLL.Core/Paging/{PageRequest, PagedResult, PagingViewModel}.cs` | `Paging/PagingTests.cs`<br>`Paging/PagingViewModelTests.cs`<br>`V2Pagination/KeysetPaginationTests.cs` | ✅ pass |
| ~~F-INFRA-03~~ | AGENTS Stage 6 P8 | ~~Trend 分析 + 多维突发检测~~ | **已删除（次轮 P0 / 档 2.A.2，commit `88e757d`）** — Trend / Weekly / WoW / Burst 子系统整体下线（与项目当前科研主线正交，CLAUDE.md §2.1 指明对接成熟工具而非自研） | — | ~~~~ 已删除 |
| F-INFRA-04 | AGENTS Stage 8 P-A | ApproxEqual 等式断言 + EqualityThresholds | `SystemMT/ApproxEqualAssertion.cs`<br>`SystemMT/EqualityThresholds.cs`<br>`SystemMT/Assertions/*.cs`<br>`SystemMT/GreaterThanAssertion.cs` / `LessThanAssertion.cs` / `IMrAssertion.cs` | `SystemMT/ApproxEqualAssertionTests.cs`<br>`SystemMT/GreaterThanAssertionTests.cs`<br>`SystemMT/LessThanAssertionTests.cs`<br>`V2Pipeline/AssertionEvaluatorTests.cs`<br>`V2Pipeline/AssertionExtensionsTests.cs` | ✅ pass |
| F-INFRA-05 | AGENTS Stage 8 P-C | 方程 / MR 元信息 catalog + 漂移守卫 | `SystemMT/Metadata/{EquationMetadata, MrMetadata, SystemMtMetadataCatalog}.cs`<br>`MetBench_DAL/LiteDbSystemMtMetadataRepository.cs` | `SystemMT/Metadata/SystemMtMetadataCatalogTests.cs`<br>`SystemMT/Metadata/LiteDbSystemMtMetadataRepositoryTests.cs` | ✅ pass |
| F-INFRA-06 | AGENTS Stage 8 P-B | 样本点级输入配对 | `SystemMT/InputSamplePoint.cs`<br>`SystemMT/InputCaseReader.cs`<br>`Persistence/SystemMtResultRecord.cs`（+InputSamples） | `SystemMT/InputCaseReaderTests.cs` | ✅ pass |
| F-INFRA-07 | AGENTS Stage 8 PR #77 | R-Case 复现 | `MetBench_BLL.Core/RCaseRepro/{RCaseReproductionService, Report, Spec}.cs` | `V2RCaseRepro/RCaseReproductionServiceTests.cs` | ✅ pass |
| F-INFRA-08 | AGENTS Stage 6 P3 | FieldPathResolver（JsonPointer / Namelist / McnpCard） | `SystemMT/ParameterMapping/FieldPathResolverFactory.cs`<br>`ParameterMapping/IFieldPathResolver.cs`<br>`ParameterMapping/{JsonPointerResolver, NamelistKeyResolver, McnpCardResolver}.cs` | `V2Transformations/FieldPathResolverTests.cs` | ✅ pass |
| F-INFRA-09 | AGENTS Stage 1 | ApplicationService + 冷启动集成 | `MetBench_BLL/ApplicationService.cs` 等 | `Bll/ApplicationServiceTests.cs`<br>`ColdStart/ColdStartIntegrationTests.cs` | ✅ pass |
| F-INFRA-10 | AGENTS Stage 7 W12 | UAT 双轨（21 BDD wrapper + 4 cloud-covered cross-ref） | `MetBench_SystemMT.Tests/Features/Uat/UC-*.feature`<br>`Steps/UatRubricSteps.cs`<br>`docs/uat/test-procedures.md` / `acceptance-rubric.md` / `runbooks/windows-uat-round-1.md` | `Features/Uat/UC-C*.feature.cs` / `UC-F*.feature.cs` / `UC-G*.feature.cs`（共 21 个）<br>`Steps/UatRubricSteps.cs` | ✅ pass |

## 10. 缺口清单（gap report）

> 本节是当前**唯一**待办池。新增功能编号或修复缺口必须在此登记，未登记不得动代码。

| 缺口编号 | 关联功能 | 缺口描述 | 影响范围 | 处置建议 |
|---|---|---|---|---|
| G-01 | F-T1-05（WPF 客户端） | 云端 CI 不能编译 WPF（`net8.0-windows7.0`），完全无自动测试覆盖 | WPF 页面行为只能 Windows 手动验证 | 维持现状（CLAUDE.md §3 已硬约束）；UAT runbook 已覆盖 |
| ~~G-02~~ ✅ 已完成(2026-05-23) | F-T2-03（LiveCharts 数据层） | ~~MTVisualizationService 跨平台部分无独立单测~~ | — | 新建 `Bll/MtVisualizationServiceTests.cs`（6 测试覆盖 Line/Scatter/Pie/未初始化/非法 PlotType/重复 Initialize） |
| ~~G-03~~ ✅ 已完成(2026-05-23) | F-T3-03（反应堆 5 方程锚定） | ~~diffusion + Navier-Stokes 两条 L2 解析解 / SUT 未落地~~ | T3 覆盖完成 | S8-P3 落 navier-stokes（1D subchannel SUT + 2 MR）；S8-P4 落 diffusion（1D FD SUT + 2 MR）。5 方程全覆盖（boltzmann / bateman / fourier / diffusion / navier-stokes） |
| G-04 | F-T6-02（语义变异 + 等价识别 + 最小 MR 子集） | 完全未实现 | Stage 8 变异模块增强未启动 | CLAUDE.md §2 / AGENTS Stage 8 "主线之外"已列为 backlog |
| ~~G-05~~ ✅ 已完成(2026-05-23) | F-MR-P7 | ~~LaTeX→SymPy `[Obsolete]` 后无 grep 守卫单测~~ | — | 已建 `Architecture/ObsoleteAttributeGuardTests.cs` 覆盖 `Latextosympy` + `Latextosympy_Await` + `SystemMtRunner`（5 测试） |
| ~~G-06~~ ✅ 已完成(2026-05-23) | F-T1-04 / F-MR-P5 | ~~method MT 协议层未接入业务路径~~ | — | 已建 `IMtPipeline<TReq,TOut>` 共享抽象（BLL.Core/MT）+ `MethodMtPipeline`（BLL/MethodMT，实现协议层）+ `MethodMtRunRequest/Outcome` 数据 record + `MethodMtCatalogService` 扩 CRUD（Get/Update/Delete）+ `SystemMtPipeline` 加 IMtPipeline 显式接口实现；20 新测试（7 pipeline + 4 Bateman 参数化 AAA + 9 CRUD）；全量回归 810→830 pass。注：4 处 `Latextosympy*` 调用已澄清属 v1 展示衍生字段（不在 G-06 范围），归 G-11 处置 |
| ~~G-07~~ ✅ 已完成(全部) | F-T0-02 / F-T0-01 | ~~W1 引擎残留~~ | — | 云端：`SystemMtRunner` 加 `[Obsolete]`；VM 端（G-07b）：`App.xaml.cs:130` DI 注册已删除（commit `dcf978a`）。深度审计发现完全删 W1 涉及 cross-cutting 重构，属 Stage 9 范围 |
| ~~G-08~~ ✅ 已完成(全部) | F-T1-04 / F-T0-03 | ~~catalog 双 seed 不自动同步~~ | — | 云端：`SystemMtBootstrap.SeedCatalogsAsync` helper + 4 测试；VM 端（G-08b）：`App.xaml.cs` 注册 `ISystemMtMetadataRepository` + `LauncherCatalogV2Importer`，`OnStartup` 调 bootstrap（commit `13b3447`）。完整 source-of-truth 收口属 Stage 9+ 重构 |
| ~~G-09~~ ✅ 已完成(2026-05-23) | F-T3-02 / F-T1-04 | ~~projectile SUT 未进 launcher catalog~~ | — | 已补 `projectile-motion` EquationMetadata + `projectile-scale-v0` MrMetadata + MrBlueprint + `SUT/projectile/sample/standard.txt`；2 个 launcher 测试 + cascade 4 个 importer 测试更新；全量 809→810 pass |
| ~~G-10~~ ✅ 已完成(2026-05-23) | F-T1-04 | ~~CRUD 不全~~ | — | (a) `ISystemMtMetadataRepository` 加 3 个 DeleteAsync（Equation / MR / Recipe）+ LiteDb 实现 + Fake repo 实现；(b) `SystemMtCatalogService` 加 `UpdateEquationFunctionAsync` / `DeleteEquationFunctionAsync`；(c) `MethodMtCatalogService` MR-CRUD 子集已在 G-06 落地。9 新测试。**剩余开口**：MR / Application binding 的 Delete cascade 语义（非本次范围） |
| G-X1-Adv ✅ 已完成(2026-05-24) | F-T1-05（WPF 客户端） | PR #88 删 `CandidateReviewViewModel.UseAdversarial`，XAML CheckBox 残留 binding error | WPF 云端不可编译，binding 错误在 VM 启动后才可见 | 删除 `CandidateReviewPage.xaml` 中 adversarial-mutmut CheckBox 元素（commit `254c167`） |
| G-X2-LatexGuard ✅ 已完成(2026-05-24) | F-T1-04（v1 兼容守卫） | G-11 裁决配套守护测试：grep 断言 4 处 `Latextosympy*` 调用仅存在于指定 v1 兼容路径，新增调用即失败 | 新 MR 误用 LaTeX 老路径无感知 | 新建 `Architecture/LegacyPathBoundaryTests.cs`（2 测试，878 pass，commit `1479962`） |
| G-11 ⚖ 已裁决(2026-05-23)：保留至 Stage 9 | F-T1-04（v1 兼容） | **v1 LaTeX 展示衍生字段路径**：`MetamorphicRelationService.Add/Update` + `AutoMRParser.ProduceMRs/Async` + `MRRecommendationViewModel` + `MRManagementViewModel` 共 4 处调 `Latextosympy*`（已 `[Obsolete]`）。与 method MT 执行栈（G-06）完全正交，不影响新功能 | v1 UI 展示完整，`ObsoleteAttributeGuardTests` 守卫防止新增调用 | **裁决(a)：保留为 v1 兼容**，不做额外修改。**Stage 9 清理义务**：届时须删除 `Latextosympy` / `Latextosympy_Await` 类、4 处调用、LiteDB 中的 SymPy 文本 + PNG 衍生字段，并迁移已有 MR 记录。此决策由用户于 2026-05-23 确认 |
| G-12 | F-T1-04（远期 PBT 升级） | **method MT 升级到 property-based testing**：当前走 AAA + catalog-driven validator（G-06），未引入 FsCheck / Hedgehog 等 PBT 框架。PBT 与 MT 范式天然契合（property over many inputs + shrinking） | 当前 SUT 是解析解，无 bug 可找；MR 数个位数；PBT generator 工程量大于当前 MR 验证工程量 | 触发条件：method MR 数 ≥ 20 跨多方程 ∥ 接入有 bug 风险的真实 C# SUT。届时 catalog schema 加 input domain 字段，AAA 测试保留为基线、新增 PBT validator 作为第二层 |
| G-13 | F-T3-02（远期 PBT 升级） | **system MT 在轻量 SUT 上叠加 PBT 做模糊测试**：当前 system MT 走 BDD `.feature`，OpenMOC/OpenMC 单 case 时长（30s / 5min）禁止 PBT；但 projectile / damped-oscillator / lotka-volterra / decay-chain 单 case < 1s，PBT 可行 | BDD 的领域沟通价值不可替代（OpenMOC/OpenMC 永远不走 PBT）；轻量 SUT 是 PBT 的合适载体 | 触发条件：轻量 SUT 的 BDD 稳定 + input generator 工程量预算允许。覆盖范围严格限制为 < 1s 单 case 的 ODE SUT |
| G-X3-CatalogConvergence | F-T0-02 / F-T1-04 / F-T0-03 | **System-MT catalog 双事实源 + 硬编码 launcher 收敛**：当前 `SystemMtLauncher.BuildBlueprints()` 私有方法硬编码全部 17 MR × 9 SUT 蓝图，与 `LiteDbSystemMtMetadataRepository`（catalog 元信息）+ `MetamorphicRelationV3` LiteDB 表（PR #88 V3 5D-tag schema）三者并存，**修改任一处都需手工同步另两处**；同时执行记录 `SystemMtResultRecord` 只持久化 summary 级（无样本点级 evidence、无 V3 IdV3 反向链接） | T0 执行路径无单一事实源；Stage 8 主线 MR 库扩张 → 蓝图体积失控；V3 schema landed 但 unwired（未进 pipeline 写入路径） | 收敛方案见 [`docs/superpowers/specs/2026-05-24-systemmt-catalog-convergence-design.md`](superpowers/specs/2026-05-24-systemmt-catalog-convergence-design.md) v3 / 实施 [`docs/superpowers/plans/2026-05-24-systemmt-catalog-convergence-plan.md`](superpowers/plans/2026-05-24-systemmt-catalog-convergence-plan.md) v2（8 任务，3 PR：PR-A 蓝图→Provider，PR-B 样本级 evidence + V3 写入，PR-C 删除 hardcoded + 文档同步） |

## 11. 与 P0–P7 对应的快速索引（执行历史）

| Commit | F-MR 编号 | 备注 |
|---|---|---|
| `4c85b6c` | F-MR-P0 | schema 入位 |
| `16de54b` | F-MR-P1 | 17 L0 基元 |
| `a004ac4` | F-MR-P2 | 方程函数 + Resolver |
| `f05de05` | F-MR-P3 | Catalog 扩展 |
| `cc779a9` | F-MR-P4 | Bateman 样板 |
| `80c2317` | F-MR-P5 | method 侧对称 |
| `3991c52` | F-MR-P6 | BDD steps 切 W2 |
| `17a6093` | F-MR-P7 | legacy Obsolete |
| `266485e` | （文档） | plan 表中 commit hash 回填 |
| `5856bd8` | （文档） | requirements.md 追溯矩阵初版 |
| `8259093` | （文档） | §10 追加 G-06..G-10 |
| `e0cceea` | **G-09** | projectile SUT 接入 launcher + metadata catalog（810 pass） |
| `75df630` | （文档） | §11 索引补登 |
| `b047668` | （文档） | §10 追加 G-11/G-12/G-13（v1 清理 + PBT 升级占位） |
| `7305230` | **G-06** | method MT 执行栈：IMtPipeline 共享抽象 + MethodMtPipeline + Catalog CRUD（830 pass） |
| `47cb96b` | **G-08** | catalog 自动 bootstrap helper（云端范围；834 pass） |
| `2c56b8a` | **G-07 + G-05** | `SystemMtRunner` 加 [Obsolete] + ObsoleteAttributeGuardTests 守卫（839 pass） |
| `4fc7f15` | **G-10** | ISystemMtMetadataRepository Delete + Recipe Update/Delete（848 pass） |
| `dcf978a` | **G-07b** | App.xaml.cs 删除 SystemMtRunner DI 注册（VM 端；848 pass） |
| `13b3447` | **G-08b** | App.xaml.cs 接入 SystemMtBootstrap：注册 metadata repo + importer，OnStartup seed（VM 端） |
| `71665f3` | **档 2.A.1** | next-stage P0：删 AdversarialMutmutValidator + AdversarialCampaignSampler（842 pass）|
| `88e757d` | **档 2.A.2** | next-stage P0：删 MetBench_BLL.Trend 子系统（829 pass）|
| `8c7ddd5` | **G-02 / 档 2.C** | MTVisualizationService 数据层 6 单测（835 pass）|
| `01fcba3` | **S8-P1** | Bateman MR 库扩展：`bateman-mass-conservation` (m_inv) + `bateman-timestep-cauchy` (m_conv) + importer 元模式识别扩展（837 pass）|
| `0d119f3` | **S8-P2** | Fourier MR 库扩展：`fourier-timestep-convergence` (m_conv) + `fourier-alpha-monotonic` (m_mono)（839 pass）|
| `f3bd535` | **S8-P3** | 1D subchannel SUT 接入 + navier-stokes 方程：2 新 MR（841 pass，G-03 部分闭合）|
| `bb06ae5` | **S8-P4** | 1D diffusion SUT 接入 + diffusion 方程：2 新 MR（843 pass，**G-03 完全闭合**，5 方程全覆盖）|
| `7bbb746` | **S8-P5a** | V3 5D-tag schema entity + 7 enum + round-trip 测试（847 pass）|
| `85aae2e` | **S8-P5b** | V3 IDAL + LiteDB repo + CRUD/5D 维度过滤 9 测试（856 pass）|
| `5fc6f15` | **S8-P5c** | V2→V3 MR 投影 migration + 7 测试（V2 字段映射到 5D enum + RigorClass 启发式，863 pass）|
| `60f9910` | **review-fix-1** | critical 数据链 + Tolerance hard-code 修复（5 新测试，868 pass）|
| `b8fdd85` | **review-fix-2** | cleanup misses — README/AGENTS/Report doc + UAT rubric/procedures + smokeshot Trends（doc-only，868 pass）|
| `44a5d1b` | **review-fix-3** | medium：DeleteMr binding guard + enum int 锁定 + SUT divide-by-zero/edge guard（+8 测试，876 pass）|
| `254c167` | **G-X1-Adv** | CandidateReviewPage.xaml 删除 UseAdversarial CheckBox（VM 端 binding error 消除） |
| `1479962` | **G-X2-LatexGuard** | LegacyPathBoundaryTests：v1 LaTeX 调用边界守卫（2 测试，878 pass） |
| _(pending)_ | **G-X3 docs** | Catalog convergence spec v3 + plan v2 + §10/§11/AGENTS 指针（doc-only，无测试基线变化）|
| `290b927` | **G-X3 Task 1** | Catalog definition models + IMrCatalogProvider boundary (878→884, then amended 884→919 after 15-finding self-review) |
| `953da7b` | **G-X3 Task 2a** | refactor: extract LegacyCatalogFactory from SystemMtLauncher (919 → 919, behavior-preserving) |
| `e5aade8` | **G-X3 Task 2b** | HardcodedMrCatalogProvider + 8 smoke tests (919 → 927) |
| `9f74d69` | **G-X3 Task 2c** | ManifestMrCatalogProvider + 9 catalog.json + 12 manifest tests + 2 parity tests (927 → 943) |
| `c923063` | **G-X3 Task 3** | Inject IMrCatalogProvider into SystemMtLauncher; MrCatalogEntry 8 → 13 fields; ToBlueprint inverse; 3 injection tests (943 → 946) |
| _(pending)_ | **G-X3 Task 4** | [Obsolete] on HardcodedMrCatalogProvider + sunset guard (+2 tests, 946 → 948) |
| `2005909` | **G-X3 Task 5** | Execution evidence models + V3MrIdRef + repo contract (4 model tests, 948 → 952) |
| `5f9d27d` | **G-X3 Task 6 step 1** | LiteDb evidence repository + roundtrip tests (+7 tests, 952 → 959) |
| _(pending)_ | **G-X3 Task 6 step 2** | SystemMtExecutionRecorder write-through evidence + V3 lookup (+6 tests, 959 → 965) |

## 12. 受控开发模式工作流

1. 任何新工作 → 先在本表 §10 缺口清单登记 / 或定位到既有 `F-Tn-xx`。
2. 改动前在对话中报告：
   - 涉及哪个功能编号；
   - 会修改哪些文件；
   - 会新增哪些测试；
   - 不会修改哪些无关模块。
3. 用户确认后，按 TDD 走：红测试 → 实现 → 绿 → 提交 → 推送 → 回写本表。
4. 没有测试对应的实现不计入完成；本表对应行的"测试结果"列必须显示 ✅ pass 才能标完成。
5. 多功能并行 → 拒绝。一次只走一个编号。

---

**更新规则**：每次完成一个功能编号，更新对应行的"测试结果"列 + §10 缺口清单 + §11 commit 索引（如属 F-MR 系列）。本文件本身的修改也走受控流程，但不需要新增测试（属文档元数据）。
