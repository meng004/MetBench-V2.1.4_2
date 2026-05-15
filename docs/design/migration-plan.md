# MetBench v2 迁移计划

> 从 Stage 4 + Stage 5 Python 矩阵的现状，迁移到 v2 设计的执行路径。
> 增量、可 ship、可回滚。每个阶段独立 PR。
> **核心原则**：v1 数据零损失；过渡期新旧并存。

---

## 1. 迁移范围

| 来源 | 目标 |
|------|------|
| v1 `MR.litedb`（方法级 MT） | **保持原样**，不动 |
| Stage 4 `SystemMt.litedb` `SystemMtResultRecord` 集合 | 转 v2 `Execution + Result + Anomaly` 三表 |
| `MetBench_BLL.Core/SystemMT/Launcher/SystemMtScenarioLauncher.BuildScenarios` 硬编码 5 个 scenarios | 转 v2 `MetamorphicRelations + Applications + MRBindings + MRInstances` 数据驱动 |
| `tools/mutation_study.py::SCENARIOS` 29 个 dict | 转 v2 `MRSchemas + MRBindings + MRInstances` 数据 |
| `tools/mutations.py` 48 mutations | 转 v2 `MutationOperators + Mutants` 数据 |
| `tools/noether_candidates.py::METAPATTERN_TABLE` | 转 `DiscoveryMethods` 数据 |
| `docs/experiments/_data/*.json` 历史矩阵结果 | 选择性导入到 v2 `Executions/Results` 作历史参考 |
| 既有 `.feature` 文件 5 个 | 保留 + 升级到 v2 generic step bindings |

---

## 2. 阶段路线（8 周）

每阶段 = 一个 PR；每个 PR 独立通过 CI；中间任一阶段失败可回滚不影响生产。

### 阶段 P1（第 1 周）— LiteDB schema 扩展

**目标**：DB 层就绪，旧应用仍能读旧字段。

**任务清单**：
- [ ] 扩展 `MetamorphicRelation`（加 v2 字段，默认值兼容）
- [ ] 扩展 `Application`（加 v2 字段，默认值兼容）
- [ ] 新增 18 个 collection 的 C# class（`Runtime`, `MRBinding`, `MRInstance`, `Execution`, `Result`, `Anomaly`, `DiscoveryMethod`, `DiscoveryRun`, `CandidateMR`, `ValidationRun`, `MutationOperator`, `Mutant`, `MutationCampaign`, `MutationResult`, `KnownBug`, `AuditLog`, `Batch`, `BatchPlan`, `Report`, `ApplicationDomain`）
- [ ] 扩展 `DbConfig`：注册新 collection key + 索引
- [ ] 单元测试：每个 collection insert/read/delete

**Migration script**：`tools/migrate_v1_to_v2_schema.cs`

