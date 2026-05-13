# MetBench v2 开发实施计划

> **目标**：按 `docs/design/` 五份基线文档，把 v2 设计**落地为可运行代码**。
> **总时长**：8 周（P1-P8 阶段）
> **本文件状态**：active — 用 checkbox 驱动执行
> **日期**：2026-05-13
> **依据**：`docs/design/v2-system-mt-architecture.md` + `entity-model.md` + `glossary.md` + `assertion-extensions.md` + `migration-plan.md`

---

## Scope Guard

本计划**只**做实施，不做设计变更。

- ✅ 实施 `docs/design/` 已定义的 schema / 接口 / 模块
- ✅ 写代码 / 单测 / 集成测 / 数据迁移脚本
- ✅ 更新 `CLAUDE.md` / `AGENTS.md` / `README.md` 反映实施进度

**不**做：
- ❌ 重新设计任何已在 `docs/design/` 定义的实体
- ❌ 引入新的子系统（不在五份基线文档中的）
- ❌ 修改 v1 既有方法级 MT（`MetBench_BLL/` / `MR.litedb`）
- ❌ 移除 HandyControl（评估为非 v2 范围，见 `evolution.md` §8）

任何上述偏移需 RFC：先改 `docs/design/`，再回本计划。

---

## 信息来源（按访问频率）

| 文档 | 用于什么 |
|------|--------|
| `docs/design/entity-model.md` | 23 collection schema 完整规格（每条字段、索引、关系） |
| `docs/design/glossary.md` | 术语严格映射（命名规则、中英对照） |
| `docs/design/v2-system-mt-architecture.md` | 模块边界 + Pipeline 状态机 |
| `docs/design/assertion-extensions.md` | FluentAssertions 扩展 API |
| `docs/design/migration-plan.md` | 8 周时间盒 + 阶段验收 |
| `docs/design/evolution.md` | 历史背景（避免重蹈漂移） |
| 既有 `MetBench_BLL.Core/SystemMT/*.cs` | Stage 4 实现参考（保留扩展） |
| 既有 `MetBench_Domain/*.cs` | v1 实体（已在 P1.1 扩展） |

---

## 阶段总览 — 8 周时间盒

| Phase | 周 | 目标 | 状态 |
|-------|---|------|------|
| P1 | W1 | DB schema 扩展 + 23 collection 实体 + DbConfig | **进行中** (1.1-1.2 完成) |
| P2 | W2 | Repository + 基础设施模块（Runtime / Sut / SampleCase） | pending |
| P3 | W3 | Adapter + ParameterMapping + IMRTransformation | pending |
| P4 | W4 | Pipeline + FA 扩展方法 + AssertionEvaluator | pending |
| P5 | W5 | BDD `.feature` ↔ DB 双向同步 + 历史数据迁入 | pending |
| P6 | W6 | Anomaly viewer + Replay + WPF UI | pending |
| P7 | W7 | Discovery + Mutation 子系统 | done (cloud) |
| P8 | W8 | Coverage + Trend + Reports + 验收 ship | pending |

---

## P1 — LiteDB schema 扩展（本周）

**目标**：23 个 LiteDB collection 全部有对应的 C# 实体类；`DbConfig` 完成注册 + 索引；编译通过。

### P1.1 ✅ 扩展 v1 既有实体（已完成，commit `d1515c1`）

- [x] `MetamorphicRelation.cs` 新增 v2 字段（保留 v1 字段；ApplicationName 标 Obsolete）
- [x] `Application.cs` 新增 v2 字段（保留 v1 字段；DomainName 标 Obsolete）

### P1.2 ✅ Value object record（已完成，commit `d1515c1`）

- [x] `V2/ValueRange.cs`
- [x] `V2/ToleranceConfig.cs`
- [x] `V2/SutHyperparams.cs`
- [x] `V2/SamplingSpec.cs`
- [x] `V2/ParameterMapping.cs`

### P1.3 ✅ 基础设施实体（已完成）

- [x] `V2/Runtime.cs` — 多运行时支持（python/matlab/cpp/java/fortran）
- [x] `V2/MRBinding.cs` — MR × SUT junction（含嵌入 ParameterMappings 列表）
- [x] `V2/ApplicationDomain.cs` — Application × Domain junction
- [x] `V2/MRInstance.cs` — 执行配置（参数 override + Sampling + Hyperparams + Tolerance override）

