# MetBench v2 系统级 MT 架构设计

> **版本**: 2.0 草案
> **日期**: 2026-05-13
> **状态**: 基线文档；2026-05-24 已按当前 `main` 做有限事实同步
> **目标读者**: 实验室研究人员、平台开发者、协作者

> **当前实现状态注记（2026-05-24）**：
> 1. 运行时已切到 provider-backed catalog 路径：`IMrCatalogProvider` + `ManifestMrCatalogProvider`。
> 2. `ExecutionEvidence` / `V3MrIdRef` / recorder write-through 已进入执行路径。
> 3. Trend 子系统已于 next-stage P0 下线，本文保留的 Trend 相关内容仅作为历史设计背景；不得再把它理解为当前活跃运行时模块。

## 文档结构

本文档是 **入口**。详细规格分到以下子文档：

| 文档 | 内容 |
|------|------|
| [`mr-architecture.md`](mr-architecture.md) | MR 协议层 + 双轨(method/system) + 方程作为函数容器 + L0/L1/L2 算子 + 集合形态边界 |
| [`glossary.md`](glossary.md) | 术语表 — 4 级 MR 语义 + 全部专业术语 |
| [`entity-model.md`](entity-model.md) | ER 图 + 21 个 LiteDB collection 完整 schema |
| [`assertion-extensions.md`](assertion-extensions.md) | FluentAssertions 扩展方法 API 参考 |
| [`migration-plan.md`](migration-plan.md) | 现有 schema 到 v2 的迁移路径与脚本 |

---

## 1. 设计目标

MetBench v2 是**工程级**系统级 metamorphic testing（MT）平台，面向**科研所/高校实验室**的反复持续 MT 工作流。

### 1.1 核心 KPI（按优先级）

1. **测试覆盖完整性**（MR × SUT × params 网格覆盖率）— 主 KPI
2. **真实 bug 检出数**（已知 bug + 未知 bug）— 副 KPI
3. **回归速度**（SUT 改动后多久知道是否破坏 MR）— 三 KPI

### 1.2 业务约束

- **保留 v1 投资**：`MetBench_BLL/` 方法级 MT、`MR.litedb`、Reqnroll、WPF、LiteDB 全部不动
- **C# 编排** + **Python adapter**：C# 是系统级 MT pipeline 编排者；Python 仅做 SUT 文件解析 + LLM 调用
- **LiteDB 持久化**：不引入 SQL Server / PostgreSQL
- **不上服务化栈**：不上 FastAPI / 微服务 / 容器编排
- **WPF 是主 UI**：dashboard.html 嵌 WebView2
- **BDD `.feature` 作为 MR 视图**：几百条 MR 量级承受得起
- **3NF 严格遵守**：实体分离，禁止冗余字段
- **断言系统复用成熟库**：FluentAssertions + Math.NET 扩展方法

---

## 2. 三层架构（系统级 MT）