```csharp
public class V1ToV2SchemaMigration
{
    public static void Run(LiteDatabase db)
    {
        // 1. 既有 MetamorphicRelation 字段不缺失就 OK（C# 新字段读出来是默认值）
        EnsureV2FieldsHaveDefaults(db.GetCollection<MetamorphicRelation>("MetamorphicRelations"));

        // 2. 既有 Application 同理
        EnsureV2FieldsHaveDefaults(db.GetCollection<Application>("Applications"));

        // 3. 拆分 ApplicationName 多值字符串 → MRBindings collection
        SplitApplicationNameToMRBindings(db);

        // 4. 拆分 DomainName 多值字符串 → ApplicationDomains collection
        SplitDomainNameToApplicationDomains(db);

        // 5. 旧 SystemMtResultRecord 展开 → Executions + Results
        MigrateSystemMtResultRecords(db);

        // 6. 写 migration audit log
        db.GetCollection<AuditLog>("AuditLog").Insert(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = "system",
            Action = "schema.migrate.v1-to-v2",
            DetailsJson = JsonSerializer.Serialize(new { fromVersion = "v1", toVersion = "v2" })
        });
    }

    private static void SplitApplicationNameToMRBindings(LiteDatabase db)
    {
        var mrs = db.GetCollection<MetamorphicRelation>("MetamorphicRelations").FindAll().ToList();
        var apps = db.GetCollection<Application>("Applications").FindAll().ToList();
        var bindings = db.GetCollection<MRBinding>("MRBindings");

        foreach (var mr in mrs)
        {
            if (string.IsNullOrWhiteSpace(mr.ApplicationName)) continue;
            foreach (var appName in mr.ApplicationName.Split(':', StringSplitOptions.RemoveEmptyEntries))
            {
                var app = apps.FirstOrDefault(a => a.Name == appName.Trim());
                if (app is null)
                {
                    Console.WriteLine($"[migrate] MR {mr.Code} references missing app '{appName}', skipped");
                    continue;
                }
                bindings.Insert(new MRBinding
                {
                    MRId = mr.IdMR,
                    ApplicationId = app.IdApplication,
                    ParameterMappings = new List<ParameterMapping>(),   // 待人工补
                    DefaultSampleCasePath = "",                          // 待人工补
                    DefaultTolerance = new ToleranceConfig(),
                    DefaultHyperparams = new SutHyperparams(),
                    IsActive = true,
                    BoundAt = DateTime.UtcNow,
                    BoundBy = "migrate-v1-to-v2"
                });
            }
        }
    }

    private static void MigrateSystemMtResultRecords(LiteDatabase db)
    {
        var oldCol = db.GetCollection<SystemMtResultRecord>("SystemMtResults");
        var executionsCol = db.GetCollection<Execution>("Executions");
        var resultsCol = db.GetCollection<Result>("Results");
        var anomaliesCol = db.GetCollection<Anomaly>("Anomalies");

        foreach (var record in oldCol.FindAll())
        {
            var execId = Guid.NewGuid();
            var resultId = Guid.NewGuid();

            executionsCol.Insert(new Execution
            {
                IdExecution = execId,
                MRInstanceId = -1,                                       // 历史数据无 instance 引用
                TriggeredBy = "legacy-system-mt",
                QueuedAt = record.RunAt.UtcDateTime,
                StartedAt = record.RunAt.UtcDateTime,
                FinishedAt = record.RunAt.UtcDateTime.Add(record.SourceElapsed + record.FollowUpElapsed),
                Status = record.Passed ? "ok" : "anomaly",
                CatalogVersionSha = "(legacy-unknown)",
                SutVersionSnapshot = "(legacy-unknown)",
                MetbenchVersion = "stage4-legacy",
                ArtifactsDirectory = string.Empty
            });

            resultsCol.Insert(new Result
            {
                IdResult = resultId,
                ExecutionId = execId,
                SourceValue = record.SourceValue,
                FollowupValue = record.FollowUpValue,
                AssertionPassed = record.Passed,
                AssertionExpression = $"({record.AssertionName}) on '{record.ValueName}'",
                FailureReason = record.FailureReason,
                SourceMetrics = record.SourceMetrics,
                FollowupMetrics = record.FollowUpMetrics,
                SourceElapsed = record.SourceElapsed,
                FollowupElapsed = record.FollowUpElapsed,
                SourceExitCode = record.SourceExitCode,
                FollowupExitCode = record.FollowUpExitCode
            });

            if (!record.Passed)
            {
                anomaliesCol.Insert(new Anomaly
                {
                    IdAnomaly = Guid.NewGuid(),
                    ResultId = resultId,
                    Severity = "minor",
                    Category = "legacy",
                    Status = "known",
                    Notes = $"Migrated from SystemMtResultRecord {record.Id}; original FailureReason: {record.FailureReason}",
                    DiscoveredAt = record.RunAt.UtcDateTime
                });
            }
        }
    }
}
```

**回滚**：保留旧 `SystemMtResultRecord` 集合不删（标 obsolete 但读取兼容）；v2 新表均可独立 drop。

---

### 阶段 P2（第 2 周）— Repository + 基础设施模块

**目标**：CRUD 层完备，新建实体页 + 列表页可用。

