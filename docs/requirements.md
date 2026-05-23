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

**基线**：2026-05-23（G-06 后），`dotnet test MetBench_SystemMT.Tests` = **830 pass / 0 fail**（OpenMC 跨程序场景在无 OpenMC 环境下首跑 skip / 二跑 warm 后 0 skip）。

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
| F-T2-03 | CLAUDE.md §3 表注 | 跨平台 LiveCharts 数据层（`MTVisualizationSerive`） | `MetBench_BLL/MTVisualizationSerive.cs`（无 WPF 依赖部分） | ⚠ 无独立单测（图形组件 plotter 已迁 `MetBench_Client/Services/Plotting/`） | ☐ **缺口**：数据层测试未建 |

## 4. T3 · 覆盖（代表性方程 × 程序类型）

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T3-01 | AGENTS Stage 6 P8 | CoverageService 4 维报告 | `MetBench_BLL.Core/Coverage/CoverageService.cs`<br>`Coverage/CoverageReport.cs` | `V2Coverage/CoverageServiceTests.cs`<br>`V2Coverage/FakeCoverageRepositories.cs` | ✅ pass |
| F-T3-02 | AGENTS Stage 8 | 代表性 SUT 接入（已落地：decay_chain / damped_oscillator / lotka_volterra / heat_equation / projectile / openmoc / openmc；**全部进入 launcher catalog + metadata catalog**，2026-05-23 G-09） | `SUT/decay_chain/`<br>`SUT/damped_oscillator/`<br>`SUT/lotka_volterra/`<br>`SUT/heat_equation/`<br>`SUT/projectile/`（+ `sample/standard.txt`）<br>`SUT/openmoc/`<br>`SUT/openmc/` | （上述各 SUT 的 Parser / Adapter / Smoke / Sample 测试，见 F-T1-02）<br>+ `Launcher/SystemMtLauncherTests.ListAvailableAsync_projectile_descriptor_has_expected_metadata` | ✅ pass |
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
| F-INFRA-03 | AGENTS Stage 6 P8 | Trend 分析 + 多维突发检测 | `MetBench_BLL.Core/Trend/{TrendAnalysisService, WeeklyReport}.cs` | `V2Trend/TrendAnalysisServiceTests.cs`<br>`V2Trend/MultiDimBurstDetectionTests.cs`<br>`V2Trend/FakeTrendRepositories.cs` | ✅ pass |
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
| G-02 | F-T2-03（LiveCharts 数据层） | `MTVisualizationSerive` 跨平台部分无独立单测 | 数据形态修改无回归保护 | 视后续 P 编号补一组数据形态单测 |
| G-03 | F-T3-03（反应堆 5 方程锚定） | diffusion + Navier-Stokes 两条 L2 解析解 / SUT 未落地 | T3 覆盖目标未达成 | 留待 Stage 8 后续 plan；不在 P0–P7 范围 |
| G-04 | F-T6-02（语义变异 + 等价识别 + 最小 MR 子集） | 完全未实现 | Stage 8 变异模块增强未启动 | CLAUDE.md §2 / AGENTS Stage 8 "主线之外"已列为 backlog |
| G-05 | F-MR-P7 | LaTeX→SymPy `[Obsolete]` 后无 grep 守卫单测，回归仅靠人工 | 未来若有人去 `[Obsolete]` 易漏 | 视需要在 `V2Schema/` 加一条 ObsoleteAttributeGuard 测试 |
| ~~G-06~~ ✅ 已完成(2026-05-23) | F-T1-04 / F-MR-P5 | ~~method MT 协议层未接入业务路径~~ | — | 已建 `IMtPipeline<TReq,TOut>` 共享抽象（BLL.Core/MT）+ `MethodMtPipeline`（BLL/MethodMT，实现协议层）+ `MethodMtRunRequest/Outcome` 数据 record + `MethodMtCatalogService` 扩 CRUD（Get/Update/Delete）+ `SystemMtPipeline` 加 IMtPipeline 显式接口实现；20 新测试（7 pipeline + 4 Bateman 参数化 AAA + 9 CRUD）；全量回归 810→830 pass。注：4 处 `Latextosympy*` 调用已澄清属 v1 展示衍生字段（不在 G-06 范围），归 G-11 处置 |
| G-07 | F-T0-02 / F-T0-01 | **W1 引擎残留**：`SystemMtRunner` + `SystemMtTask` 仍由 `MetBench_Client/App.xaml.cs:130` DI 注册；测试 `SystemMtRunnerTests` / `SystemMtRunnerGeneratedFollowupTests` 在跑 | system 侧双轨未拆 | 拆 DI 注册 + 删 W1 测试 + 删 `SystemMtRunner` / `SystemMtTask` / `SystemMtCase` / `SystemMtResult` |
| G-08 | F-T1-04 / F-T0-03 | **catalog 双 seed 不自动同步**：launcher 硬编码 8 `MrBlueprint` + `SystemMtMetadataCatalog` 静态 5 方程 8 MR + entity 表三处；`LauncherCatalogV2Importer` 不在启动路径自动跑 | 任何新 MR 需改 2-3 处；entity 表可能为空 | 启动时自动 Import + 单一 source-of-truth 收口（推荐 metadata catalog 主，launcher 派生） |
| ~~G-09~~ ✅ 已完成(2026-05-23) | F-T3-02 / F-T1-04 | ~~projectile SUT 未进 launcher catalog~~ | — | 已补 `projectile-motion` EquationMetadata + `projectile-scale-v0` MrMetadata + MrBlueprint + `SUT/projectile/sample/standard.txt`；2 个 launcher 测试 + cascade 4 个 importer 测试更新；全量 809→810 pass |
| G-10 | F-T1-04 | **CRUD 不全**：`EquationFunctionRecipe` 缺 Update/Delete；元信息层 (`ISystemMtMetadataRepository`) 全无 Delete；`MethodMtCatalogService` 公开方法 0 个（疑似仅构造时拒绝 MetaPattern） | 元信息一旦写入只能 Upsert 覆盖；无清理路径 | 视 UI/UAT 需要再补；目前 backlog（注：G-06 将补 `MethodMtCatalogService` MR-CRUD 子集） |
| G-11 | F-T1-04（v1 兼容） | **v1 LaTeX 展示衍生字段路径清理**：`MetamorphicRelationService.Add/Update` + `AutoMRParser.ProduceMRs/Async` + `MRRecommendationViewModel` + `MRManagementViewModel` 共 4 处仍调 `Latextosympy*`（已 `[Obsolete]`）。`mr-architecture.md §4.1` 明确"LaTeX 仅展示、不驱动执行"，但 v1 数据形态下衍生字段仍存于 LiteDB | 与 method MT 执行栈正交，归 v1 数据展示路径迁移 | VM 端工作（云端不可编译 WPF）；待 method MT 执行栈（G-06）稳定后裁决：保留为 v1 兼容 / 拆迁至独立展示服务 / 直接删 |
| G-12 | F-T1-04（远期 PBT 升级） | **method MT 升级到 property-based testing**：当前走 AAA + catalog-driven validator（G-06），未引入 FsCheck / Hedgehog 等 PBT 框架。PBT 与 MT 范式天然契合（property over many inputs + shrinking） | 当前 SUT 是解析解，无 bug 可找；MR 数个位数；PBT generator 工程量大于当前 MR 验证工程量 | 触发条件：method MR 数 ≥ 20 跨多方程 ∥ 接入有 bug 风险的真实 C# SUT。届时 catalog schema 加 input domain 字段，AAA 测试保留为基线、新增 PBT validator 作为第二层 |
| G-13 | F-T3-02（远期 PBT 升级） | **system MT 在轻量 SUT 上叠加 PBT 做模糊测试**：当前 system MT 走 BDD `.feature`，OpenMOC/OpenMC 单 case 时长（30s / 5min）禁止 PBT；但 projectile / damped-oscillator / lotka-volterra / decay-chain 单 case < 1s，PBT 可行 | BDD 的领域沟通价值不可替代（OpenMOC/OpenMC 永远不走 PBT）；轻量 SUT 是 PBT 的合适载体 | 触发条件：轻量 SUT 的 BDD 稳定 + input generator 工程量预算允许。覆盖范围严格限制为 < 1s 单 case 的 ODE SUT |

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
| `<TBD>` | **G-06** | method MT 执行栈：IMtPipeline 共享抽象 + MethodMtPipeline + Catalog CRUD（830 pass） |

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