```
┌──────────────────────────────────────────────────────────────────┐
│  L3 — UI 层（WPF + WebView2）                                       │
│  • SUT / MR / Adapter / Runtime CRUD 页                            │
│  • Execution 启动 + 实时进度页                                       │
│  • Anomaly 列表 + 详情 + 一键重放页                                  │
│  • Coverage Dashboard                                              │
│  • dashboard.html 嵌入页（聚合视图）                                  │
└────────────────────────┬─────────────────────────────────────────┘
                         ↓ in-proc method call
┌──────────────────────────────────────────────────────────────────┐
│  L2 — 业务编排层（MetBench_BLL.Core，C# net8.0）                      │
│                                                                    │
│  ┌─ 核心 pipeline ────────────────────────────────────────┐       │
│  │  SystemMtPipeline (orchestrator)                       │       │
│  │    1. 读 MRInstance + MRBinding + ParameterMapping     │       │
│  │    2. 调 Input Parser (Python subprocess) → dict       │       │
│  │    3. 调 IMRTransformation (C#) → modified dict        │       │
│  │    4. 调 Input Parser write (Python subprocess)        │       │
│  │    5. 调 SUT runner (subprocess via Runtime)           │       │
│  │    6. 调 Output Parser (Python subprocess) → metrics   │       │
│  │    7. 调 AssertionEvaluator (FA extension methods)     │       │
│  │    8. 持久化 Execution + Result + Anomaly              │       │
│  └────────────────────────────────────────────────────────┘       │
│                                                                    │
│  ┌─ 模块（按 §4）─────────────────────────────────────────┐       │
│  │  Runtime / SUT / Adapter / MR / Discovery /            │       │
│  │  Mutation / Anomaly / Replay / Coverage /              │       │
│  │  Reports / Audit                                       │       │
│  └────────────────────────────────────────────────────────┘       │
└────────────────────────┬─────────────────────────────────────────┘
                         ↓ subprocess (Python) + LiteDB
┌──────────────────────────────────────────────────────────────────┐
│  L1 — 数据 + SUT 边界                                                │
│                                                                    │
│  LiteDB                                                            │
│    • System-MT.litedb  (21 collections, 3NF)                       │
│    • MR.litedb         (v1 方法级，不动)                            │
│                                                                    │
│  Artifacts 文件系统                                                  │
│    runtime/artifacts/<yyyy>/<mm>/<dd>/<execution_id>/              │
│                                                                    │
│  Python adapters                                                   │
│    SUT/<sut_name>/<sut_name>_runner.py                             │
│    SUT/<sut_name>/<sut_name>_input_parser.py                       │
│    SUT/<sut_name>/<sut_name>_output_parser.py                      │
│    SUT/<sut_name>/sample/*.in                                      │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. MT Pipeline 核心数据流

### 3.1 Pipeline 状态机

```
┌────────────┐
│  queued    │   MRInstance 入队，分配 Execution.Id
└──────┬─────┘
       ↓
┌────────────┐
│  parsing-  │   ① Input Parser 读 source.in → dict
│  source    │
└──────┬─────┘
       ↓
