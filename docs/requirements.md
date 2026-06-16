# MetBench 需求-功能-代码-测试 追溯矩阵

> **受控开发模式（2026-05-23 启用）**：本表是 MetBench 当前**唯一**的需求-代码-测试映射事实源。
> 一切新增 / 修改须先在本表登记功能编号（`F-Tn-xx` / `F-MR-Pn`），未登记禁止新增代码。
> 每次只处理一个功能编号；改动前需说明：(1) 涉及编号、(2) 修改文件、(3) 新增测试、(4) 不动模块。
> 没有测试对应的功能不算完成；没有功能编号对应的代码不允许随意新增。
> 当前主线头、测试绿基线、活跃计划和开放风险以 [`docs/status/current.md`](status/current.md) 为唯一状态账本；本表只负责需求-实现-测试追溯投影。

## 0. 编号规则与文档边界

| 维度 | 来源 | 说明 |
|---|---|---|
| **需求** | `CLAUDE.md` §2（T0–T6 核心功能模型）+ `AGENTS.md` Stage 1–8 路线图 + `docs/superpowers/plans/` 单次实施计划 | 三层指针，互不复制 |
| **功能编号** | 本表（唯一） | 形如 `F-T0-01`（按 T 分层）或 `F-MR-Pn`（MR 协议层横切） |
| **实现文件** | 代码仓库相对路径 + 关键类名 | 多文件用换行分隔 |
| **测试文件** | `MetBench_SystemMT.Tests/` 下相对路径 | 单元 + BDD + UAT |
| **测试结果** | `dotnet test MetBench_SystemMT.Tests` 最近一次基线 | `pass/skip/fail` 或缺口说明 |

**基线**：当前共享、可审计代码状态以 [`docs/status/current.md`](status/current.md) §2 为准；本追溯矩阵不复制 live `origin/main` SHA、测试总数或 runtime inventory，以免与状态账本漂移。Release-readiness、client i18n、runtime-governance、Windows VM evidence 和当前 catalog inventory 均由状态账本 §2-§3 解释。历史参考基线仅保留为追溯锚点：`e839214`（PR #110，1043 / 0 / 0）、`373bb59`（2026-05-24，961 / 0 / 8 / 969）与 `763e067`（PR #93，965 / 0 / 0）。

## 1. T0 · 核心 —— 系统级 MT 流程

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T0-01 | CLAUDE.md §2 T0；AGENTS Stage 1 | System-MT pipeline：源输入→变换→执行→断言 | `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`<br>`Pipeline/PipelineContext.cs`<br>`Pipeline/PipelineOutcome.cs`<br>`Pipeline/PipelineStatus.cs` | `V2Pipeline/SystemMtPipelineTests.cs`<br>`Steps/SystemMtPipelineV2Steps.cs` | ✅ pass |
| F-T0-02 | CLAUDE.md §6；AGENTS Stage 6 P4 | Launcher facade（`ISystemMtLauncher` 单一入口） | `SystemMT/Launcher/SystemMtLauncher.cs`<br>`Launcher/ISystemMtLauncher.cs`<br>`Launcher/MrSummary.cs` / `MrRunResult.cs`<br>`Launcher/BatchMrRunRequest.cs` / `BatchProgress.cs` | `Launcher/SystemMtLauncherTests.cs`<br>`Launcher/SystemMtLauncherBatchTests.cs`<br>`Launcher/LauncherEndToEndOdeTests.cs` | ✅ pass |
| F-T0-03 | AGENTS Stage 6 P1/P2；CLAUDE.md §6 | LiteDB 持久化（系统级独立 DB） | `MetBench_DAL/LiteDbSystemMtResultRepository.cs`<br>`SystemMT/Persistence/ISystemMtResultRepository.cs`<br>`Persistence/SystemMtResultRecord.cs` | `Persistence/LiteDbSystemMtResultRepositoryTests.cs`<br>`V2Schema/V2EntityRoundtripTests.cs` | ✅ pass |
| F-T0-04 | AGENTS Stage 6 P4 | 执行记录 + Replay | `SystemMT/Pipeline/SystemMtExecutionRecorder.cs`<br>`Pipeline/ReplayService.cs`<br>`Pipeline/ReplayContextBuilder.cs` | `V2Pipeline/SystemMtExecutionRecorderTests.cs`<br>`V2Pipeline/ReplayServiceTests.cs`<br>`V2Pipeline/ReplayContextBuilderTests.cs` | ✅ pass |
| F-T0-05 | AGENTS Stage 1 acceptance | BDD steps（Reqnroll）执行 MR 场景 | `MetBench_SystemMT.Tests/Features/*.feature`（HeatEquation / OpenMocPinCell / SystemLevelCliMt / ProjectileRange / CrossProgram / SystemLevelGeneratedFollowup）<br>`Steps/*.cs`（同名 step bindings） | （同列实现文件） | ✅ pass |
| F-T0-06 | CLAUDE.md §2 T0；`docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-release-closure-design.md`（PR-1 #298） | 异步作业执行：单条 MR 与批量 MR 走 submit→poll→cancel 异步 job，终态显式（Succeeded/Failed/Cancelled）。批量保留逐条 MR 明细；MR 断言失败=检出异常（仍 Succeeded，归 T5），仅基础设施故障置 Failed（设计 §7）。同步 launcher API 保留为内部兼容路径。 | `SystemMT/Jobs/SystemMtJobService.cs`（`SubmitOperationAsync`）<br>`Jobs/SystemMtJobWorker.cs` / `SystemMtAsyncPipeline.cs`<br>`Jobs/Operations/SystemMtJobKind.cs` / `SystemMtOperationJobRequest.cs` / `SystemMtJobOperationDispatcher.cs`<br>`Jobs/Operations/RunBatchJobOperationHandler.cs` / `SystemMtBatchJobItem.cs` | `SystemMT/Jobs/SystemMtJobServiceTests.cs`<br>`Jobs/RunBatchJobOperationHandlerTests.cs`<br>`Jobs/SystemMtJobWorkerTests.cs`<br>`Jobs/JobFacadeTypeLeakageTests.cs` | ✅ pass（云端 focused 116/116）；Windows VM：RunMr / RunBatch 异步终态 Succeeded（`...vm-evidence/vm-summary.md`） |