**任务清单**：
- [ ] 新建 Repository 接口（`IRuntimeRepository` / `IMRBindingRepository` / ...）共 20 个
- [ ] LiteDB 实现（`LiteDb<Entity>Repository`）共 20 个
- [ ] DI 注册（`App.xaml.cs` 加 20 个 `services.AddScoped<...>()`）
- [ ] WPF 页面：`RuntimeManagementPage` / `SutManagementPage` / `SampleCaseManagementPage`（≤ 200 行 XAML+VM 每对）

**测试**：每 Repository 跑端到端 add/update/delete/get；至少 60 个测试。

---

### 阶段 P3（第 3 周）— Adapter + ParameterMapping + Transformations

**目标**：MR 输入变换内核可用。

**任务清单**：
- [ ] 新建 `IMRTransformation` C# 接口 + 6 个实现（`ScaleField` / `TranslateField` / `PermuteIndices` / `MirrorAxis` / `IdentityTransform` / `CompositeTransform`）
- [ ] `ParameterMapping` 嵌入 record 完整定义
- [ ] WPF `AdapterManagementPage`：列出 SUT 的 InputParser/OutputParser 路径 + ParameterMapping 列表 + 健康检查按钮
- [ ] Python 端：把既有 `SUT/openmoc/openmoc_input_adapter*.py` 拆分：
  - `openmoc_input_parser.py`（纯文件 IO，read/write JSON）
  - **不再有** `openmoc_input_adapter_<mr>.py`（变换逻辑由 C# 端 IMRTransformation 接管）

**单元测试**：
- 6 个 `IMRTransformation` 实现的 input dict → output dict 测试
- `openmoc_input_parser.read(file) → dict → write(dict, file)` round-trip

---

### 阶段 P4（第 4 周）— Pipeline + Assertion 扩展方法 + 端到端单 MR 走通

**目标**：完整 pipeline 跑通 1 个 MR Execution。

**任务清单**：
- [ ] FluentAssertions + Math.NET NuGet 加入
- [ ] `MetbenchAssertionExtensions.cs` 写完（见 [`assertion-extensions.md`](assertion-extensions.md)）
- [ ] `AssertionEvaluator.cs` 写完
- [ ] `SystemMtPipeline` 编排器写完（C# 类，~300 行）
- [ ] `ReplayService` 写完
- [ ] 删除既有 `IMrAssertion` / `GreaterThanAssertion` / `LessThanAssertion`（保留旧 SystemMtRunner 不动）

**验收测试**：从 WPF 启动一次 OpenMOC MR-NuSigmaF Execution，查询 LiteDB 看到 Execution + Result 行。

---

### 阶段 P5（第 5 周）— BDD `.feature` 双向同步 + Phase 2/3 数据迁入

**目标**：所有 v1+v2 MR 数据进 LiteDB；`.feature` 文件可一致同步。

**任务清单**：
- [ ] `tools/feature_to_db.py`：解析 `.feature` upsert MRSchema + MRBindings
- [ ] `tools/db_to_feature.py`：反向
- [ ] `tools/validate_feature_sync.py`：CI 一致性检查
- [ ] Reqnroll 通用 step bindings 5 个（见 `v2-system-mt-architecture.md` §6.3）
- [ ] 既有 5 个 `.feature` 文件升级到新 step bindings 形式
- [ ] **数据迁移**：`tools/migrate_python_scenarios_to_db.py` — 把 `mutation_study.SCENARIOS` 29 行 dict 转 `MRSchemas + MRBindings + MRInstances` 数据
- [ ] **数据迁移**：把 Phase 1-3 真实 R-Case (6 个) 迁入 `KnownBugs` collection

**MRSchemas 与 `.feature` 一一对应**生成 200-300 个 `.feature` 文件骨架（脚本生成 + 人工填充正文）。

---

### 阶段 P6（第 6 周）— Anomaly + Replay + 可视化

**目标**：异常调查工作流完整。

**任务清单**：
- [ ] WPF `AnomalyListPage` + `AnomalyDetailPage`
- [ ] `AnomalyService`（共性分析、状态机转移）
- [ ] `ReplayService` 接入 UI 按钮
- [ ] dashboard.html 升级：从 Python `tools/render_dashboard.py` 改为读 LiteDB + 嵌入 WebView2