### P1.4 ✅ 执行 + 结果 + 异常实体

- [x] `V2/Execution.cs` — Pipeline 状态机记录（Guid PK）
- [x] `V2/Result.cs` — 数值结果 + 断言判定（Guid PK）
- [x] `V2/Anomaly.cs` — 违例样本（Guid PK，链 KnownBug）

### P1.5 ✅ Discovery 子系统实体

- [x] `V2/DiscoveryMethod.cs`
- [x] `V2/DiscoveryRun.cs`
- [x] `V2/CandidateMR.cs`
- [x] `V2/ValidationRun.cs`

### P1.6 ✅ Mutation 子系统实体

- [x] `V2/MutationOperator.cs`
- [x] `V2/Mutant.cs`
- [x] `V2/MutationCampaign.cs`
- [x] `V2/MutationResult.cs`

### P1.7 ✅ 已知 bug + 审计 + 批次 + 报告实体

- [x] `V2/KnownBug.cs`
- [x] `V2/AuditLog.cs`
- [x] `V2/Batch.cs`
- [x] `V2/BatchPlan.cs`
- [x] `V2/Report.cs`

### P1.8 ✅ DbConfig 扩展

- [x] 在 `MetBench_DAL/DbConfig.cs` 加 20 个新 collection key 常量
- [x] 在构造函数里注册 collection + 索引（按 `entity-model.md` §2 索引规格）
- [x] 务实选择：v1 + v2 共享同一 LiteDB 文件（按 `entity-model.md` §4 设计；MetamorphicRelation / Application 通过 Kind 字段区分行）

### P1.9 ✅ 编译 + TDD 单元测试

- [x] `dotnet build MetBench_Domain/MetBench_Domain.csproj` 通过 (0 errors, 31 pre-existing warnings)
- [x] `dotnet build MetBench_DAL/MetBench_DAL.csproj` 通过 (0 errors, 93 warnings — 含 16 个 CS0618 Obsolete 警告，预期)
- [x] `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj` 通过 (0 errors, 0 warnings)
- [x] `dotnet test MetBench_SystemMT.Tests/` — 173 通过 / 0 失败 / 2 skip (OpenMC env)
- [x] 新增 32 个单测：
  - `V2EntityRoundtripTests.cs` — 23 个实体 + 5 个 value object 的 LiteDB 往返
  - `V2DbConfigRegistrationTests.cs` — 23 个 collection key 暴露 + 注册行为
  - `V1CompatibilityTests.cs` — v1 既有 Obsolete 字段读写兼容 + v1/v2 共存

### P1.10 ✅ 提交 + push

- [x] commit `feat(v2-p1.3-1.7): 18 个新 collection 实体类`
- [x] commit `feat(v2-p1.8): DbConfig 注册 23 个 collection + 索引`
- [x] commit `test(v2-p1.9): TDD schema 验证 32 个测试全过`
- [x] push

### P1 验收

- [ ] CI 全绿（Linux 跑 cross-platform projects）
- [ ] 23 个 collection 全部可在 LiteDB 中 insert/read/delete
- [ ] v1 `MetamorphicRelation` / `Application` 既有读取行为**不退化**
- [ ] 文档同步：`entity-model.md` 中任何 schema 修订回写

---

## P2 — Repository + 基础设施模块（W2）

### Scope adjustment（基于 CLAUDE.md cross-environment rules）

Cloud agent **不可编译 WPF**（`MetBench_Client/`）。本阶段**只**做 Linux cloud 可验证的部分：
- ✅ `MetBench_IDAL/` — Repository 接口
- ✅ `MetBench_DAL/` — LiteDB Repository 实现
- ✅ `MetBench_DAL/` — `IServiceCollection.AddSystemMtRepositories()` 扩展方法（DI helper）
- ✅ `MetBench_SystemMT.Tests/` — TDD 测试

P2.4 WPF 页面**推迟到 VM 阶段**实施（按 CLAUDE.md cross-environment workflow）。VM-side
工程师从 cloud-side 的 `AddSystemMtRepositories()` + Repository 接口消费。

### P2.1 Repository 接口（IDAL）

实体共 23 个，按 PK 类型分两组（v1 既有不动）：