## 2. T1 · 直接支撑

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T1-01 | CLAUDE.md §2 T1；AGENTS Stage 1 | CLI runner：SUT 进程调用 + 超时 + 退出码 + 工作目录 | `SystemMT/CliProgramRunner.cs`<br>`Pipeline/IProcessExecutor.cs`<br>`Pipeline/DefaultProcessExecutor.cs`<br>`SystemMT/CliRunResult.cs` | `SystemMT/CliProgramRunnerTests.cs`<br>`V2Pipeline/DefaultProcessExecutorSmokeTests.cs` | ✅ pass |
| F-T1-02 | CLAUDE.md §2 T1；AGENTS Stage 3；PR-A #162 扩展非 JSON wire format | I/O 文件适配（Python adapter + 非 JSON wire format helper） | `SystemMT/PythonInputAdapter.cs`<br>`SystemMT/PythonOutputAdapter.cs`<br>`SystemMT/InputCaseReader.cs` / `InputGenerator.cs` / `InputSamplePoint.cs`<br>SUT/openmoc/openmc/heat_equation/projectile/ 下的 `*_input_parser.py` / `*_output_parser.py`<br>`SUT/_shared/metbench_io/` (PR-A pure-stdlib csv-row + plain-text helper) | `SystemMT/PythonInputAdapterTests.cs`<br>`SystemMT/PythonOutputAdapterTests.cs`<br>`SystemMT/OpenMocInputAdapterTests.cs` / `OpenMocOutputAdapterTests.cs` / `OpenMocSigmaAInputAdapterTests.cs`<br>`SystemMT/OpenMcInputAdapterTests.cs` / `OpenMcOutputAdapterTests.cs`<br>`SystemMT/HeatEquationInputAdapterTests.cs` / `HeatEquationOutputAdapterTests.cs`<br>`SystemMT/DampedOscillatorParserTests.cs` / `DecayChainParserTests.cs` / `LotkaVolterraParserTests.cs`<br>`SystemMT/InputCaseReaderTests.cs` / `InputGeneratorTests.cs`<br>`SystemMT/Shared/MetBenchIoHelperTests.cs` (PR-A, 11 facts)<br>`SystemMT/Launcher/LauncherEndToEndTestCsvTests.cs` (PR-A, 端到端 csv-row 经未改 launcher) | ✅ pass |
| F-T1-03 | CLAUDE.md §2 T1；AGENTS Stage 7 W12；PR-B #161 抽象 differential runner | 同源异构差分测试（OpenMOC × OpenMC 直接走 BDD；任意 SUT 对走 IDifferentialTestRunner cloud API） | `Features/CrossProgramNeutronTransportMrs.feature`<br>`Steps/CrossProgramSteps.cs`<br>`MetBench_BLL.Core/SystemMT/Differential/IDifferentialTestRunner.cs` 与 `DifferentialTestRunner.cs` (PR-B, 三种 deterministic agreement criteria：BothPassed / DirectionConcordant / FollowUpRatioWithinTolerance；9 种显式 disagreement reason；纯函数 IO-free 仅经两次 launcher 调用) | （BDD 同列）<br>`SystemMT/Differential/DifferentialTestRunnerTests.cs` (PR-B, 28 facts after Theory expansion) | ✅ pass（OpenMC 缺失时 BDD 3 场景 skip；differential runner 自身不依赖 OpenMC） |
| F-T1-04 | CLAUDE.md §2 T1；AGENTS Stage 6 P5 | CRUD（程序 / 方程 / MR / 算例 / 测试过程；含 method-level MR CRUD，G-06 补齐） | `SystemMT/Catalog/SystemMtCatalogService.cs`<br>`Catalog/MethodMtCatalogService.cs`（CRUD: Create/Get/Find/List/Update/Delete + Kind 强制 + MetaPatternCode 拒绝）<br>`Metadata/EquationMetadata.cs` / `MrMetadata.cs` / `SystemMtMetadataCatalog.cs` / `EquationFunctionRecipe.cs` / `EquationFunctionDescriptor.cs`<br>`Metadata/ISystemMtMetadataRepository.cs`<br>`MetBench_DAL/LiteDbSystemMtMetadataRepository.cs` | `Catalog/SystemMtCatalogServiceTests.cs`<br>`Catalog/P3CatalogExtensionTests.cs`<br>`Metadata/LiteDbSystemMtMetadataRepositoryTests.cs`<br>`Metadata/SystemMtMetadataCatalogTests.cs`<br>`MethodMT/MethodMtCatalogCrudTests.cs`（9 测试） | ✅ pass |
| F-T1-05 | CLAUDE.md §2 T1；AGENTS Stage 4 | WPF 客户端（操作入口 + 页面导航 + T0-T5 用户入口） | `MetBench_Client/` 全部（`net8.0-windows7.0`）<br>`tools/smokeshot/`（Windows UIA / PrintWindow 截图证据） | `MetBench_Client.Tests/ClientI18n/*.cs`（Windows-only WPF tests）<br>`docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/`<br>`docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/` | ✅ Windows VM evidence：WPF build 0 errors；T0-T5 screenshot matrix 21/21 PASS；ClientI18n 3/3 PASS；base i18n UIA 9/9 PASS；full-page bilingual screenshots captured。⚠ WPF / legacy warning debt 仍存在，不能把 0 errors 解读为 warnings 清零 |
| F-T1-06 | AGENTS Stage 6 P5 | Feature ↔ DB 同步工具与迁移 | `SystemMT/Launcher/LauncherCatalogV2Importer.cs` | `Launcher/LauncherCatalogV2ImporterTests.cs`<br>`V2Schema/V2SoftDeleteAndMigrationTests.cs` / `V2DbConfigRegistrationTests.cs` / `V2IndexConstraintTests.cs` / `V2RepositoryDIBindingTests.cs` / `V1CompatibilityTests.cs` | ✅ pass |
| F-T1-07 | CLAUDE.md §2 T1；async closure design §5.3（PR-2 #302） | 异步资产包导入/导出：SUT/MR/样例/变异体单 SUT import-unit 包走异步 job。导入校验通过后在确定性 staging root 写出真实 staged artifact（`staging-manifest.json` + `sut-import-unit.json`），非"仅校验"；导出前先校验再写。校验失败→显式 Failed job。结果/证据导入显式排除。 | `SystemMT/Jobs/Operations/ImportAssetsJobOperationHandler.cs` / `ExportAssetsJobOperationHandler.cs`<br>`ImportExport/Put/SutImportStagingService.cs` / `SutImportStagingManifest.cs`<br>（复用 `SutImportValidator.cs` / `SutImportPackageExporter.cs`） | `SystemMT/Jobs/AssetImportExportJobTests.cs`<br>`ImportExport/AGroupPutImportExportTests.cs` / `BGroupPutImportExportTests.cs`<br>`Architecture/ExecutionArtifactImportBoundaryTests.cs`（无导入路径守卫） | ✅ pass；Windows VM：ImportAssets / ExportAssets 异步终态 Succeeded + artifact 路径（`...vm-evidence/`） |

