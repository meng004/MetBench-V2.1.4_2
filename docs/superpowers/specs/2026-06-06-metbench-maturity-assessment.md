# MetBench 成熟度与代码质量评估（2026-06-06）

> 一次性快照报告。方法：全仓硬指标 + 3 个独立 fresh-session 取证 agent（T0–T3 / T4–T6 / 各 project）
> + 本会话亲历核查。基线 `main`@`ba0c808`，全量 **1800 pass / 0 fail / 19 env-gated skip**，
> 1563 [Fact]/[Theory] + 27 BDD feature。成熟度分级：**Prototype < Functional < Hardened < Production-grade**。
> 本报告是事实快照，不是状态账本（状态以 `docs/status/current.md` 为准）；修复计划见
> `docs/superpowers/plans/2026-06-06-metbench-maturity-remediation-plan.md`。

## 总体结论

核心引擎（BLL.Core）与测试工程已 production-grade / A 级；功能层 T0–T5 普遍 **Functional**（T0/T5 偏 Hardened）；
短板集中三处：**T6 变异是原型尾巴、遗留外围层（BLL/DAL/Domain/IDAL）有质（非仅量）的债、38 个 MR 仅 3 个在 CI 真跑的覆盖盲区**。

## 维度一：按 T 计划（功能层成熟度）

| 层 | 成熟度 | 真实已实现（证据） | 主要缺口/风险 |
|---|---|---|---|
| T0 核心 MT 流程 | Functional→Hardened | 9 状态 pipeline（`SystemMtPipeline.cs` 540 LOC）+ 11 typed 谓词内核（`Catalog/Typed/Runtime/PredicateDispatcher.cs`）+ `ISystemMtLauncher` facade + 记录器；真子进程（`DefaultProcessExecutor`，超时杀进程树）。`SystemMtPipelineTests`(58)、`SystemMtLauncherTests`(110)、BDD 跑真 Python | 多相路径 `ExecuteMultiPhaseAsync` 与 `SampleTraces` 仅 fake 单测，未在 CI 用真 SUT 端到端验证 |
| T1 直接支撑 | Functional | 进程/IO 适配器、CRUD 编辑器（`Catalog/Editing`，持久化前校验+越权防护）、异步作业（`Jobs/`，93 测试）、导入导出（29）、OpenMOC×OpenMC 差分（架构通） | Python 适配器测试薄（5）；差分 CI 内 env-gated；已知 OpenMOC×OpenMC 疑似缺陷未固化为失败测试 |
| T2 可视化与报表 | Functional | 四端渲染器（HTML/Word/Excel/PDF）+ SkiaSharp 图表，结构化测试 84+；异步导出作业接通（#308–#317） | 无 job→render→persist 跨层集成测试；`SystemMtReportService` 出 Markdown 与四端渲染器之间有断层 |
| T3 覆盖 | Functional→Hardened（catalog 治理 Hardened） | 38 MR / 21 SUT / 17 方程；反应堆物理 5 锚定方程全有真 Python SUT；catalog 计数+parity CI 强约束；typed v1.2：44 MR+4 Property；**15 个 `LauncherEndToEnd*Tests.cs` 覆盖 38/38 MR**（CI 内 32 真跑 / 8 env-gated 跳过 OpenMOC/OpenMC/scipy） | `CoverageService` 4 维仅 5 个 happy-path 测试；env-gated 测试本地需配 venv 才能跑 |
| T4 MR 识别 | Functional | 三条路线都真实现：meta-pattern（跑真 python sidecar）、multi-LLM 共识（Cohen's κ）、SCG 语义因果图（真 `scg.json`，do-calculus 三模式）；`ILlmGateway` 真 HTTP；`ValidationService` 提升闭环。50+ 测试 | 生产 DI 未接真 LLM key / `EmpiricalRepoSampler` → 运行应用里 LLM 路线静默返回空 |
| T5 异常 | Functional→Hardened | 真 CRUD+过滤+共性分析+状态机（代码强约束非法转移）+孤儿清扫+launcher 桥接。44+ 测试 5 个类 | 缺陷回放三元组（程序版本×MR×输入）未接通（`RCaseRepro` 独立未连）；状态机无真 LiteDB 集成测试 |
| T6 变异 | **Prototype** | 战役编排引擎真+测试好（`MutationCampaignService`，8 实质测试，杀死率/覆盖率算对） | **无真实变异注入器**（`Mutant.AppliedDiff` 存了但无 `IMutantApplicator`）；WPF 用 hash `StubCellRunner` 假跑；**最小 MR 完备子集搜索完全缺失** |

## 维度二：按解决方案下的 project（代码质量）