┌────────────┐
│  transform │   ② IMRTransformation.Apply (C#, 在内存 dict 上)
└──────┬─────┘
       ↓
┌────────────┐
│  writing-  │   ③ Input Parser write dict → followup.in
│  followup  │
└──────┬─────┘
       ↓
┌────────────┐
│  running-  │   ④ subprocess Runtime.invoke(SUT, source.in)
│  source    │      → source.out
│  ↓         │
│  running-  │   ④ subprocess Runtime.invoke(SUT, followup.in)
│  followup  │      → followup.out
└──────┬─────┘
       ↓
┌────────────┐
│  parsing-  │   ⑤ Output Parser 读 source.out + followup.out
│  outputs   │      → source_dict, followup_dict
└──────┬─────┘
       ↓
┌────────────┐
│  asserting │   ⑥ AssertionEvaluator (FA extension methods)
└──────┬─────┘
       ↓
       ├─ Passed ───→ ┌────────┐ persist Result, status=ok
       │              │   ok   │
       │              └────────┘
       └─ Failed ───→ ┌────────┐ persist Result + Anomaly, status=anomaly
                      │ anomaly│
                      └────────┘

         任意一步 throw  →  status=error / timeout
```

### 3.2 关键边界

| 边界 | 谁负责 | 数据形态 |
|------|------|--------|
| 文件 ↔ 内存 dict | **Python Input/Output Parser**（per-SUT） | SUT 原生文件 / dict |
| MR 输入变换 | **C# IMRTransformation**（per-MR-type） | dict → dict |
| 字段路径解析 | **C# 用 ParameterMapping** | abstract name → SUT field path |
| SUT 执行 | **subprocess via Runtime** | SUT 原生文件 |
| 断言 | **C# FluentAssertions 扩展方法** | values → AssertionResult |
| 持久化 | **LiteDB + Artifacts 文件系统** | Execution / Result / Anomaly + 原始文件 |

**核心解耦**：Transformation 在内存 dict 上操作，**不知道 SUT 文件格式**；Parser 解析 SUT 文件，**不知道 MR 含义**。两者通过 dict 解耦。

---

## 4. 模块清单（12 个 C# 模块）

| # | 模块名 | 命名空间 | 职责 |
|---|--------|---------|------|
| M1 | **Runtimes** | `MetBench_BLL.Core.Runtimes` | Runtime 实体 CRUD + 健康检查 |
| M2 | **Suts** | `MetBench_BLL.Core.Suts` | 系统级 Application 管理（v1 Application 扩展） |
| M3 | **Adapters** | `MetBench_BLL.Core.Adapters` | Input/Output Parser 注册 + ParameterMapping 管理 |
| M4 | **MRs** | `MetBench_BLL.Core.MRs` | MR Schema + Binding + Instance 4 级管理 |
| M5 | **Transformations** | `MetBench_BLL.Core.SystemMT.Transformations` | IMRTransformation 实现集合 |
| M6 | **Assertions** | `MetBench_BLL.Core.SystemMT.Assertions` | FluentAssertions 扩展方法 + Evaluator |
| M7 | **Pipeline** | `MetBench_BLL.Core.SystemMT.Pipeline` | SystemMtPipeline 编排器 |
| M8 | **Discovery** | `MetBench_BLL.Core.Discovery` | IMRDiscoverer 接口 + 2 个实现 + Validators |
| M9 | **Mutation** | `MetBench_BLL.Core.Mutation` | MutationOperator + Campaign + Result 分析 |
| M10 | **Anomaly** | `MetBench_BLL.Core.Anomaly` | 异常列表 + 详情 + Replay |
| M11 | **Coverage** | `MetBench_BLL.Core.Coverage` | 多维覆盖率计算 |
每个模块独立 namespace + DI registration + 单元测试，**无横向依赖**（通过 Repository 接口解耦）。
当前运行时额外包含一个收敛中的 catalog/evidence 子主线：`IMrCatalogProvider` /
`ManifestMrCatalogProvider` / `SystemMtExecutionRecorder` / `ExecutionEvidence`。

---

## 5. 关键设计决策（一表汇总）

| 决策 | 选择 | 文档章节 |
|------|------|---------|
| MT 执行编排 | C# | §2-3 |
| Adapter 实现语言 | Python | §3 |
| Adapter 职责拆解 | 2 项：Input/Output **Parser**（格式转换） + **ParameterMapping**（字段映射） | §3.2 |
| **MR 变换在哪里** | **C# Pipeline，不在 Python adapter** | §3.2 |
| 持久化 | LiteDB（21 collections，3NF） | `entity-model.md` |
| MR 描述层级 | 4 级：MetaPattern / MRSchema / MRBinding / MRInstance | `glossary.md` |
| MR 数据库实体 | 扩展既有 `MetamorphicRelation` + 新增 `MRBindings` / `MRInstances` | `entity-model.md` |
| SUT 数据库实体 | 扩展既有 `Application` | `entity-model.md` |
| BDD `.feature` 角色 | MR 人类可读视图，与 LiteDB 双向同步 | §6 |
| 断言系统 | **FluentAssertions 扩展方法**（`BeLessThanWithNoiseFloor` 等） | `assertion-extensions.md` |
| Discovery 子系统 | `IMRDiscoverer` 接口 + 2 个实现（MetaPattern + LLM-Native） + 3 个 Validator | §7 |
| Mutation 子系统 | 4 个新实体（Operator / Mutant / Campaign / Result） | §8 |
| Anomaly 重放 | 同 MRInstance + 同 SUT 版本重跑，自动对比 | §9 |
| Coverage 维度 | MetaPattern / SUT×MR / Bug / Mutation 四维 | §10 |
| 报告 | HTML / PDF，多 Scope（单跑 / Campaign / 周报 / 月报） | §12 |

---

## 6. BDD `.feature` 与 LiteDB 双向同步

```
metbench/catalog/features/
├── m_mono/
│   ├── MR-T-RaiseFuelTemperature.feature
│   ├── MR05-ScaleFuelSigmaT.feature
│   └── ...
├── m_inv/
│   ├── MR01-Rotate90.feature
│   └── ...
├── m_conv/
│   └── MR12-RefineParticles.feature
└── m_cmp/
    └── MR14-CrossProgram.feature
```

### 6.1 `.feature` 模板

```gherkin
@metapattern:m_mono @assertion:less-noise-aware @value:k_eff
@noise_aware:true @tolerance_rel:0.0
Feature: MR-T — RaiseFuelTemperature: k_eff monotonically decreases

  Background:
    The Doppler effect broadens U-238 resonances at higher temperatures,
    increasing absorption and decreasing k_eff. See Lamarsh §6.

  Scenario Outline: Apply MR-T to <sut> with factor <factor>
    Given the MR Schema "MR-T" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "fuel.temperature" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the noise-aware "less" assertion holds on "k_eff"

    Examples:
      | sut                       | sample        | factor |
      | openmoc-prod-2026q2       | pincell.json  | 1.5    |
      | openmc-multigroup-2026q2  | pincell.json  | 1.5    |
```

### 6.2 同步工具

- `tools/feature_to_db.py` — 解析 `.feature` → upsert MRSchema + MRBindings 行
- `tools/db_to_feature.py` — 从 LiteDB → 重新生成 `.feature`（审计 / 离线 review）
- `tools/validate_feature_sync.py` — CI 检查 `.feature` 与 LiteDB 一致

### 6.3 Reqnroll Step Bindings（5 个固定 step 覆盖所有 MR）

```csharp
[Given(@"the MR Schema ""(.*)"" is bound to SUT ""(.*)""")]
[Given(@"the binding uses sample case ""(.*)""")]
[Given(@"the parameter mapping for ""(.*)"" is configured")]
[When(@"the MT pipeline runs with parameter ""(.*)""=""(.*)""")]
[Then(@"the (noise-aware )?""(.*)"" assertion holds on ""(.*)""")]
```

加 100 个新 MR 不需要写 100 个 step binding。

---

## 7. MR 识别子系统

### 7.1 抽象接口

```csharp
public interface IMRDiscoverer
{
    string MethodName { get; }
    string MethodVersion { get; }

    Task<DiscoveryRun> StartAsync(int? targetSutId, DiscoveryConfig config, CancellationToken ct);

    IAsyncEnumerable<CandidateMR> ProposeMRsAsync(DiscoveryRun run, CancellationToken ct);
}
```

### 7.2 Day-1 实现

| 实现 | 输入 | 输出 |
|------|------|------|
| `MetaPatternDiscoverer` | NOETHER 8 个 MP × SUT.InputParameters | 结构化候选 MR |
| `LlmNativeDiscoverer` | SUT 文档 + 接口 schema + 物理知识提示 | LLM 提议候选 MR |

### 7.3 Validation Pipeline

```
CandidateMR → EmpiricalValidator     (跑 5+ baseline 看是否一致成立)
            → TheoreticalValidator   (LLM 反向问"是否物理合理")
            → AdversarialValidator   (注入 mutation 看 MR 是否非 vacuous)
            ↓
       至少 2 个通过 → promote 到 MRs collection
```

---

## 8. 变异生成与分析子系统

### 8.1 实体

```
MutationOperator    变异算子（如 "scatter-transpose"）
Mutant              一次具体应用（含 diff patch）
MutationCampaign    一次活动（mutants × MRBindings × sample cases）
MutationResult      单 cell 结果
```

### 8.2 应用模式

| 模式 | 流程 | 用途 |
|------|------|------|
| **A. 注入式变异**（runner-level） | 复制 runner → 应用 diff → 替换 → 跑 MR → 还原 | Day-1 实现 |
| **B. 静态变异**（source-level） | mutmut / AST 改 SUT 源码 → 重编译 → 跑 MR | 后期 |

### 8.3 跨 SUT 差分分析

```python
for mutant in active_mutants:
    for mr_binding in matched_pair_bindings:  # 同 MRSchema 在 SUT-A vs SUT-B
        run_a = execute(mutant, mr_binding_a)
        run_b = execute(mutant, mr_binding_b)
        # 若 detection 不一致 → 揭示 SUT 实现差异
```

报告 Cohen's κ + 不一致清单。

---

## 9. 异常样本管理

### 9.1 异常列表（WPF AnomalyListPage）

字段：Run ID、MR Code、SUT、参数、Δk%、Severity、Category、Status、Link to KnownBug、Replay button。

过滤：Severity / Status / SUT / MR / 日期 / Category。

### 9.2 异常详情（WPF AnomalyDetailPage）

显示：
- Source / Followup 输入 JSON diff（高亮变化字段）
- Source / Followup 输出值 + std
- 断言表达式 + 阈值 + 观察值
- ★ 邻近 sweep 点（自动取 ±0.1 factor 邻居作辅证）
- ★ "Replay" 按钮

### 9.3 一键重放（ReplayService）

```csharp
public async Task<ReplayResult> ReplayAsync(Guid anomalyId, CancellationToken ct)
{
    // 1. 读 Anomaly → Result.ExecutionId → Execution.MrInstanceId
    // 2. 创建新 Execution (新 Id, status=queued)
    //    用相同 MRInstance + 相同 SUT 版本 + 当前 catalog 版本
    // 3. 走标准 SystemMtPipeline
    // 4. 比对新旧 Result：
    //    一致     → "Reproduced ✓", Anomaly.ReplayCount++
    //    不一致 + SUT 版本变 → "Likely fixed in current SUT"
    //    不一致 + SUT 版本不变 → "⚠ Flaky test, investigate"
}
```

---

## 10. MR 覆盖率（多维度）

主页 KPI 表盘（来自 `CoverageService.ComputeAsync()`）：

| 维度 | 度量 | 数据源 |
|------|------|--------|
| **MetaPattern Coverage** | `count(distinct MR.MetaPatternCode) / 8` | MRs |
| **SUT × MR Coverage** | `count(MRBindings) / (#Suts × #MRSchemas)` | MRBindings 矩阵 |
| **Bug Coverage** | `count(Anomaly.LinkedKnownBugId distinct) / count(KnownBugs)` | Anomalies + KnownBugs |
| **Mutation Coverage** | `mutants detected by ≥1 MR / total mutants` | MutationResults |

每个数字可点开 drill-down，进入相应矩阵热图。

---

## 11. 历史设计残页（Trend 已下线，不代表当前运行时）

> 以下 Trend 片段保留为历史设计记录，帮助解释旧文档 / 旧提交中的命名来源。
> 它们**不再**代表 2026-05-24 `main` 的现行运行时；当前主线已移除 Trend 子系统。

```csharp
public class TrendAnalysisService
{
    // 每周一凌晨 cron 跑
    public async Task<TrendReport> ComputeWeeklyAsync(CancellationToken ct)
    {
        // 1. GROUP BY MrSchemaId, Week → pass rate
        // 2. GROUP BY SutVersionSnapshot, Week → 新增 anomaly 数
        // 3. Anomaly.Status 转移历史
        // 4. 阈值告警：MR.pass_rate < 95% 触发 webhook
    }
}
```

WPF `TrendDashboardPage` 折线 + 表格 + 周报下载（邮件可选）。

---

## 12. 报告子系统

| 报告类型 | Scope | 格式 | 触发 |
|---------|-------|------|------|
| 单次 Execution | ExecutionId | HTML | 完成时自动 |
| MutationCampaign | CampaignId | HTML / PDF | Campaign 结束 |
| 周报 | 时间范围 | HTML / PDF + email | cron 周一凌晨 |
| 月报 | 时间范围 | HTML / PDF | cron 每月 1 号 |
| 论文复现包 | catalog tag + 时间范围 | tar.gz（catalog + artifacts + figures） | 手动触发 |

---

## 13. 非功能需求

| NFR | 量化目标 |
|-----|---------|
| 数据完整性 | LiteDB ACID transaction；critical write 必经 transaction |
| 可审计性 | 任一 Execution 可追溯到当时 catalog SHA + SUT version + 触发人 |
| 可复现性 | Execution 在保留环境下可一键重跑得到一致结果（确定性 SUT 前提） |
| 可扩展性 | 加新 SUT / Runtime / IMRDiscoverer / IMRTransformation / 断言：仅改 ≤3 文件 |
| 响应延迟 | 单 Execution 启动到首个 SUT 调用 < 2s（不含 SUT 自身耗时） |
| 并发 | LiteDB WAL 模式：5+ 客户端读 / 1 写无冲突 |
| 存储容量 | 单 LiteDB ≤ 10 GB；超过触发归档脚本 |
| 部署 | 单 Windows 工作站或 Linux 服务器，30 min 装完 |
| 离线 | 不强制网络（LLM 识别可降级关闭） |

---

## 14. 实施路线（8 周）

| 周 | 阶段 | 输出 |
|----|------|------|
| 1 | **P1** — LiteDB schema 设计 + 21 collection 迁移脚本 | DB 可用 |
| 2 | **P2** — Entity CRUD + Repository + Foundation 模块（Runtime/SUT/SampleCase） | 数据层完成 |
| 3 | **P3** — Adapter 模块（含 ParameterMapping） + Transformations C# 实现 | 内核组件 |
| 4 | **P4** — Pipeline 编排器 + 断言扩展方法 + AssertionEvaluator | 端到端走通 1 个 MR |
| 5 | **P5** — `.feature` ↔ DB 双向同步 + Reqnroll 通用 step bindings + Phase-2/3 既有 29 MR 迁入 | BDD 闭环 |
| 6 | **P6** — Anomaly viewer + Replay + WPF UI | 异常调查可视化 |
| 7 | **P7** — Discovery (MetaPattern + LLM-Native) + Validators + Mutation 子系统 | 完整功能 |
| 8 | **P8** — Coverage + Trend + Reports + 文档同步 + 验收 | 平台 ship |

详细工时分解见 [`migration-plan.md`](migration-plan.md)。

---

## 15. 明确不做（避免过度设计）

| 不做 | 原因 |
|------|------|
| SQL Server / PostgreSQL | LiteDB 在几百 MR / 万级 Execution 量级足够 |
| FastAPI / 微服务 / gRPC | 单进程 C# 编排即可 |
| Python 拥有 workflow 控制 | 违 §1.2 业务约束 |
| MR DSL（自定义语法） | Gherkin 已是标准 |
| 自动 schema migration framework | LiteDB schema 简单，手写 migration 脚本即可 |
| 通用 ORM 抽象层 | LiteDB API 已清晰 |
| Plugin DLL 热加载 | DI 静态注册足够 |
| AST 级 mutation framework | mutmut（Python SUT）或 patch/regex 即可 |
| Web 前端框架（React / Vue） | dashboard.html 静态够 |
| 多用户 RBAC | 实验室内信任 |
| 实时 push（WebSocket / SignalR） | dashboard 主动 refresh 够 |

---

## 16. 风险登记

| 风险 | 严重度 | 缓解 |
|------|-------|------|
| Python subprocess 错误传播不畅 | 中 | 标准化 exit code + stderr JSON marker（见 `glossary.md` §7） |
| LiteDB schema 演化破坏既有数据 | 高 | 每次 schema 变更需 migration 脚本 + 单元测试 + 回滚路径 |
| `.feature` 与 LiteDB 漂移 | 中 | CI 跑 `validate_feature_sync.py` |
| Mutation campaign 占满 SUT 资源 | 中 | Semaphore 限流 + 优先级队列 |
| LLM-Native discovery 幻觉 | 中 | 强制 2 个以上 validator 通过才进 catalog |
| WPF + Python 双语 onboarding 门槛 | 低 | 文档 + adapter 脚手架生成器 |

---

## 17. 文档维护约定

- **术语严格按 `glossary.md`** — 任何文档 / 代码 / UI 标签使用错误术语 = PR review 必驳
- **Schema 改动必须同步 `entity-model.md`** + 提供 migration script
- **新增 IMRTransformation / IMRDiscoverer / 断言扩展方法** 必须在 `assertion-extensions.md` 或对应 spec 文档里加 API 参考
- **重大架构变更**（如增减模块、改 collection 数量）需要 RFC PR
- **本文档版本号** 与 CLAUDE.md / AGENTS.md 中提及的版本号同步

---

## 18. 参考与延伸阅读

- Chen, T. Y. et al. (1998). Metamorphic Testing
- Pham et al. (2026). NOETHER framework for MR identification
- 现有 v1 实体定义：`MetBench_Domain/MetamorphicRelation.cs`、`Application.cs`、`Domain.cs`
- 现有 Stage 4 持久化：`MetBench_BLL.Core/SystemMT/Persistence/SystemMtResultRecord.cs`
- 历史演化复盘：上下文对话历史中"MetBench 设计演化复盘"段落

---

**本文档是 v2 开发基线**。后续 PR 必须引用本文档的章节号或子文档；任何与本文档相左的实现需 RFC 流程。