**验收**：找一个历史 anomaly，从 WPF 点 Replay → 看到新 Execution 完成 → 系统自动标 "Reproduced ✓"。

---

### 阶段 P7（第 7 周）— Discovery + Mutation 子系统

**目标**：MR 识别与变异系统就绪。

**任务清单**：
- [ ] `IMRDiscoverer` 接口
- [ ] `MetaPatternDiscoverer` C# 实现（调既有 `tools/noether_candidates.py` 作 Python backend）
- [ ] `LlmNativeDiscoverer` C# 实现（调 LLM API；prompt 模板 + dedup）
- [ ] 3 个 Validator：`EmpiricalValidator`、`TheoreticalLlmValidator`、`AdversarialMutmutValidator`
- [ ] `MutationCampaignService`：跑 mutants × MRBindings 矩阵
- [ ] WPF `DiscoveryPage` + `CandidateReviewPage` + `MutationCampaignPage`
- [ ] **数据迁移**：`tools/migrate_mutations_to_db.py` — `tools/mutations.py` 48 mutations → `MutationOperators + Mutants` 数据

---

### 阶段 P8（第 8 周）— Coverage + Trend + Reports + 验收

**目标**：平台 ship。

**任务清单**：
- [ ] `CoverageService` + WPF `CoverageDashboardPage`
- [ ] `TrendAnalysisService` + WPF `TrendDashboardPage` + 周报邮件
- [ ] `ReportService` 扩展：支持 single-execution / single-campaign / weekly / monthly / paper-package 五种 Scope
- [ ] 文档同步：`CLAUDE.md` / `AGENTS.md` / `README.md` / `docs/design/glossary.md` 全部反映 v2 现实
- [ ] 端到端验收：
  1. WPF 启动一个新 SUT 接入（OpenMOC mock）
  2. CRUD 一个新 MRSchema
  3. 自动绑定到 SUT
  4. 启动 Execution 跑通
  5. Anomaly 出现 → drill-down → Replay
  6. 跑 MutationCampaign
  7. 看 dashboard 覆盖率四个表盘
  8. 生成周报

---

## 3. 数据迁移脚本一览

| 脚本 | 来源 | 目标 |
|------|------|------|
| `tools/migrate_v1_to_v2_schema.cs` | v1 `MR.litedb` 字段填充 | v2 字段默认值 |
| `tools/migrate_application_name_split.cs` | `MR.ApplicationName` ":" 多值 | `MRBindings` 行 |
| `tools/migrate_domain_name_split.cs` | `App.DomainName` ":" 多值 | `ApplicationDomains` 行 |
| `tools/migrate_systemmtresult_to_v2.cs` | Stage 4 `SystemMtResults` 集合 | `Executions + Results + Anomalies` |
| `tools/migrate_python_scenarios_to_db.py` | `mutation_study.SCENARIOS` 29 行 | `MRSchemas + MRBindings + MRInstances` |
| `tools/migrate_mutations_to_db.py` | `tools/mutations.py` 48 mutations | `MutationOperators + Mutants` |
| `tools/migrate_real_bugs_to_db.py` | `docs/experiments/bug-inventory.md` R-Case-1..6 | `KnownBugs` |
| `tools/migrate_features_to_db.py`（= `feature_to_db.py`） | `metbench/catalog/features/*.feature` | `MRSchemas + MRBindings` upsert |
| `tools/migrate_history_data.py` | `docs/experiments/_data/*.json` | 选择性 `Executions + Results` 历史参考 |

**所有 migration 脚本必须**：
- 幂等（运行多次结果相同）
- 包含 `--dry-run` 模式
- 写 `AuditLog` 记录
- 提供回滚脚本

---

## 4. 归档策略（长期运营）

**触发条件**：`runtime/metbench.db` > 10 GB 或月度归档定时器。

**步骤**：

```bash
# 每月 1 号 02:00 cron
metbench archive \
  --before 2026-01-01 \
  --to /archive/2025/ \
  --keep-anomalies \              # 异常永不归档
  --keep-known-bug-related
```