## 3. T2 · 可视化与报表

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T2-01 | AGENTS Stage 4 acceptance；CLAUDE.md §6 | HTML 报告渲染器 | `SystemMT/Reporting/HtmlSystemMtResultReportRenderer.cs`<br>`Reporting/ISystemMtResultReportRenderer.cs` | `Reporting/HtmlSystemMtResultReportRendererTests.cs` | ✅ pass |
| F-T2-02 | AGENTS Stage 6 P8 | 5-scope 报告生成（Word / Excel / PDF） | `MetBench_BLL.Core/Reporting/SystemMtReportService.cs`<br>`MetBench_BLL/` 下的 Word/Excel/PDF 生成器（cross-platform 部分） | `V2Reporting/SystemMtReportServiceTests.cs` | ✅ pass |
| F-T2-03 | CLAUDE.md §3 表注 | 跨平台 LiveCharts 数据层（`MTVisualizationService`） | `MetBench_BLL/MTVisualizationService.cs`<br>+ 支撑类 `CsvDataReader.cs` / `ColumnDefinition.cs` / `PlotType.cs` / `Visualization/SeriesBuilder.cs` | `Bll/MtVisualizationServiceTests.cs`（6 测试） | ✅ pass |
| F-T2-04 | CLAUDE.md §2 T2；async closure design §5.4（PR-3 #300）；gap-fill A1 #308 / A3 #310 / C1 #313 | 异步执行结果/证据/报告导出（单向）：按 executionId 走异步 job 导出 bundle = `manifest.json` + `execution-result.json` + `execution-evidence.json`（仅当证据存在）+ HTML 报告；**A1 加 Word/Excel/PDF（`report.docx/xlsx/pdf`，按注入的渲染器能力输出，某格式被请求但渲染器缺失即 fail-closed）**；Markdown 报告在缺 `SystemMtReportService` 时 fail-closed。**A3 加 `ExportReport` 作业（report-only：仅报告文件，`IncludeResultJson:false`，无 result/evidence json）**。**C1 加批量导出（请求带 `ExecutionIds` 列表 → 每 execution 导出到 `<id>/` 子目录 + 顶层 `batch-manifest.json`，continue-on-error；全成功 Succeeded 否则 Failed 报失败计数）**。缺失 execution → 显式 Failed。结果/证据**导入**显式排除（待信任模型）。 | `SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExporter.cs`（+`IWord/IExcel/IPdf` 可选渲染器 + `HasWord/Excel/PdfRenderer`）/ `ExecutionArtifactExportRequest.cs`（+`IncludeWord/Excel/Pdf/ResultJson`）/ `ExecutionArtifactBatchExporter.cs` + `ExecutionArtifactBatchManifest.cs`<br>`Jobs/Operations/ExportExecutionArtifactsJobOperationHandler.cs`（批量分支）/ `ExportReportJobOperationHandler.cs` | `ImportExport/ExecutionArtifactExporterTests.cs`（四端导出 + fail-closed Theory）<br>`Jobs/ExecutionArtifactExportJobTests.cs` / `Jobs/ExportReportJobTests.cs` / `Jobs/ExecutionArtifactBatchExportJobTests.cs`<br>`Architecture/ExecutionArtifactImportBoundaryTests.cs`（无导入路径守卫） | ✅ pass（云端全量 1800/0/19）；**Windows VM 实测（#317，`docs/superpowers/specs/2026-06-06-t0-t2-gap-fill-vm-evidence/`）**：运行 WPF 里 ExportExecutionArtifacts 产出真实 `report.docx/xlsx/pdf` + result/evidence json，ExportReport 为 report-only（docx/xlsx/pdf 在、result/evidence json 不在），均 Succeeded；WPF build 0 errors、focused 29/29、UIA driver exit 0。批量（C1）UI 暴露为可选后续增强（云端能力已具并测）。 |