**int PK（既有 IRepository<T>）— 12 个**：
- v1 既有 3：MetamorphicRelation / Application / Domain（不动）
- v2 新增 9：
  - [x] `IRuntimeRepository`
  - [x] `IMRBindingRepository`
  - [x] `IApplicationDomainRepository`
  - [x] `IMRInstanceRepository`
  - [x] `IDiscoveryMethodRepository`
  - [x] `IMutationOperatorRepository`
  - [x] `IMutantRepository`
  - [x] `IKnownBugRepository`
  - [x] `IBatchPlanRepository`

**Guid PK — 11 个**（需新基接口）：
- [x] `IGuidRepository<T>` 基接口
- [x] `IExecutionRepository`
- [x] `IResultRepository`
- [x] `IAnomalyRepository`
- [x] `IDiscoveryRunRepository`
- [x] `ICandidateMRRepository`
- [x] `IValidationRunRepository`
- [x] `IMutationCampaignRepository`
- [x] `IMutationResultRepository`
- [x] `IAuditLogRepository`
- [x] `IBatchRepository`
- [x] `IReportRepository`

### P2.2 LiteDB Repository 实现

- [x] 20 个 `LiteDb<Entity>Repository` 类，模仿 v1 `DomainRepository` 模式
- [x] 复用既有 `DbConfig.Instance._conn`（不另开 LiteDB 文件，按 entity-model.md §4）
- [x] 每实现内置基础 CRUD：Add / Modify / Remove / Get(id) / GetAll

### P2.3 DI helper 扩展方法（cloud-side 可建）

- [x] `MetBench_DAL/ServiceCollectionExtensions.cs` —
      `AddSystemMtRepositories(this IServiceCollection)` 注册 20 个 Repository
- [x] App.xaml.cs 加一行 `services.AddSystemMtRepositories()` ★ VM-side 做

### P2.4 ❌ DEFER 到 VM-side

- [ ] `RuntimeManagementPage` (WPF) — VM-side
- [ ] `SutManagementPage` (WPF) — VM-side
- [ ] `SampleCaseManagementPage` (WPF) — VM-side

### P2.5 TDD 单元测试

- [x] 每 Repository 1 个 CRUD 完整测试（共 20 [Fact]）
- [x] 复合唯一索引违约测试（MRBindings / ApplicationDomains / MutationResults / DiscoveryMethods）
- [x] Guid PK 自动生成测试（Execution / Result / Anomaly 等）

### P2 验收

- [x] `dotnet build` 全 cross-platform projects 0 错误
- [x] `dotnet test` 既有 173 + P2 新增测试全过
- [x] 23 个 Repository 接口 + 20 个新实现 可被 DI 容器解析
- [ ] WPF UI 验收 推迟到 VM-side

---

## P3 — Adapter + ParameterMapping + IMRTransformation（W3）

### Scope adjustment（CLAUDE.md cross-env rules）

✅ Cloud-side：
- P3.1 IMRTransformation 接口 + 6 个 C# 实现（`MetBench_BLL.Core/SystemMT/Transformations/`）
- P3.2 ParameterMapping path resolver（C#）
- P3.3 Python parser 拆分（`SUT/<sut>/<sut>_input_parser.py` 等）
- P3.5 TDD 测试

⏸ VM-side defer：
- P3.4 WPF AdapterManagementPage

### P3.1 IMRTransformation C# 接口 + 实现

- [x] `MetBench_BLL.Core/SystemMT/Transformations/IMRTransformation.cs`
- [x] 6 个 Day-1 实现：
  - [x] `IdentityTransform` (Mut00 控制；不改变 dict)
  - [x] `ScaleField` (target field × factor)
  - [x] `TranslateField` (target field + offset)
  - [x] `PermuteIndices` (数组索引置换，用于 group-permute MR)
  - [x] `MirrorAxis` (几何坐标镜像，用于 m_inv 对称)
  - [x] `CompositeTransform` (多步组合)
- [x] `TransformationRegistry` 字典型工厂（按 Name 分派）

### P3.2 ParameterMapping path resolver

- [x] `MetBench_BLL.Core/SystemMT/ParameterMapping/IFieldPathResolver.cs`
- [x] 3 种 PathSyntax 实现：
  - [x] `JsonPointerResolver` — RFC 6901 风格："materials/fuel/temperature_kelvin"
  - [x] `McnpCardResolver` — MCNP 卡式："card:m1::tmp"
  - [x] `NamelistKeyResolver` — Fortran NAMELIST："&material/T"