**归档行为**：
1. Executions/Results 超过 N 天导出 Parquet
2. Anomalies、KnownBugs、MRSchemas、MRBindings 永不归档
3. artifacts 文件夹按月打 tar.zst
4. SQLite `VACUUM` 收缩主库

**归档目录结构**：
```
/archive/
├── 2025/
│   ├── 2025-01/
│   │   ├── executions.parquet
│   │   ├── results.parquet
│   │   └── artifacts-2025-01.tar.zst
│   └── ...
```

---

## 5. 风险与缓解（迁移期专属）

| 风险 | 严重 | 缓解 |
|------|------|------|
| 迁移脚本破坏 v1 数据 | 高 | 每次跑前 backup `MR.litedb` + `SystemMt.litedb`；保留 7 天 |
| ApplicationName 多值拆分时找不到 Application | 中 | 跳过 + 日志告警；不阻塞 |
| `.feature` 与 LiteDB 漂移 | 中 | CI 必跑 `validate_feature_sync.py` |
| 历史 Python scenarios 迁过去后参数映射不全 | 中 | 自动留空 `ParameterMappings`，人工 review 后补 |
| Reqnroll step binding 改动破坏既有 5 个 `.feature` | 中 | 旧 `.feature` 加版本注解；新 step 兼容旧语法过渡期 |
| 大量历史 SystemMtResultRecord 迁过去后查询变慢 | 低 | 索引覆盖 + 限制 limit；超 10 GB 触发归档 |
| 团队成员忘记新术语 | 中 | PR review checklist 含术语检查项；CLAUDE.md 明文 |
| LLM-Native Discovery API key 泄漏 | 高 | `.env` 模板 + `.gitignore` 严守；密钥定期轮换 |

---

## 6. 验收 checklist（每阶段）

每个 P 阶段 PR 合并前必过：

- [ ] CI 全绿（`dotnet test` + `dotnet build` + lint）
- [ ] 单元测试新增 ≥ 10 个（针对本阶段产出）
- [ ] 文档同步：术语、ER 图、API 参考更新
- [ ] 数据迁移脚本 dry-run + 实际跑 + 回滚演练
- [ ] WPF UI 新页面手动测试（如有）
- [ ] AuditLog 新增 action 记录
- [ ] 至少 1 人 review

---

## 7. 时间盒（hard deadline）

| 阶段 | 起 | 止 | 备注 |
|------|---|---|------|
| P1 | W1 Mon | W1 Fri | DB 层 |
| P2 | W2 Mon | W2 Fri | Repository + 基础设施 |
| P3 | W3 Mon | W3 Fri | Adapter + Transformation |
| P4 | W4 Mon | W4 Fri | Pipeline + 断言 |
| P5 | W5 Mon | W5 Fri | BDD + 历史数据迁入 |
| P6 | W6 Mon | W6 Fri | Anomaly + Replay |
| P7 | W7 Mon | W7 Fri | Discovery + Mutation |
| P8 | W8 Mon | W8 Fri | Coverage + Trend + 验收 ship |

**8 周硬时间盒**。超出按风险评估决定是否分裂出 P9 应急阶段。

---

## 8. 文档维护责任

| 阶段 | 必须更新的文档 |
|------|-------------|
| P1 | `entity-model.md`（schema）+ `glossary.md`（新术语） |
| P2 | `entity-model.md` §5（Repository 模式） |
| P3 | `glossary.md` §2（Parser / Mapping 命名） |
| P4 | `assertion-extensions.md`（API） + 主架构文档 §3.1（pipeline） |
| P5 | `v2-system-mt-architecture.md` §6（BDD 同步） |
| P6 | 主架构文档 §9（Anomaly） |
| P7 | 主架构文档 §7、§8（Discovery、Mutation） |
| P8 | `CLAUDE.md`、`AGENTS.md`、`README.md` 全面同步 |

---

**本迁移计划与 [`v2-system-mt-architecture.md`](v2-system-mt-architecture.md) §14 实施路线对应。任何阶段调整需先改本文件 PR。**