## 4. T3 · 覆盖（代表性方程 × 程序类型）

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T3-01 | AGENTS Stage 6 P8 | CoverageService 4 维报告 | `MetBench_BLL.Core/Coverage/CoverageService.cs`<br>`Coverage/CoverageReport.cs` | `V2Coverage/CoverageServiceTests.cs`<br>`V2Coverage/FakeCoverageRepositories.cs` | ✅ pass |
| F-T3-02 | AGENTS Stage 8；T3 Poisson (PR #134)；T3 Advection (PR #136)；T3 Wave (PR #138)；T3 Burgers (PR #140)；T3C-IVP scipy-ivp-lotka-volterra (2026-05-26)；T3C-BVP scipy-bvp-poisson-1d (2026-05-26)；Minimum-MR-SubSet A/B runtime promotion (2026-06-02..04) | 代表性 SUT 接入（当前 runtime catalog provider inventory：**21 SUT / 17 equations / 38 MRs**）。核心真实物理 / external-solver SUT 仍包括 decay_chain / damped_oscillator / lotka_volterra / heat_equation / projectile / openmoc / openmc / subchannel_1d / diffusion_1d / poisson_1d / advection_1d / wave_1d / burgers_1d / scipy_ivp_lotka_volterra / scipy_bvp_poisson_1d，并全部进入 launcher catalog + metadata catalog。Minimum-MR-SubSet A/B group 另加入 5 个受控 staged-import runtime slices：`minimum_mr_subset_p3` / `p4` / `p5` / `p8` / explicit `p9` surrogate；P9 是 deterministic OpenMC surrogate，不声明真实 OpenMC execution。**S8-P1..P4（2026-05-23）扩 MR 库**：S8-P1 Bateman 2 MR；S8-P2 Fourier 2 MR；S8-P3 1D subchannel SUT + navier-stokes + 2 MR；S8-P4 1D diffusion SUT + diffusion 方程 + 2 MR。**PR #134 / #136 / #138 / #140** 覆盖 Poisson / Advection / Wave / Burgers 四类 PDE。**T3C-IVP / T3C-BVP** 打通 SciPy external-solver pilot。**Minimum-MR-SubSet A/B group** 打通 import-only staging → live runtime promotion → async job pipeline 的受控增量路径；外部 P3/P8 NumPy/SciPy smoke 仍不作为 MetBench runtime 完成证据。 | `SUT/decay_chain/` (`bateman`)<br>`SUT/damped_oscillator/`<br>`SUT/lotka_volterra/`<br>`SUT/heat_equation/`<br>`SUT/projectile/`<br>`SUT/openmoc/`<br>`SUT/openmc/`<br>`SUT/subchannel_1d/`<br>`SUT/diffusion_1d/`<br>`SUT/poisson_1d/`<br>`SUT/advection_1d/`<br>`SUT/wave_1d/`<br>`SUT/burgers_1d/`<br>`SUT/scipy_ivp_lotka_volterra/`（SciPy `solve_ivp` 自适应 RK45）<br>`SUT/scipy_bvp_poisson_1d/`（SciPy `solve_bvp` 自适应 BVP）<br>`SUT/minimum_mr_subset_p3/`<br>`SUT/minimum_mr_subset_p4/`<br>`SUT/minimum_mr_subset_p5/`<br>`SUT/minimum_mr_subset_p8/`<br>`SUT/minimum_mr_subset_p9/` | 上述各 SUT 的 Parser / Adapter / Smoke / Sample 测试见 F-T1-02；<br>+ `Launcher/SystemMtLauncherTests.ListAvailableAsync_*_descriptor_has_expected_metadata`（各 MR 一例）；<br>+ `Launcher/LauncherEndToEndPoissonTests`<br>+ `Launcher/LauncherEndToEndAdvectionTests`<br>+ `Launcher/LauncherEndToEndWaveTests`<br>+ `Launcher/LauncherEndToEndBurgersTests`<br>+ `Launcher/LauncherEndToEndScipyIvpLotkaVolterraTests`（SciPy IVP, `[SkippableFact]` 缺 SciPy 时干净 skip）<br>+ `Launcher/LauncherEndToEndScipyBvpPoissonTests`（SciPy BVP, `[SkippableFact]` 缺 SciPy 时干净 skip）<br>+ `Launcher/LauncherEndToEndOdeTests`<br>+ `SystemMT/ScipyIvpLotkaVolterraParserTests`<br>+ `SystemMT/ScipyBvpPoissonParserTests`<br>+ `LauncherEndToEndMinimumMrSubsetAGroupTests` / Minimum-MR-SubSet B-group launcher + async job tests（见 status ledger §3 evidence rows） | ✅ pass（外部 P3/P8 NumPy/SciPy smoke 不声明完成） |
| F-T3-03 | `docs/t3-program-selection.md` | 反应堆物理 5 方程锚定（boltzmann / diffusion / bateman / fourier / NS） | bateman: `Equations/Bateman/BatemanAnalyticSolution.cs`（L2）<br>boltzmann: 通过 OpenMOC/OpenMC SUT（无独立 L2）<br>fourier: 通过 heat_equation SUT<br>diffusion: 通过 `SUT/diffusion_1d/`<br>NS: 通过 `SUT/subchannel_1d/` | bateman: `SystemMT/Equations/BatemanP4Tests.cs`<br>diffusion / NS 相关 launcher / parser / smoke tests 见 F-T1-02 / F-T3-02 | ✅ pass（5 方程锚定已闭合；非每个方程都要求独立 L2 实现） |

## 5. T4 · MR 识别

| 编号 | 需求来源 | 功能描述 | 实现文件 | 测试文件 | 测试结果 |
|---|---|---|---|---|---|
| F-T4-01 | CLAUDE.md §2 T4；AGENTS Stage 6 P7 | IMRDiscoverer 框架 + 三技术路线（meta-prompt / LLM-native / SCG-heuristic） | `MetBench_BLL.Core/Discovery/IMRDiscoverer.cs`<br>`Discovery/MetaPatternDiscoverer.cs`<br>`Discovery/LlmNativeDiscoverer.cs`<br>`Discovery/ScgHeuristicDiscoverer.cs`<br>`Discovery/DiscoveryService.cs`<br>`Discovery/CandidateMrProposal.cs` / `MetaPatternSeed.cs` / `DiscoveryMethodSeed.cs` / `MrFeatureGenerator.cs` / `JsonFileScgGraphBuilder.cs` / `RuleBasedScgPatternMiner.cs` | `V2Discovery/DiscoveryServiceTests.cs`<br>`V2Discovery/DiscovererParsingTests.cs`<br>`V2Discovery/MetaPatternDiscovererIntegrationTests.cs`<br>`V2Discovery/ScgHeuristicDiscovererTests.cs`<br>`V2Discovery/JsonFileScgGraphBuilderTests.cs`<br>`V2Discovery/MrFeatureGeneratorTests.cs`<br>`V2Discovery/DiscoveryMethodSeedTests.cs` | ✅ pass |
| F-T4-02 | AGENTS Stage 6 P7 | ValidationService + 当前保留 validator 组合（Empirical / Theoretical-LLM / Multi-LLM 共识） | `Discovery/ValidationService.cs`<br>`Discovery/Validators/EmpiricalValidator.cs` / `EmpiricalRepoSampler.cs`<br>`Discovery/Validators/TheoreticalLlmValidator.cs`<br>`Discovery/Validators/IMRValidator.cs`<br>`Discovery/MultiLlmConsensusValidator.cs`<br>`Discovery/MrSchemaValidationService.cs`<br>`Discovery/NullLlmGateway.cs` / `OpenAiCompatibleLlmGateway.cs` / `ILlmGateway.cs` | `V2Discovery/ValidationServiceTests.cs`<br>`V2Discovery/ValidatorTests.cs`<br>`V2Discovery/RealSamplerTests.cs`<br>`V2Discovery/MrSchemaValidationServiceTests.cs`<br>`V2Discovery/OpenAiCompatibleLlmGatewayTests.cs`<br>`V2Discovery/MultiLlmConsensusValidatorTests.cs` | ✅ pass |
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
| F-T6-02 | CLAUDE.md §2 T6 backlog | 语义变异 + 等价变异体识别 + 最小 MR 完备子集 | **未落地**；本轮仅登记后续边界 | **无**；需独立 T6 plan + tests | ☐ **缺口**：Stage 8 backlog（CLAUDE.md §2 / AGENTS Stage 8 "主线之外待完善"）；后续边界见 `docs/superpowers/plans/2026-06-16-quality-follow-up-plan.md` |

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
| F-INFRA-04 | AGENTS Stage 8 P-A → 验证语义收敛 PR-C/D（#118/#119） | 标量等式 / 比较断言：现统一走 Typed Semantic Catalog `BinaryComparisonKernel`（Less/Greater/Equal）与 `ScaledEqualityKernel`，由 `LegacyAssertionPredicateMapper` 把 `less` / `greater` / `approx` 字符串映射到 typed predicate；W1 接口 `IMrAssertion` 与 `ApproxEqualAssertion` / `GreaterThanAssertion` / `LessThanAssertion` / `EqualityThresholds` 已删除 | `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/{BinaryComparisonKernel, ScaledEqualityKernel, DeterministicScalarToleranceEvaluator}.cs`<br>`Catalog/Typed/Migration/{LegacyAssertionPredicateMapper, TypedSpecFactory, TypedVerificationContextFactory}.cs`<br>`SystemMT/Assertions/{AssertionEvaluator, AssertionInput, AssertionTolerance, AssertionTypeCodes, SystemMtAssertionResultV2}.cs`（`AssertionEvaluator` 保留为非生产 helper，`AssertionTypeCodes` 仅供 `Catalog/MrBindingDefinition.cs` 校验绑定字符串） | `SystemMT/Catalog/Typed/{BinaryComparisonKernelTests, ScaledEqualityKernelTests, LegacyAssertionPredicateMapperTests, SystemMtPipelineTypedRuntimeContractTests}.cs`<br>`V2Pipeline/AssertionEvaluatorTests.cs`<br>`V2Pipeline/AssertionExtensionsTests.cs`<br>`Architecture/SemanticCatalogBoundaryTests.cs`（守卫 IMrAssertion / AssertionEvaluator / string dispatch 生产侧回潮） | ✅ pass |
| F-INFRA-05 | AGENTS Stage 8 P-C | 方程 / MR 元信息 catalog + 漂移守卫 | `SystemMT/Metadata/{EquationMetadata, MrMetadata, SystemMtMetadataCatalog}.cs`<br>`MetBench_DAL/LiteDbSystemMtMetadataRepository.cs` | `SystemMT/Metadata/SystemMtMetadataCatalogTests.cs`<br>`SystemMT/Metadata/LiteDbSystemMtMetadataRepositoryTests.cs` | ✅ pass |
| F-INFRA-06 | AGENTS Stage 8 P-B | 样本点级输入配对 | `SystemMT/InputSamplePoint.cs`<br>`SystemMT/InputCaseReader.cs`<br>`Persistence/SystemMtResultRecord.cs`（+InputSamples） | `SystemMT/InputCaseReaderTests.cs` | ✅ pass |
| F-INFRA-07 | AGENTS Stage 8 PR #77 | R-Case 复现 | `MetBench_BLL.Core/RCaseRepro/{RCaseReproductionService, Report, Spec}.cs` | `V2RCaseRepro/RCaseReproductionServiceTests.cs` | ✅ pass |
| F-INFRA-08 | AGENTS Stage 6 P3 | FieldPathResolver（JsonPointer / Namelist / McnpCard） | `SystemMT/ParameterMapping/FieldPathResolverFactory.cs`<br>`ParameterMapping/IFieldPathResolver.cs`<br>`ParameterMapping/{JsonPointerResolver, NamelistKeyResolver, McnpCardResolver}.cs` | `V2Transformations/FieldPathResolverTests.cs` | ✅ pass |
| F-INFRA-09 | AGENTS Stage 1 | ApplicationService + 冷启动集成 | `MetBench_BLL/ApplicationService.cs` 等 | `Bll/ApplicationServiceTests.cs`<br>`ColdStart/ColdStartIntegrationTests.cs` | ✅ pass |
| F-INFRA-10 | AGENTS Stage 7 W12 | UAT 双轨（21 BDD wrapper + 4 cloud-covered cross-ref） | `MetBench_SystemMT.Tests/Features/Uat/UC-*.feature`<br>`Steps/UatRubricSteps.cs`<br>`docs/uat/test-procedures.md` / `acceptance-rubric.md` / `runbooks/windows-uat-round-1.md` | `Features/Uat/UC-C*.feature.cs` / `UC-F*.feature.cs` / `UC-G*.feature.cs`（共 21 个）<br>`Steps/UatRubricSteps.cs` | ✅ pass |
| F-INFRA-11 | AGENTS Stage 8；v1.2 PR #97 / #99；PR-B 重命名（#115） | **MR 验证统一设计 v1.2 基础层**：YAML / typed catalog foundation + anti-legacy lint + typed semantic model + fail-closed validator。PR-B 已把 `V12Catalog` 目录与命名空间永久重命名为 `Catalog/Typed`，类名前缀同步为 `TypedCatalog*`。 | `MetBench_BLL.Core/SystemMT/Catalog/Typed/Schema/*`<br>`Catalog/Typed/Serialization/TypedCatalogSerializer.cs`<br>`Catalog/Typed/Lint/TypedCatalogAntiLegacyLinter.cs`<br>`Catalog/Typed/Specs/{MrSpec, PropertySpec, PredicateSpec, PropertyPredicateSpec, ProjectionSpec, ToleranceSpec, ShapeSpec, ParameterExpression, FiveDTags, MethodBinding, TransformStepSpec, FieldPairing}.cs`<br>`Catalog/Typed/Validation/{ValidationRegistry, MrSpecValidator, PropertySpecValidator, BinaryComparisonPredicateValidator, ScaledEqualityPredicateValidator, ErrorMonotonicPredicateValidator, BoundPropertyPredicateValidator, SharedReferenceResolver, ParameterExpressionResolver, ToleranceCompatibilityChecker}.cs` | `SystemMT/Catalog/Typed/{V12CatalogSerializationTests, V12CatalogLintTests, V12TypedModelTests, V12ValidationContractTests, V12CatalogSemanticValidationTests, ErrorMonotonicPredicateValidatorTests}.cs`（测试文件名保留 `V12*` 前缀以保留 v1.2 路线图溯源；命名空间已迁移）<br>`Architecture/SemanticCatalogNamingBoundaryTests.cs`（PR-B 命名守卫） | ✅ pass |
| F-INFRA-12 | AGENTS Stage 8；v1.2 PR #100–#110；PR-B 重命名（#115）；PR-C 运行时收敛（#118）；PR-D 守卫与 W1 清理（#119） | **MR 验证统一设计 v1.2 运行时与迁移 gate（已在当前路线图闭环）**：scalar runtime / applicability / 5 态状态 / convergence / sequence shape / subadditive / field equality / derived invariant / statistical + cross-method / property runtime / exponential growth / typed migration + coverage gate / retrospective review-fix。**PR-C 进一步把 System MT pipeline 断言阶段切到 `PredicateDispatcher` + `VerificationContext`**，`AssertionEvaluator` 已不在生产路径上。 | `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/{PredicateDispatcher, IPredicateDispatcher, VerificationContext, VerificationResult, VerificationDiagnostic, DiagnosticContext, VerifyStatus, RoleOutput, DeterministicScalarToleranceEvaluator, BinaryComparisonKernel, ScaledEqualityKernel, ErrorMonotonicKernel, SequenceShapeKernel, SubadditiveKernel, FieldEqualityKernel, DerivedInvariantKernel, VarianceRatioKernel, FieldProportionalityKernel, CrossMethodComparisonKernel, OrderedSequenceShapeKernel, SequenceValue, Field2DValue, StatisticalValue, LogLinearFit}.cs`<br>`Catalog/Typed/Property/{PropertyChecker, BoundPropertyChecker, ShapePropertyChecker, PropertyVerificationContext, PropertyResult, PropertyStatus, PropertyCoverageSnapshot, IPropertyChecker}.cs`<br>`Catalog/Typed/Validation/{ApplicabilityEvaluator, OrderedSequenceShapePredicateValidator}.cs`<br>`Catalog/Typed/Derived/{FiniteDifferenceDerivedExpression, CoefficientOfVariation, MassNumberSum, L2Norm, LinfNorm, FieldRegionMean, ScalarSubtract}.cs`<br>`Catalog/Typed/Migration/{LegacyAssertionPredicateMapper, TypedSpecFactory, TypedVerificationContextFactory}.cs`<br>`Pipeline/{SystemMtPipeline, PipelineContext}.cs`（PR-C 接 `IPredicateDispatcher`） | `SystemMT/Catalog/Typed/{BinaryComparisonKernelTests, ScaledEqualityKernelTests, ApplicabilityModelTests, ApplicabilityEvaluatorTests, VerificationStatusFlowTests, PredicateDispatcherTests, ReferenceRoleModelTests, ErrorMonotonicKernelTests, SequenceShapeModelTests, SequenceShapeKernelTests, SubadditiveKernelTests, DerivedExpressionTests, FieldModelTests, FieldEqualityKernelTests, DerivedInvariantKernelTests, StatisticalModelTests, VarianceRatioKernelTests, FieldProportionalityKernelTests, CrossMethodComparisonTests, PropertyRuntimeContractTests, ExecutablePropertyFixturesTests, ExponentialGrowthModelTests, ExponentialGrowthEvaluatorTests, KinPhy02ExecutableTests, V12CatalogMigrationTests, V12GoldenFixtureTests, V12CoverageGateTests, LegacyAssertionPredicateMapperTests, SystemMtPipelineTypedRuntimeContractTests}.cs`<br>`Architecture/SemanticCatalogBoundaryTests.cs`（PR-D 生产侧守卫） | ✅ pass |
| F-INFRA-13 | 2026-05-30 client multilingual i18n plan；T1 WPF user entry | **UI-neutral bilingual localization core + WPF client adapter**：中文/英文 `.resx` 资源、运行时 culture 切换、缺失 key / unsupported culture fallback、WPF view-model/provider binding；核心库保持 Avalonia-ready，不依赖 WPF/Avalonia。 | `MetBench_UI.Localization/{IAppLocalizationService,AppLocalizationService,LocalizedTextProvider,AppCultureOption}.cs`<br>`MetBench_UI.Localization/Resources/{Strings.resx,Strings.zh-CN.resx}`<br>`MetBench_Client/ViewModels/*` and `MetBench_Client/Views/Pages/*.xaml` localization bindings | `MetBench_SystemMT.Tests/ClientI18n/*.cs`<br>`MetBench_Client.Tests/ClientI18n/*.cs`<br>`docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/` | ✅ SystemMT ClientI18n 10/10 PASS；Client.Tests ClientI18n 3/3 PASS；WPF build 0 errors；UIA screenshots 9/9 PASS + full-page bilingual evidence。⚠ WPF / legacy warning debt remains out of this i18n gate |
| F-INFRA-14 | T0-T5 release delivery / user handoff | **T0-T5 用户操作说明**：图文说明启动、语言切换、T0 执行、T1 CRUD/catalog/history、T2 报告、T3 覆盖、T4 发现/候选评审、T5 异常/回放、异步运行/资产导入导出/结果导出（§7）。 | `docs/usage/MetBench-T0-T5-操作指南.md`<br>`docs/usage/images/*.png`（含 `t7-async-*.png`，源自 async import/export VM 证据） | `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/` and `docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/` provide screenshot provenance；§7 异步操作截图源自 `docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-vm-evidence/` | ✅ 中文图文指南已合入（含 §7 异步操作）；英文版用户指南尚未建立 |

## 10. 缺口清单（gap report）

> 本节是当前**唯一**待办池。新增功能编号或修复缺口必须在此登记，未登记不得动代码。

| 缺口编号 | 关联功能 | 缺口描述 | 影响范围 | 处置建议 |
|---|---|---|---|---|
| ~~G-01~~ ✅ 已缓解(2026-05-31) | F-T1-05（WPF 客户端） | ~~云端 CI 不能编译 WPF（`net8.0-windows7.0`），完全无自动测试覆盖~~ | Windows-only WPF 仍不能在 Linux CI 编译，但已新增 `MetBench_Client.Tests` + `tools/smokeshot` VM/UIA 证据路径 | T0-T5 VM release smoke 21/21 PASS；Client i18n VM 9/9 base screenshots + full-page bilingual evidence；后续 Windows-touching PR 仍需 VM evidence |
| ~~G-02~~ ✅ 已完成(2026-05-23) | F-T2-03（LiveCharts 数据层） | ~~MTVisualizationService 跨平台部分无独立单测~~ | — | 新建 `Bll/MtVisualizationServiceTests.cs`（6 测试覆盖 Line/Scatter/Pie/未初始化/非法 PlotType/重复 Initialize） |
| ~~G-03~~ ✅ 已完成(2026-05-23) | F-T3-03（反应堆 5 方程锚定） | ~~diffusion + Navier-Stokes 两条 L2 解析解 / SUT 未落地~~ | T3 覆盖完成 | S8-P3 落 navier-stokes（1D subchannel SUT + 2 MR）；S8-P4 落 diffusion（1D FD SUT + 2 MR）。5 方程全覆盖（boltzmann / bateman / fourier / diffusion / navier-stokes） |
| G-04 | F-T6-02（语义变异 + 等价识别 + 最小 MR 子集） | 完全未实现 | Stage 8 变异模块增强未启动；`2026-06-16-quality-follow-up-plan.md` 仅记录边界，不宣称交付 | CLAUDE.md §2 / AGENTS Stage 8 "主线之外"已列为 backlog |
| ~~G-05~~ ✅ 已完成(2026-05-23；PR-D refit 2026-05-25) | F-MR-P7 | ~~LaTeX→SymPy `[Obsolete]` 后无 grep 守卫单测~~ | — | 已建 `Architecture/ObsoleteAttributeGuardTests.cs` 覆盖 `Latextosympy` + `Latextosympy_Await`（`SystemMtRunner` 已在 PR-D / #119 删除，对应 InlineData 与单独 fact 已同步移除；其作用由 `Architecture/SemanticCatalogBoundaryTests.cs` 接管） |
| ~~G-06~~ ✅ 已完成(2026-05-23) | F-T1-04 / F-MR-P5 | ~~method MT 协议层未接入业务路径~~ | — | 已建 `IMtPipeline<TReq,TOut>` 共享抽象（BLL.Core/MT）+ `MethodMtPipeline`（BLL/MethodMT，实现协议层）+ `MethodMtRunRequest/Outcome` 数据 record + `MethodMtCatalogService` 扩 CRUD（Get/Update/Delete）+ `SystemMtPipeline` 加 IMtPipeline 显式接口实现；20 新测试（7 pipeline + 4 Bateman 参数化 AAA + 9 CRUD）；全量回归 810→830 pass。注：4 处 `Latextosympy*` 调用已澄清属 v1 展示衍生字段（不在 G-06 范围），归 G-11 处置 |
| ~~G-07~~ ✅ 已完成(全部；PR-D 终局清理 2026-05-25) | F-T0-02 / F-T0-01 | ~~W1 引擎残留~~ | — | 云端：`SystemMtRunner` 先加 `[Obsolete]`；VM 端（G-07b）：`App.xaml.cs:130` DI 注册已删除（commit `dcf978a`）。**PR-D / #119 已删除整套 W1 类**：`IMrAssertion.cs`、`ApproxEqualAssertion.cs`、`GreaterThanAssertion.cs`、`LessThanAssertion.cs`、`SystemMtRunner.cs`、`EqualityThresholds.cs` 与对应 5 个测试文件均已 `git rm`，`Architecture/SemanticCatalogBoundaryTests.cs` 守卫防止回潮 |
| ~~G-08~~ ✅ 已完成(全部) | F-T1-04 / F-T0-03 | ~~catalog 双 seed 不自动同步~~ | — | 云端：`SystemMtBootstrap.SeedCatalogsAsync` helper + 4 测试；VM 端（G-08b）：`App.xaml.cs` 注册 `ISystemMtMetadataRepository` + `LauncherCatalogV2Importer`，`OnStartup` 调 bootstrap（commit `13b3447`）。完整 source-of-truth 收口属 Stage 9+ 重构 |
| ~~G-09~~ ✅ 已完成(2026-05-23) | F-T3-02 / F-T1-04 | ~~projectile SUT 未进 launcher catalog~~ | — | 已补 `projectile-motion` EquationMetadata + `projectile-scale-v0` MrMetadata + MrBlueprint + `SUT/projectile/sample/standard.txt`；2 个 launcher 测试 + cascade 4 个 importer 测试更新；全量 809→810 pass |
| ~~G-10~~ ✅ 已完成(2026-05-23) | F-T1-04 | ~~CRUD 不全~~ | — | (a) `ISystemMtMetadataRepository` 加 3 个 DeleteAsync（Equation / MR / Recipe）+ LiteDb 实现 + Fake repo 实现；(b) `SystemMtCatalogService` 加 `UpdateEquationFunctionAsync` / `DeleteEquationFunctionAsync`；(c) `MethodMtCatalogService` MR-CRUD 子集已在 G-06 落地。9 新测试。**剩余开口**：MR / Application binding 的 Delete cascade 语义（非本次范围） |
| G-X1-Adv ✅ 已完成(2026-05-24) | F-T1-05（WPF 客户端） | PR #88 删 `CandidateReviewViewModel.UseAdversarial`，XAML CheckBox 残留 binding error | WPF 云端不可编译，binding 错误在 VM 启动后才可见 | 删除 `CandidateReviewPage.xaml` 中 adversarial-mutmut CheckBox 元素（commit `254c167`） |
| G-X2-LatexGuard ✅ 已完成(2026-05-24) | F-T1-04（v1 兼容守卫） | G-11 裁决配套守护测试：grep 断言 4 处 `Latextosympy*` 调用仅存在于指定 v1 兼容路径，新增调用即失败 | 新 MR 误用 LaTeX 老路径无感知 | 新建 `Architecture/LegacyPathBoundaryTests.cs`（2 测试，878 pass，commit `1479962`） |
| G-11 ⚖ 已裁决(2026-05-23)：保留至 Stage 9 | F-T1-04（v1 兼容） | **v1 LaTeX 展示衍生字段路径**：`MetamorphicRelationService.Add/Update` + `AutoMRParser.ProduceMRs/Async` + `MRRecommendationViewModel` + `MRManagementViewModel` 共 4 处调 `Latextosympy*`（已 `[Obsolete]`）。与 method MT 执行栈（G-06）完全正交，不影响新功能 | v1 UI 展示完整，`ObsoleteAttributeGuardTests` 守卫防止新增调用 | **裁决(a)：保留为 v1 兼容**，不做额外修改。**Stage 9 清理义务**：届时须删除 `Latextosympy` / `Latextosympy_Await` 类、4 处调用、LiteDB 中的 SymPy 文本 + PNG 衍生字段，并迁移已有 MR 记录。此决策由用户于 2026-05-23 确认 |
| G-12 | F-T1-04（远期 PBT 升级） | **method MT 升级到 property-based testing**：当前走 AAA + catalog-driven validator（G-06），未引入 FsCheck / Hedgehog 等 PBT 框架。PBT 与 MT 范式天然契合（property over many inputs + shrinking） | 当前 SUT 是解析解，无 bug 可找；MR 数个位数；PBT generator 工程量大于当前 MR 验证工程量 | 触发条件：method MR 数 ≥ 20 跨多方程 ∥ 接入有 bug 风险的真实 C# SUT。届时 catalog schema 加 input domain 字段，AAA 测试保留为基线、新增 PBT validator 作为第二层 |
| G-13 | F-T3-02（远期 PBT 升级） | **system MT 在轻量 SUT 上叠加 PBT 做模糊测试**：当前 system MT 走 BDD `.feature`，OpenMOC/OpenMC 单 case 时长（30s / 5min）禁止 PBT；但 projectile / damped-oscillator / lotka-volterra / decay-chain 单 case < 1s，PBT 可行 | BDD 的领域沟通价值不可替代（OpenMOC/OpenMC 永远不走 PBT）；轻量 SUT 是 PBT 的合适载体 | 触发条件：轻量 SUT 的 BDD 稳定 + input generator 工程量预算允许。覆盖范围严格限制为 < 1s 单 case 的 ODE SUT |
| G-X3-CatalogConvergence | F-T0-02 / F-T1-04 / F-T0-03 | **System-MT catalog 收敛已推进但未闭环**：`SystemMtLauncher` 已接入 `IMrCatalogProvider`，WPF 默认注册 `ManifestMrCatalogProvider`，launcher 生产 fallback 已删除，`LauncherCatalogV2Importer` 已改依赖 `ISystemMtCatalogReader`，ExecutionEvidence / V3MrIdRef / LiteDB evidence repository / recorder write-through 已合入，`SampleTraces` 已开始写入目标字段级 source / transformed / output triples | 双事实源压力已从“纯硬编码 catalog”降到“manifest 默认 + evidence 覆盖粒度仍可扩展” | 后续收敛重点应转为：扩展 sample trace 粒度、补 Windows 侧构建回执，并同步文档与基线叙事 |
| G-X4-V12Verification | F-INFRA-11 / F-INFRA-12 | **MR 验证统一设计 v1.2 已完成当前路线图闭环 + 验证语义收敛 PR-B/C/D 已合并**：`origin/main` 已合并 PR-0..PR-10 + PR #110 retrospective review-fix + PR #115 命名永久化 + PR #118 运行时切到 typed dispatcher + PR #119 守卫与 W1 清理。生产代码面位于 `MetBench_BLL.Core/SystemMT/Catalog/Typed/`，pipeline 断言阶段已不再调 `AssertionEvaluator`；W1 `IMrAssertion` 路径在生产侧消失。 | 主线已具备 schema + validate + 多类 runtime kernel + property path + migration gate + 运行时收敛 + 生产守卫的全闭环 | 当前 inventory 真相层以仓库 gate 为准：**44 MR + 4 Property**。后续工作不再是“完成 PR-7..PR-10”或“收敛断言运行时”，而是 (a) ExecutionEvidence v2 实现（PR-C0），(b) 把未映射的 6 个旧 assertion code 扩到 typed predicate（`less-noise-aware` / `greater-noise-aware` / `approx-invariant` / `variance-ratio` / `flux-pointwise-approx` / `cross-program-agree`），(c) 后续 evidence 粒度与 coverage 质量提升 |
| ~~G-T1-MultiVenv~~ ✅ 已完成 (PR-1 #157) | F-T1-01 / F-T1-02 | ~~**多 venv 配置与管理产品化**~~：`LauncherOptions.RuntimePythons` 通用 map + `ResolvePythonExecutable(runtimeKey)` 已替换 per-runtime 硬编码槽位；`ManifestMrCatalogProvider` 改为单点 resolver 调用；未知非 system key 在 resolver 处 fail-closed 并附带可定位的诊断 (`RuntimeEnvironmentResolutionException`)；新 runtime family（FEniCS / FiPy / torch-surrogate 等）从此为纯配置变更，无需改 `LauncherOptions` 字段或 `PythonExecutableKinds.All`。`PythonExecutableKinds.All` 仅保留作 parity 测试的 legacy 兼容字段，不再作 closed-set rejection gate。 | 已解决 — 状态账本 "T1 multi-env management" 行已由 Open 改为 Controlled；plan 已归档到 active plan index §3 | 已完成；scoped plan: `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md`（已合并） |
| G-T1T2-UiOnlyMrSut | F-T1-04 / F-T1-05 / F-T2-01 | **UI-only MR / SUT 接入**：当前 MR / SUT 入 catalog 仍要求编辑 manifest `catalog.json` 及相关 metadata/blueprint；WPF 用户无法在不修改源码的前提下登记、查看、修改、校验、保存 MR。CLI / repository CRUD 与作者手编 JSON 不能替代研究者可用的 MR CRUD 页面。 | 非开发者使用者无法自助登记 MR；MetBench 仍偏作者私用工具；外部研究者无法只通过 UI 完成 MR 资产管理 | 单独 T1+T2 Windows/VM capability，**不属于 PR-T3C，不阻塞也不解锁 PR-T3C**。需要专门 Windows-scoped plan；执行时用 SSH 编译/日志、RDP 或 FlaUI 做 WPF 可见交互验证 |
| G-T1T3-HeavyDepSutSpec | F-T1-02 / F-T3-02 | **重依赖 / data-driven SUT 接入规范**：当前 SUT 接入 checklist（`docs/PROJECT-STRUCTURE.md` §10）面向 pure-stdlib 与传统 Python 求解器，未覆盖 (a) checkpoint 出处 / 哈希 / 许可与缓存策略、(b) asset manifest 强制项、(c) clean-skip 策略（venv / checkpoint / fixture 缺失时 `[SkippableFact]` 行为）、(d) tiny fixture 大小约束、(e) 许可证追踪。新 data-driven 候选（MeshGraphNets backlog）需要这套规范才能安全落地。 | data-driven SUT 接入若各家自选规范，将导致 fixture 体积失控、checkpoint 来源不可复现、CI 失败模式紊乱 | 单独 T1 / T3 边界规范 capability，**不属于 PR-T3C，不阻塞也不解锁 PR-T3C**。需求源：[`docs/superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md`](superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md) §5.1.2。启动时间由用户决定，需先在 active plan index 注册独立的 capability 实施 plan |
| G-T2-SutMrDocProduct | F-T2-01 / F-T2-02 / F-INFRA-14 | **SUT 接入说明与 MR 使用说明产品化**：T0-T5 用户操作层已有中文图文指南 `docs/usage/MetBench-T0-T5-操作指南.md`，但每 SUT 接入文档与每 MR 使用文档仍缺统一模板、索引页和 lint。规模随 T3 / T4 推进仍会失控。 | 用户侧最小上手路径已改善；开发者/研究者级 SUT/MR 资产文档仍不成体系 | 单独 T2 docs-product capability 仍有效，但范围应从“先写用户操作指南”调整为“每 SUT / 每 MR 文档模板 + 索引 + lint”。需先在 active plan index 注册独立 scoped plan |

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
| `792cc46` | **G-X3 docs** | Catalog convergence spec v3 + plan v2 + §10/§11/AGENTS 指针（doc-only，无测试基线变化）|
| `290b927` | **G-X3 Task 1** | Catalog definition models + IMrCatalogProvider boundary (878→884, then amended 884→919 after 15-finding self-review) |
| `953da7b` | **G-X3 Task 2a** | refactor: extract LegacyCatalogFactory from SystemMtLauncher (919 → 919, behavior-preserving) |
| `e5aade8` | **G-X3 Task 2b** | HardcodedMrCatalogProvider + 8 smoke tests (919 → 927) |
| `9f74d69` | **G-X3 Task 2c** | ManifestMrCatalogProvider + 9 catalog.json + 12 manifest tests + 2 parity tests (927 → 943) |
| `c923063` | **G-X3 Task 3** | Inject IMrCatalogProvider into SystemMtLauncher; MrCatalogEntry 8 → 13 fields; ToBlueprint inverse; 3 injection tests (943 → 946) |
| `73d3e36` | **G-X3 Task 4** | [Obsolete] on HardcodedMrCatalogProvider + sunset guard（并入 PR #91，948 pass） |
| `2005909` | **G-X3 Task 5** | Execution evidence models + V3MrIdRef + repo contract (4 model tests, 948 → 952) |
| `5f9d27d` | **G-X3 Task 6 step 1** | LiteDb evidence repository + roundtrip tests (+7 tests, 952 → 959) |
| `763e067` | **G-X3 Task 6 step 2** | SystemMtExecutionRecorder write-through evidence + V3 lookup (+6 tests, 959 → 965) |
| `fe864ec` | **G-X3 VM** | App.xaml.cs registers ManifestMrCatalogProvider for IMrCatalogProvider DI (unblocks Task 7 fallback removal) |
| `5691727` | **G-X3 hotfix** | ManifestMrCatalogProvider 路径分隔符规范化 — fixes Windows CatalogParityTests 回归 from PR #91（历史主线节点） |
| `ba7a9a1` | **G-X4 / v1.2 PR-0** | typed catalog foundation + anti-legacy lint 入主线 |
| `dce8378` | **G-X4 docs** | PR-1..PR-10 master roadmap + per-PR plans 入主线 |
| `ded74fc` | **G-X4 / v1.2 PR-1** | typed model + fail-closed validators（979 pass） |
| `bfa3097` | **G-X4 / v1.2 PR-2** | scalar verification runtime（两层 review 前仍走本地 review；后已补 retrospective review） |
| `7f2aca3` | **G-X4 / v1.2 PR-3** | applicability gating + verify statuses（990 pass） |
| `bbac97f` | **G-X4 / v1.2 PR-4** | reference / convergence runtime（994 pass） |
| `cac2b94` | **G-X4 / v1.2 PR-5** | sequence shape + subadditive runtime（1006 pass） |
| `8bd734f` | **G-X4 / v1.2 PR-6** | field + derived invariant runtime（1015 pass） |
| `b5419cb` | **G-X4 / v1.2 PR-7** | statistical + cross-method runtime（1023 pass） |
| `0ea8207` | **G-X4 / v1.2 PR-8** | property runtime path（1031 pass） |
| `428297c` | **G-X4 / v1.2 PR-9** | exponential growth runtime |
| `406ae15` | **G-X4 / v1.2 PR-10** | typed migration + coverage gates；显式 inventory 收口为 **44 MR + 4 Property** |
| `e839214` | **G-X4 / v1.2 PR-10 review-fix** | invalid golden fixture / coverage semantics retrospective hardening（**1043 pass / 0 fail / 0 skip**，当前共享代码测试绿基线） |

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