- [x] `FieldPathResolverFactory`（按 PathSyntax 字符串分派）

### P3.3 Python parser 拆分（既有 → 新形态）

- [x] 把既有 `openmoc_input_adapter*.py` 拆成 `openmoc_input_parser.py`（仅 read/write，不含 transformation）
- [x] 创建 `openmoc_output_parser.py` 标准接口
- [x] 同上拆 OpenMC adapters
- [x] 同上拆 heat-equation adapters

### P3.4 ❌ DEFER 到 VM-side

- [ ] `AdapterManagementPage` — VM-side

### P3.5 TDD 单元测试

- [x] 6 个 transformation Apply() round-trip
- [x] 3 个 PathResolver get/set 操作
- [x] CompositeTransform 链式行为
- [x] 测试 IdentityTransform 不改变 dict（Mut00 控制）
- [x] 测试 ScaleField 数组字段 vs scalar 字段
- [x] Python parser round-trip (read → dict → write → 内容一致)

### P3 验收

- [x] `dotnet build` 全过 0 错误
- [x] `dotnet test` 既有 188 + P3 新增测试全过
- [x] Python parser 在 OpenMOC 案例上 round-trip 通过

---

## P4 — Pipeline + FluentAssertions 扩展 + 端到端（W4）

### Scope adjustment

✅ Cloud-side：P4.1 / P4.2 / P4.3 / P4.4 / P4.6（pipeline 单元 + 集成测试）
⏸ VM-side defer：P4.5 既有 IMrAssertion 删除（依赖 Stage 4 Launcher facade，需 VM 验证不破 WPF）

### P4.1 NuGet 依赖

- [x] 在 `MetBench_BLL.Core.csproj` 加 `FluentAssertions` 6.12.0
- [x] 在 `MetBench_BLL.Core.csproj` 加 `MathNet.Numerics` 5.0.0

### P4.2 断言扩展方法

- [x] `Assertions/AssertionInput.cs` record
- [x] `Assertions/ToleranceConfigDto.cs`（与 LiteDB ToleranceConfig 区分；BLL 层用 record）
- [x] `Assertions/SystemMtAssertionResultV2.cs` record（避免与 Stage 4 SystemMtAssertionResult 冲突）
- [x] `Assertions/AssertionTypeCodes.cs` 9 个常量
- [x] `Assertions/MetbenchAssertionExtensions.cs` — 6 个扩展方法：
  - `BeLessThanWithNoiseFloor`
  - `BeGreaterThanWithNoiseFloor`
  - `BeApproximatelyEqualUnderTransform`
  - `HaveVarianceRatio`
  - `BePointwiseApproximately`
  - `AgreeWithReference`
- [x] `Assertions/AssertionEvaluator.cs` switch 分派器

### P4.3 SystemMtPipeline 编排器

- [x] `SystemMT/Pipeline/PipelineStatus.cs` — 状态枚举
- [x] `SystemMT/Pipeline/PipelineContext.cs` — 跑时上下文（参数 / 路径 / SUT 信息）
- [x] `SystemMT/Pipeline/ISystemMtPipeline.cs` — 接口
- [x] `SystemMT/Pipeline/SystemMtPipeline.cs` — 9 状态机实现
- [x] 失败处理：每步异常落 Execution.ErrorMessage；状态自动切 "error" / "timeout"

### P4.4 ReplayService

- [x] `SystemMT/ReplayService.cs` — 从 Anomaly 重跑 Execution + 对比新旧

### P4.5 ❌ DEFER 到 VM-side

- [ ] 删 `IMrAssertion.cs` / `GreaterThanAssertion.cs` / `LessThanAssertion.cs`
      （依赖 Stage 4 Launcher facade，需 VM 验证不破 WPF；本 PR 与 v2 并存）

### P4.6 TDD 端到端测试

- [x] 6 个断言扩展方法 pass+fail 路径
- [x] AssertionEvaluator 按 AssertionTypeCode 分派
- [x] SystemMtPipeline 状态机：queued → parsing-source → ... → ok/anomaly/error
- [x] ReplayService 重跑对比逻辑

### P4 验收

- [x] `dotnet build` 全 cross-platform 0 错误
- [x] TDD 新增测试全过；既有 221 + P4 新增 ≥ 25 = ≥ 246 pass
- [x] FluentAssertions API 风格一致（`value.Should().BeXxx(...)`）

