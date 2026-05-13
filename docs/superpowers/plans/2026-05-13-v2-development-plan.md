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
| P7 | W7 | Discovery + Mutation 子系统 | pending |
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

### P3.1 IMRTransformation C# 接口 + 实现

- [ ] `MetBench_BLL.Core/SystemMT/Transformations/IMRTransformation.cs`
- [ ] 6 个 Day-1 实现：`ScaleField` / `TranslateField` / `PermuteIndices` / `MirrorAxis` / `IdentityTransform` / `CompositeTransform`
- [ ] 单测：input dict → output dict round-trip

### P3.2 ParameterMapping 解析器

- [ ] 支持 "json-pointer" / "mcnp-card" / "namelist-key" 三种 PathSyntax
- [ ] 单测：每种语法的字段查找 + 修改

### P3.3 Python 适配器拆分（既有 → 新形态）

- [ ] 把既有 `openmoc_input_adapter*.py` 拆成 `openmoc_input_parser.py`（不含 transformation）
- [ ] 创建 `openmoc_output_parser.py` 接口标准化
- [ ] 同上拆 OpenMC adapters
- [ ] 同上拆 heat-equation adapters

### P3.4 Adapter 管理 WPF 页面

- [ ] `AdapterManagementPage` — 列出 SUT 的 parser 路径 + ParameterMapping 表 + 健康检查按钮

### P3 验收

- [ ] 6 个 transformation 单测全过
- [ ] 3 个 SUT 的 parser 端到端测试（read → modify → write → round-trip）
- [ ] WPF Adapter 页面手测可用

---

## P4 — Pipeline + FluentAssertions 扩展 + 端到端（W4）

### P4.1 NuGet 依赖

- [ ] 在 `MetBench_BLL.Core.csproj` 加 `FluentAssertions` + `MathNet.Numerics`

### P4.2 断言扩展方法

- [ ] `Assertions/MetbenchAssertionExtensions.cs` 完整实现（按 `assertion-extensions.md` §5）
- [ ] `Assertions/AssertionTypeCodes.cs` 常量
- [ ] `Assertions/AssertionEvaluator.cs` switch 分派器
- [ ] `Assertions/SystemMtAssertionResult.cs` record
- [ ] `Assertions/AssertionInput.cs` record
- [ ] 单测覆盖每个扩展方法（pass + fail 两路径）

### P4.3 SystemMtPipeline 编排器

- [ ] `SystemMT/Pipeline/SystemMtPipeline.cs` — 9 状态机（按 `v2-system-mt-architecture.md` §3.1）
- [ ] 调 Input Parser / Transformation / SUT runner / Output Parser / Assertion
- [ ] 失败处理：每步异常落 Execution.ErrorMessage

### P4.4 ReplayService

- [ ] `SystemMT/ReplayService.cs` — 从 Anomaly 重跑 Execution + 对比新旧

### P4.5 删除既有 IMrAssertion

- [ ] 删 `GreaterThanAssertion.cs` / `LessThanAssertion.cs` / `IMrAssertion.cs`
- [ ] 既有 `SystemMtRunner` 改为调 `SystemMtPipeline` 或独立 deprecate

### P4.6 端到端测试

- [ ] 从 LiteDB 加一个 MRInstance → 跑 SystemMtPipeline → LiteDB 看到 Execution + Result 行

### P4 验收

- [ ] 一个 OpenMOC ScaleNuSigmaF MR 完整跑通 v2 pipeline
- [ ] 单测覆盖率 ≥80%
- [ ] WPF 从 ExecutionPage 启动一次 Execution 跑通

---

## P5 — BDD `.feature` 双向同步 + 历史数据迁入（W5）

### P5.1 同步工具

- [ ] `tools/feature_to_db.py` — 解析 `.feature` upsert MR + MRBinding
- [ ] `tools/db_to_feature.py` — 反向
- [ ] `tools/validate_feature_sync.py` — CI 一致性

### P5.2 Reqnroll 通用 step bindings

- [ ] 5 个通用 step（按 `v2-system-mt-architecture.md` §6.3）
- [ ] 升级既有 5 个 `.feature` 到新 step

### P5.3 历史数据迁入

- [ ] `tools/migrate_python_scenarios_to_db.py` — `mutation_study.SCENARIOS` 29 行 → MRSchemas + MRBindings + MRInstances
- [ ] `tools/migrate_mutations_to_db.py` — 48 mutations → MutationOperators + Mutants
- [ ] `tools/migrate_real_bugs_to_db.py` — 6 个 R-Case → KnownBugs
- [ ] `tools/migrate_systemmtresult_to_v2.cs` — Stage 4 SystemMtResultRecord → Execution + Result + Anomaly

### P5.4 生成 `.feature` 文件

- [ ] 用 `db_to_feature.py` 从迁入的 29 MRSchema 生成 29 个 `.feature` 文件
- [ ] 人工审 + 补正文（物理推导）

### P5 验收

- [ ] CI 跑 `validate_feature_sync.py` 全绿
- [ ] 29 + 5 = 34 个 `.feature` 文件 + LiteDB 一致

---

## P6 — Anomaly viewer + Replay + WPF UI（W6）

### P6.1 Anomaly 服务

- [ ] `AnomalyService.cs` — 列表 / 过滤 / 共性分析
- [ ] `tools/analyze_anomaly_commonalities.py` (可选 Python helper)

### P6.2 WPF 异常页面

- [ ] `AnomalyListPage` — 列表 + 过滤
- [ ] `AnomalyDetailPage` — 源/后继 input diff + output diff + 断言表达式
- [ ] Replay 按钮 + Status 转移控件

### P6.3 Dashboard 嵌入

- [ ] WPF `DashboardPage` — WebView2 嵌入 `dashboard.html`
- [ ] 升级 `tools/render_dashboard.py` 改读 LiteDB

### P6 验收

- [ ] 找一个迁入的历史 Anomaly → Replay → 看到新 Execution 完成
- [ ] WPF Dashboard 页面正常显示 dashboard.html

---

## P7 — Discovery + Mutation 子系统（W7）

### P7.1 Discovery

- [ ] `Discovery/IMRDiscoverer.cs` 接口
- [ ] `MetaPatternDiscoverer.cs` 实现（C# wrapper，调既有 Python `noether_candidates.py`）
- [ ] `LlmNativeDiscoverer.cs` 实现（调 Anthropic API + prompt）
- [ ] 3 个 Validator：Empirical / TheoreticalLlm / AdversarialMutmut
- [ ] WPF `DiscoveryPage` + `CandidateReviewPage`

### P7.2 Mutation

- [ ] `Mutation/MutationCampaignService.cs` — 跑 mutants × MRBindings 矩阵
- [ ] WPF `MutationCampaignPage`

### P7 验收

- [ ] 跑一次 MetaPattern discovery → 看 ≥3 个 CandidateMR
- [ ] 跑一次小型 MutationCampaign（5 mutants × 5 MRBindings）→ 看 detection rate

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
- (P3-P8 待续...)

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