| 项目 | LOC/文件 | 评级 | 自身 CS 警告 | TWAE | CI 门 | 关键债/风险（file 证据） |
|---|---|---|---|---|---|---|
| MetBench_BLL.Core | 25040/341 | B+ | 0 | ✅ | ✅必需 | `LegacyCatalogFactory.cs`(1147)+`SystemMtMetadataCatalog.cs`(1278) 维护面大；`App.xaml.cs:187` `ISystemMtCatalogReader` cast 隐式耦合；缺 `LegacyResultRecordParityTests` |
| MetBench_BLL | 7498/61 | C | ~458 | ❌ | 间接 | **真 §6 违规**：`SemanticSimilarityDetector.cs:368/60/230` 静默吞异常(CS0168)；`SyntaxSimilarityDetector.cs:392` async 无 await；`SupportRateCalculator.cs` 多处真 NPE 路径；`Latextosympy*.cs` `[Obsolete]` 仍编译 |
| MetBench_DAL | 3550/33 | B- | ~132（多为有意 CS0618） | ❌ | 间接 | 废弃字段读兼容掩盖真警告；`DbConfig._conn` public 却下划线命名；双库隔离确认真实（`LiteDbSystemMtResultRepository.cs:30` 私有 `BsonMapper`） |
| MetBench_Domain | 1770/40 | C+ | 31 | ❌ | 间接 | `MetamorphicRelation.Expression` CS8618 = LiteDB 反序列化真 NPE 风险 |
| MetBench_IDAL | 719/30 | C+ | 62 | ❌ | 间接 | `DatatoImage.cs`（图表工具）放在契约项目=放错层（CS8603） |
| MetBench_Client | 12378/102 | B- | 不可 Linux 编译 | ❌ | ❌ | **18+ 处 `.ShowDialogAsync().Result`**（8 VM，死锁面）；`MTReportGeneratorViewModel.cs:48` `async void`；遗留页非 partial+手写 `OnPropertyChanged`；**5 套 MVVM 框架并存** |
| MetBench_SystemMT.Tests | 44581/290 | A- | 16–28 | ❌ | ✅必需 | test:prod≈1.24:1；无 Moq（全手写 fake）；3 parity+5 架构边界测试；缺 `LegacyResultRecordParityTests` |
| MetBench_Analyzers | 296/2 | B | 0 | — | 随 Core | METBENCH001 仅 Info 级（advisory，不阻断） |
| MetBench_Client.Tests | 548/5 | C+ | 0 | ❌ | 间接 | 仅 i18n，无 VM/command/nav 测试 |
| MetBench_UI.Localization | 167/5 | A- | 0 | ❌ | 间接 | 无实质问题 |

**遗留层 nullable 警告债合计 ≈ 680+（BLL 458 / DAL 132 / IDAL 62 / Domain 31），均无棘轮。**

## CLAUDE.md 文档漂移（实测发现，需修文档）

1. §4 称 Stylet 仅在 `MTExecutionPage.xaml` —— 实际 **10 个 XAML** 用 `s:View.ActionTarget`。
2. §4 称 HandyControl "removal tracked as follow-up" —— 实际**已移除**，由 `Controls/SimplePagination.xaml` 替代。

## Top 风险（优先级，2026-06-06 实施后修正）

> **风险 1 修正（2026-06-06）**：原文写"38 MR 仅 3 个在 CI 真跑"是误读
> `V12CoverageGateTests.RunnableFixtureCount = 3`（v1.2 typed-catalog 内的 golden-fixture
> 数）当成全 Launcher 覆盖。实测 `LauncherEndToEnd*Tests.cs` 共 15 个，38/38 MR 全有
> `RunAsync("<mr-id>")` 真跑或 BDD 步骤；CI 内 launcher 子集 32 pass / 8 env-gated skip。
> **该风险撤销**。修复计划 Phase 1 同步 descope。

1. ~~覆盖假象（T3）~~ **撤销 2026-06-06**：原始判断基于误读，实际覆盖 38/38。
2. BLL 的 §6 违规 + NPE 路径（质，非仅量）。
3. Client 死锁面：18+ `.Result` 跨 8 VM，无自动门可抓。
4. T6 名实不符（编排壳 + 假 runner）。
5. 遗留层警告债不收敛（≈680+，无棘轮）。
6. ~~缺 `LegacyResultRecordParityTests`~~ **已修 2026-06-06**（PR #322，3 测试入 main）。
7. Domain CS8618 / IDAL DatatoImage 放错层（CS8603/8618 处 **已修 2026-06-06**，PR #322；物理移动仍为 follow-up）。

## 亮点

- 测试纪律（1.8k 测试 + 27 BDD，手写 fake + parity + 架构守护）是项目最强资产。
- 治理机械化（catalog 计数白名单 + Roslyn 分析器 + Stryker + §12.4 元规则）领先一般科研代码。
- 核心层 BLL.Core（TWAE + 0 警告 + facade 隔离 + 边界守护）是 production-grade 工程基线。