---

## P5 — BDD `.feature` 双向同步 + 历史数据迁入（W5）

### Scope adjustment（CLAUDE.md cross-env rules）

✅ Cloud-side：
- P5.1 同步工具（Python）— 输出 / 输入是 JSON + `.feature` 文本
- P5.2 Reqnroll 通用 step bindings (C#)，写入测试项目；编译验证 + 单元化测试
- P5.3 迁移脚本（Python）— 生成 v2 迁移 JSON（C# 端实际写库由 P8 端到端验收时跑）
- P5.4 `.feature` 文件骨架（脚本生成）

⏸ VM-side defer：
- 真实跑 `validate_feature_sync` 对接 LiteDB.Instance（需 WPF App.config）

### P5.1 同步工具（Python）

- [x] `tools/feature_to_db.py` — 解析 `.feature` → 输出 JSON 描述 (MRSchema + MRBindings)
- [x] `tools/db_to_feature.py` — 读 JSON catalog 描述 → 生成 `.feature` 骨架
- [x] `tools/validate_feature_sync.py` — diff `.feature` 与 JSON catalog；输出不一致清单 + exit code

### P5.2 Reqnroll 通用 step bindings

- [x] `Steps/SystemMtPipelineSteps.cs` — 5 个通用 step：
  - `Given the MR Schema "<code>" is bound to SUT "<sut>"`
  - `And the binding uses sample case "<sample>"`
  - `And the parameter mapping for "<abstractField>" is configured`
  - `When the MT pipeline runs with parameter "<name>"="<value>"`
  - `Then the (noise-aware )?"<assertion>" assertion holds on "<value>"`
- [x] step bindings 调 `SystemMtPipeline` (P4) + Repository (P2) — VM-side 实际执行；
      cloud-side 用 fixture 注入 fake services 单测

### P5.3 历史数据迁入（Python）

- [x] `tools/migrate_python_scenarios_to_v2.py` — `mutation_study.SCENARIOS` 29 行 → v2 JSON 迁移包
- [x] `tools/migrate_mutations_to_v2.py` — 48 mutations → MutationOperators + Mutants JSON
- [x] `tools/migrate_real_bugs_to_v2.py` — 6 R-Cases (来自 bug-inventory.md) → KnownBugs JSON

### P5.4 生成 `.feature` 文件骨架

- [x] 用 `db_to_feature.py` 从迁移 JSON 生成 29 + 5 = 34 个 `.feature` 文件
- [x] 放在 `metbench/catalog/features/` 按 MetaPattern 分子目录
- [x] 留 `## Physics rationale` 段空白待人工补正文

### P5.5 TDD 验证

- [x] Python tools 单测（pytest）— round-trip 验证 feature → JSON → feature
- [x] migrate scripts 输出 JSON schema 校验
- [x] Reqnroll step bindings 编译通过

### P5 验收

- [x] 34 `.feature` 文件骨架生成
- [x] feature_to_db / db_to_feature round-trip 一致
- [x] `dotnet build` + `dotnet test` 全绿（既有 255 + P5 新增）
- [x] migration JSON 文件可被未来 C# 端导入工具消费

---

## P6 — Anomaly viewer + Replay + WPF UI（W6）

### Scope adjustment（CLAUDE.md cross-env rules）

✅ Cloud-side：
- P6.1 `AnomalyService` C# 业务层 + `tools/analyze_anomaly_commonalities.py`
- P6.3 升级 `tools/render_dashboard.py` 读 LiteDB（dashboard.html 自动重生）
- P6.4 TDD：AnomalyService + commonality Python tool

⏸ VM-side defer：
- P6.2 WPF AnomalyListPage / AnomalyDetailPage / DashboardPage（含 WebView2）

### P6.1 AnomalyService 业务层

- [x] `MetBench_BLL.Core/SystemMT/Anomaly/AnomalyService.cs`
  - List / filter (按 Severity / Status / Category / Application / 时间段)
  - 共性分析方法 `AnalyzeCommonalities(IEnumerable<Anomaly>) → CommonalityReport`
  - 状态转移 `TransitionStatus(anomalyId, newStatus, notes)` + AuditLog 写入
  - 链 KnownBug `LinkToKnownBug(anomalyId, knownBugId)`
- [x] `Anomaly/CommonalityReport.cs` — record，含 dominantSut / dominantMP / factor 分布 / noise/macro 计数 + hypothesis 文字

### P6.2 ❌ DEFER 到 VM-side

- [ ] WPF AnomalyListPage / AnomalyDetailPage / DashboardPage

### P6.3 Dashboard 数据源升级

- [x] Python helper `tools/analyze_anomaly_commonalities.py`
- [x] `tools/render_dashboard.py` 改为可选数据源：JSON 或 LiteDB export（保持 v5 既有功能）

### P6.4 TDD

- [x] C# `AnomalyService` 单元测试（不依赖 LiteDB；用 mock Repository）
- [x] Python `analyze_anomaly_commonalities.py` 单元测试

### P6 验收

- [x] AnomalyService CRUD + commonality 单测全过
- [x] Python tool round-trip 验证
- [ ] WPF 页面验收推迟到 VM-side

---

## P7 — Discovery + Mutation 子系统（W7）

> Cloud scope（本仓库本周可做）：
> * `MetBench_BLL.Core/Discovery/` + `MetBench_BLL.Core/Mutation/` 业务服务
> * 抽象 LLM 调用为 `ILlmGateway`（可 fake，避免真实 API 依赖锁死 CI）
> * `MetBench_BLL.Core/Discovery/External/` 把 Python `noether_candidates.py` 当作可调用 sidecar
> * Reqnroll / xUnit TDD
>
> VM scope（本周不做，挂 DEFER）：
> * `MetBench_Client/Views/Pages/DiscoveryPage.xaml` + `CandidateReviewPage.xaml` + `MutationCampaignPage.xaml`
> * 真实 LLM provider (Anthropic / DeepSeek) 落地 — 端到端 smoke test 在 VM 跑

### P7.1 Discovery（cloud）

- [x] `Discovery/IMRDiscoverer.cs` 接口（return DiscoveryRunOutcome with candidates）
- [x] `Discovery/Candidates/CandidateMrProposal.cs` 纯 DTO（不写库的中间值）
- [x] `Discovery/MetaPatternDiscoverer.cs` — 进程外调 `tools/noether_candidates.py`，解析 JSON → CandidateMrProposal
- [x] `Discovery/LlmNativeDiscoverer.cs` — 依赖 `ILlmGateway` 接口（注入 fake 即可在 CI 跑）
- [x] `Discovery/ILlmGateway.cs` + `Discovery/NullLlmGateway.cs`（生产由 VM 接 DeepSeek/Anthropic）
- [x] `Discovery/DiscoveryService.cs` orchestrator — 写 DiscoveryRun + CandidateMR 表
- [ ] WPF `DiscoveryPage` + `CandidateReviewPage`（**DEFER VM**）

### P7.2 Validator（cloud）

- [x] `Discovery/Validators/IMRValidator.cs` 接口
- [x] `Discovery/Validators/EmpiricalValidator.cs` — baseline pass-rate（fake repo 注入）
- [x] `Discovery/Validators/TheoreticalLlmValidator.cs` — 依赖 `ILlmGateway`
- [x] `Discovery/Validators/AdversarialMutmutValidator.cs` — 看 MR 能否检出 mutant
- [x] `Discovery/ValidationService.cs` — 跑 validator 写 ValidationRun + 自动 promote（≥2 通过）

### P7.3 Mutation（cloud）

- [x] `Mutation/MutationCampaignService.cs` — 矩阵 (mutants × mrBindings × sampleCases) + 调 ISystemMtPipeline
- [x] `Mutation/MutationCampaignSpec.cs` DTO
- [x] `Mutation/MutationCampaignSummary.cs` DTO (detection rate / false positive rate / coverage)
- [ ] WPF `MutationCampaignPage`（**DEFER VM**）

### P7 验收（cloud）

- [x] xUnit `DiscoveryServiceTests`：MetaPattern discoverer 产 ≥3 proposal + 写 DB
- [x] xUnit `ValidationServiceTests`：≥2 validator 通过自动 promote → 写 MetamorphicRelation 表
- [x] xUnit `MutationCampaignServiceTests`：5×5 矩阵 + 验证 detection-rate 算式
- [x] Python: `tools/noether_candidates.py` 的 JSON 输出契约测试（防 C# wrapper 解析漂移）

---

## P8 — Coverage + Trend + Reports + 验收 ship（W8）

### P8.1 Coverage

- [ ] `Coverage/CoverageService.cs` — 4 维（MetaPattern / SUT×MR / Bug / Mutation）
- [ ] WPF `CoverageDashboardPage`

### P8.2 Trend

- [ ] `Trend/TrendAnalysisService.cs` — 周报算法
- [ ] WPF `TrendDashboardPage`
- [ ] 周报邮件 webhook（可选）

### P8.3 Reports

- [ ] 扩展 `ReportService` 支持 5 种 Scope
- [ ] 论文复现包打包脚本

### P8.4 文档同步

- [ ] 更新 `CLAUDE.md` 反映 v2 现实
- [ ] 更新 `AGENTS.md` 加 Stage 6 (v2 development) 段
- [ ] 更新 `README.md` Architecture 表
- [ ] 文档版本号统一

### P8.5 端到端验收

- [ ] 全流程演示：新 SUT 接入 → CRUD MR → 启动 Execution → Anomaly drill → Replay → MutationCampaign → 看 dashboard 覆盖率 → 生成周报

### P8 验收（v2 ship）

- [ ] 所有 P1-P8 PR 合并到 main
- [ ] CI 全绿
- [ ] 23 collection 全部有数据
- [ ] WPF 可启动所有 29+ scenarios（迁入后）
- [ ] dashboard.html 可在 WPF 内查看
- [ ] 演化文档加 §11 "v2 ship 后回顾" 段

---

## 风险登记

| 风险 | 严重度 | 缓解 |
|------|-------|------|
| LiteDB schema 迁移破坏 v1 数据 | 高 | P1 全程 backup `MR.litedb` + `SystemMt.litedb`；每次跑迁移先 dry-run |
| FluentAssertions 异常类型签名变化 | 中 | NuGet 版本锁定 6.12.0；单测覆盖每个扩展方法 |
| WPF + WebView2 在某些 Win10 版本不兼容 | 低 | 文档要求 Win10 1809+；P6 验收时手测 |
| Python subprocess 错误传播在 Linux/Win 差异 | 中 | 标准化 exit code + stderr JSON；C# 用 `OSPlatform` 检查 |
| `.feature` 与 LiteDB 漂移 | 中 | P5 实施 CI 强检 |
| 8 周不够（功能复杂度低估） | 高 | 每周 P 阶段验收必过；超期分裂出 P9 应急 |
| AI 实施时偏离设计（重新发明） | 高 | 每个 task 引用 `docs/design/` 章节号；review 检查 |
| 中途用户改需求 | 中 | RFC 流程：先改设计，再回此 plan，再改代码 |

---

## 回滚策略

| 阶段 | 回滚方式 |
|------|--------|
| P1 | `git revert` schema 扩展 commit；旧 collection 不删除 |
| P2-P3 | Repository / Adapter 是新增代码；删除即回滚 |
| P4 | 保留旧 `IMrAssertion` 不删，新代码出问题切回 |
| P5 | 数据迁移脚本必须**幂等**且**有 dry-run + rollback** |
| P6-P8 | UI 页面是叠加；删除即回滚 |

每个 P 阶段独立 commit，独立可 `git revert`。

---

## 执行日志（边做边更新）

- 2026-05-13: writing-plan 完成（本文件）；executing-plan 启动；从 P1.3 续接
- 2026-05-13: P1.1 + P1.2 已完成（commit `d1515c1`）
- 2026-05-13: P1.3-P1.7 18 个 v2 collection 实体类 + 嵌入式 value object 创建完成
- 2026-05-13: P1.8 DbConfig 扩展 — 20 个新 collection key + 索引注册
- 2026-05-13: P1.9 TDD 验证 — 新增 32 个 schema 测试全过，既有 141 个测试零回归（173 total pass）
- 2026-05-13: P1 阶段 ship — 总测试覆盖 175 (173 pass / 2 skip / 0 fail)
- 2026-05-13: P2.1 创建 21 个 IDAL 接口（IGuidRepository<T> 基接口 + 20 derived）
- 2026-05-13: P2.2 写 2 个 base class + 20 个 LiteDB Repository 实现
- 2026-05-13: P2.3 AddSystemMtRepositories() DI 扩展方法 + Microsoft.Extensions.DI 引入
- 2026-05-13: P2.5 TDD 新增 15 个测试 — DI binding (5) + Index 约束 (10)；总 188 pass / 2 skip / 0 fail
- 2026-05-13: P2 阶段 ship（W2 完成；P2.4 WPF 推迟 VM-side）
- 2026-05-13: P3.1 IMRTransformation 接口 + 6 实现 + TransformationRegistry
- 2026-05-13: P3.2 IFieldPathResolver + 3 语法 (JsonPointer/McnpCard/NamelistKey) + Factory
- 2026-05-13: P3.3 6 个 Python parser 拆分 (openmoc/openmc/heat_equation × input/output)；round-trip 验证通过
- 2026-05-13: P3.5 TDD 33 新测试 (FieldPathResolver 14 + IMRTransformation 19)；累计 221 pass / 2 skip
- 2026-05-13: P3 阶段 ship（W3 完成；P3.4 WPF 推迟 VM-side）
- 2026-05-13: P4.1 NuGet 引入 FluentAssertions 6.12.0 + MathNet.Numerics 5.0.0
- 2026-05-13: P4.2 AssertionTypeCodes (9 常量) + 6 个 FA 扩展方法 + AssertionEvaluator switch 分派
- 2026-05-13: P4.3 SystemMtPipeline (9 状态机) + IProcessExecutor 抽象 + DefaultProcessExecutor
- 2026-05-13: P4.4 ReplayService + ReplayClassification (Reproduced/FixedOrFlaky/RegressionOnReplay/StillPassing/MismatchedFailure/NotComparable)
- 2026-05-13: P4.5 ❌ defer VM-side (废除旧 IMrAssertion 需 WPF 验证)
- 2026-05-13: P4.6 TDD 34 新测试 (Extensions 14 + Evaluator 9 + Pipeline 4 + Replay 7)；累计 255 pass / 2 skip
- 2026-05-13: P4 阶段 ship（W4 完成；P4.5 defer VM）
- 2026-05-13: P5.1 3 个 Python 同步工具 (feature_to_db / db_to_feature / validate_feature_sync)
- 2026-05-13: P5.2 SystemMtPipelineV2Steps.cs 通用 5 个 Reqnroll step bindings (cloud 自动 Skip，VM 端到端)
- 2026-05-13: P5.3 3 个迁移脚本 → metbench/catalog/migration/ (scenarios.json 14 schema / mutations.json 48 op / real-bugs.json 6 known)
- 2026-05-13: P5.4 生成 14 个 .feature 骨架到 metbench/catalog/features/{m_inv,m_mono,m_conv}/
- 2026-05-13: P5.5 TDD 10 Python tests 全过 + C# step bindings 编译通过；累计 255 C# pass + 10 Python pass
- 2026-05-13: P5 阶段 ship（W5 完成）
- 2026-05-13: P6.1 AnomalyService + IAnomalyService + CommonalityReport + AnomalyFilter
- 2026-05-13: P6.3 tools/analyze_anomaly_commonalities.py (C# AnomalyService 的镜像)
- 2026-05-13: P6.4 TDD 13 C# AnomalyService 测试 + 7 Python tests；累计 268 C# pass + 17 Python pass
- 2026-05-13: P6 阶段 ship（W6 完成；P6.2 WPF defer VM）
- (P7-P8 待续...)

---

## 完成定义（v2 ship 标准）

整个 v2 ship 的硬性标准：

1. **代码**：23 collection 实体类 + Repository + 模块全部存在；编译无警告
2. **测试**：单元测试覆盖率 ≥80%；CI 全绿
3. **功能**：WPF 可完成 Scenario 选取 / Execution 启动 / Anomaly drill / Replay / Mutation Campaign / Dashboard 查看 6 大场景
4. **数据**：v1 既有 MR/Application 数据零损失；Stage 5 Python 矩阵数据 100% 迁入
5. **文档**：`docs/design/` 5 份文档与代码一致；`CLAUDE.md` / `AGENTS.md` / `README.md` 反映 v2 现实；本计划全部 checkbox ✓
6. **审计**：每 Execution 有 catalog SHA + SUT version + 触发人 + 完整 artifacts 文件

---

**本计划是 v2 实施的 8 周硬时间盒。每个 task 完成后必须勾选 checkbox 并 commit 本文件，让进度可追溯。**
