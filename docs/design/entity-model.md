# MetBench v2 数据模型（Entity Model）

> LiteDB schema 完整规格。21 个 collection；遵循 3NF；扩展既有 v1 实体而非替换。
> 术语严格按 [`glossary.md`](glossary.md)。

---

## 1. ER 图总览

```
                                    ┌────────────────┐
                                    │  MetaPattern   │
                                    │ (enum, 8 codes)│
                                    └───────┬────────┘
                                            │ embedded code
                                            ↓
   ┌────────────┐                  ┌─────────────────┐
   │   Domain   │                  │ MetamorphicRel  │ (MR Schema, L2)
   │            │                  │   (v1 扩展)      │
   └─────┬──────┘                  └───────┬─────────┘
         │                                 │ 1:N
         │ N:M                             │
         ↓ via ApplicationDomain           │
   ┌─────────────────┐                     │
   │  Application    │                     │
   │   (v1 扩展)      │  ←──── N:M ────────┤
   │   (= SUT)       │   via MRBinding     │
   └────┬────────────┘                     │
        │ N:1                              │
        ↓                                  │
   ┌──────────┐                            │
   │ Runtime  │                            │
   └──────────┘                            │
                                           │
   ┌────────────────────┐                  │
   │    MRBinding       │ ←────────────────┘
   │  (junction +       │
   │   ParameterMapping │  ← embedded
   │   + tolerance/HP)  │
   └──────────┬─────────┘
              │ 1:N
              ↓
   ┌────────────────────┐
   │    MRInstance      │   ← Execution config (params + sampling + HP override)
   └──────────┬─────────┘
              │ 1:N (一个 Instance 可多次重放)
              ↓
   ┌────────────────────┐
   │    Execution       │   ← status, version snapshot, artifacts dir
   └──────────┬─────────┘
              │ 1:1
              ↓
   ┌────────────────────┐
   │      Result        │   ← source/followup values + assertion outcome
   └──────────┬─────────┘
              │ 0:1
              ↓
   ┌────────────────────┐
   │     Anomaly        │ ←── N:1 ──→ KnownBug
   │  (when failed)     │
   └────────────────────┘

   Discovery 子树
   ┌────────────────────┐
   │ DiscoveryMethod    │
   └──────────┬─────────┘
              │ 1:N
              ↓
   ┌────────────────────┐                ┌──────────────────┐
   │  DiscoveryRun      │ ──── 1:N ───→  │  CandidateMR     │
   └────────────────────┘                └───┬──────────────┘
                                             │ 1:N
                                             ↓
                                       ┌──────────────────┐
                                       │  ValidationRun   │
                                       └──────────────────┘
                                       (passed candidates promote
                                        → new MetamorphicRelation row)

   Mutation 子树
   ┌────────────────────┐                ┌──────────────────┐
   │ MutationOperator   │ ──── 1:N ───→  │     Mutant       │
   └────────────────────┘                └───┬──────────────┘
                                             │ 1:N
                                             ↓
                                       ┌──────────────────┐
                                       │ MutationCampaign │
                                       └───┬──────────────┘
                                           │ 1:N
                                           ↓
                                     ┌──────────────────┐
                                     │ MutationResult   │ ──→ Execution
                                     └──────────────────┘

   批次 + 报告 + 审计
   BatchPlan ─1:N─ Batch ─1:N─ Execution
   Report
   AuditLog
```

---

## 2. 21 个 Collection 完整 schema

### 2.1 既有扩展（3 个）

#### `MetamorphicRelations` — MR Schema (L2)，扩展 v1

```csharp
public class MetamorphicRelation
{
    // ===== v1 既有字段（保留） =====
    [BsonId] public int IdMR { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string Constraint { get; set; } = string.Empty;
    public string OrderOfMR { get; set; } = string.Empty;              // 二元/三元/多元关系
    public RtType RepresentationType { get; set; }                     // 数值/谓词/其他
    public string InputPattern { get; set; } = string.Empty;
    public string OutputPattern { get; set; } = string.Empty;
    public string InputPatterntosympy { get; set; } = string.Empty;
    public string OutputPatterntosympy { get; set; } = string.Empty;
    public byte[]? InputPatternImageData { get; set; }
    public byte[]? OutputPatternImageData { get; set; }
    public string DimensionOfInputPattern { get; set; } = string.Empty;
    public string DimensionOfOutputPattern { get; set; } = string.Empty;
    public string Granularity { get; set; } = string.Empty;
    public string Hierarchy { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;

    // ===== 反模式修正 =====
    [Obsolete("Use MRBindings collection. Kept for v1 read compatibility.")]
    public string ApplicationName { get; set; } = string.Empty;

    // ===== v2 新增字段 =====
    public string Code { get; set; } = string.Empty;                   // "MR-T"
    public string MetaPatternCode { get; set; } = string.Empty;        // m_mono / m_inv / ...
    public string TransformationName { get; set; } = string.Empty;     // IMRTransformation 名称
    public string AssertionTypeCode { get; set; } = string.Empty;      // less / greater / less-noise-aware / ...
    public string ValueName { get; set; } = string.Empty;              // k_eff / max_u / ...
    public bool NoiseAware { get; set; }
    public double ToleranceRel { get; set; }
    public double NoiseMultiplier { get; set; } = 3.0;
    public string FeatureFilePath { get; set; } = string.Empty;        // .feature 视图路径
    public int? DiscoveryRunId { get; set; }                           // → DiscoveryRuns（可空，手动写入则为 null）
    public double? DiscoveryConfidence { get; set; }
    public string DiscoveryMethod { get; set; } = "manual";            // manual / metapattern / llm-native
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string Kind { get; set; } = "method-level";                 // method-level / system-level
}
```

**索引**：
- 复合唯一：`(InputPattern, OutputPattern, ApplicationName)`（v1 既有，保留）
- 新增唯一：`Code`
- 普通：`MetaPatternCode`、`Kind`

#### `Applications` — SUT（v1 扩展）

```csharp
public class Application
{
    // ===== v1 既有字段（保留） =====
    [BsonId] public int IdApplication { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProgrammingLanguage { get; set; } = string.Empty;
    public int LinesOfCode { get; set; }
    public List<ApplicationParameter> InputParameters { get; set; } = new();
    public List<ApplicationParameter> OutputParameters { get; set; } = new();
    public byte[]? Code { get; set; }
    public string CodeName { get; set; } = string.Empty;
    public byte[]? SourceTestCase { get; set; }
    public string SourceTestCaseName { get; set; } = string.Empty;
    public string DOI { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    // ===== 反模式修正 =====
    [Obsolete("Use ApplicationDomains junction. Kept for v1 read compatibility.")]
    public string DomainName { get; set; } = string.Empty;

    // ===== v2 新增字段（系统级 SUT 专用） =====
    public string? Version { get; set; }                               // SUT 自报版本
    public int? RuntimeId { get; set; }                                // → Runtimes
    public string? RunnerEntryPath { get; set; }                       // SUT/<sut>/<sut>_runner.py
    public string? InputParserPath { get; set; }                       // SUT/<sut>/<sut>_input_parser.py
    public string? OutputParserPath { get; set; }                      // SUT/<sut>/<sut>_output_parser.py
    public int? DefaultTimeoutSeconds { get; set; } = 60;
    public int? MaxConcurrentRuns { get; set; } = 1;
    public string Kind { get; set; } = "method-level";                 // method-level / system-level
}

public class ApplicationParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;                   // float / int / string / array
    public string Description { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;            // ">0" / "[0,100]" / ...
    public bool IsRequired { get; set; }
}
```

**索引**：
- 唯一：`Name`（v1 既有）
- 复合唯一：`(Name, ProgrammingLanguage)`（v1 既有）
- 新增：`(Name, Version)` 唯一（同 SUT 多版本共存）
- 普通：`Kind`、`RuntimeId`

#### `Domains` — v1 不变

```csharp
public class Domain
{
    [BsonId] public int IdDomain { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

### 2.2 新增 — 反模式修正与基础设施（3 个）

#### `Runtimes`

```csharp
public class Runtime
{
    [BsonId] public int IdRuntime { get; set; }
    public string Name { get; set; } = string.Empty;                   // "python-openmoc-venv"
    public string Kind { get; set; } = string.Empty;                   // python / matlab / cpp / java / fortran
    public string InvokeTemplate { get; set; } = string.Empty;
    // 例 "{python} {script} --input {input} --output {output}"
    // placeholders: {python}, {binary}, {script}, {input}, {output}
    public Dictionary<string, string> EnvVars { get; set; } = new();
    public string? HealthCheckCommand { get; set; }
    public string? Description { get; set; }
}
```

**索引**：`Name` 唯一。

#### `MRBindings` — M:N 替代 `MetamorphicRelation.ApplicationName` 多值反模式

```csharp
public class MRBinding
{
    [BsonId] public int IdMRBinding { get; set; }
    public int MRId { get; set; }                                      // → MetamorphicRelations.IdMR
    public int ApplicationId { get; set; }                             // → Applications.IdApplication
    public List<ParameterMapping> ParameterMappings { get; set; } = new(); // 嵌入
    public string DefaultSampleCasePath { get; set; } = string.Empty;
    public ToleranceConfig DefaultTolerance { get; set; } = new();
    public SutHyperparams DefaultHyperparams { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime BoundAt { get; set; } = DateTime.UtcNow;
    public string BoundBy { get; set; } = string.Empty;
}

public class ParameterMapping
{
    public string AbstractParamName { get; set; } = string.Empty;      // MR 视角参数名："fuel.temperature"
    public string ConcreteFieldPath { get; set; } = string.Empty;      // SUT input dict 路径
    public string FieldType { get; set; } = "number";                  // number / array / string
    public ValueRange? ValueRange { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string PathSyntax { get; set; } = "json-pointer";           // json-pointer / mcnp-card / namelist-key
    public string? Notes { get; set; }
}

public class ValueRange
{
    public double? Min { get; set; }
    public double? Max { get; set; }
}

public class ToleranceConfig
{
    public bool NoiseAware { get; set; }
    public double ToleranceRel { get; set; }
    public double ToleranceAbs { get; set; }
    public double NoiseMultiplier { get; set; } = 3.0;
}

public class SutHyperparams
{
    public long? Seed { get; set; }
    public int? Particles { get; set; }
    public int? Batches { get; set; }
    public int? MaxIterations { get; set; }
    public string? FitnessFunction { get; set; }
    public Dictionary<string, string>? OtherParams { get; set; }
}
```

**索引**：
- 复合唯一：`(MRId, ApplicationId, DefaultSampleCasePath)`（同一 MR + SUT + 案例只能 1 个 active binding）
- 普通：`MRId`、`ApplicationId`、`IsActive`

#### `ApplicationDomains` — M:N 替代 `Application.DomainName` 多值反模式

```csharp
public class ApplicationDomain
{
    [BsonId] public int IdJunction { get; set; }
    public int ApplicationId { get; set; }
    public int DomainId { get; set; }
}
```

**索引**：复合唯一 `(ApplicationId, DomainId)`。

### 2.3 新增 — MR Instance 与执行（4 个）

#### `MRInstances`

```csharp
public class MRInstance
{
    [BsonId] public int IdInstance { get; set; }
    public int MRBindingId { get; set; }                               // → MRBindings
    public Dictionary<string, string> ParameterOverrides { get; set; } = new();
    // 例 {factor: "1.5"}
    public SamplingSpec? Sampling { get; set; }                        // 仅 sweep / batch 时设
    public SutHyperparams? HyperparamsOverride { get; set; }
    public ToleranceConfig? ToleranceOverride { get; set; }
    public string? SampleCaseOverridePath { get; set; }
    public bool IsReusable { get; set; }                               // true = saved；false = ad-hoc
    public string? Name { get; set; }                                  // 可读名（reusable instance 用）
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

public class SamplingSpec
{
    public string Distribution { get; set; } = "fixed-list";
    // "fixed-list" / "uniform" / "log-uniform" / "normal" / "log-normal"
    public int SampleCount { get; set; }
    public Dictionary<string, object> DistributionParams { get; set; } = new();
    // 例 {min: 0.5, max: 2.0} 或 {values: [1.1, 1.25, 1.5, 1.75, 2.0]}
}
```

**索引**：`MRBindingId`、`(IsReusable, Name)`（reusable 实例查询）。

#### `Executions`

```csharp
public class Execution
{
    [BsonId] public Guid IdExecution { get; set; }                     // 高频生成 — 用 Guid
    public int MRInstanceId { get; set; }                              // → MRInstances
    public Guid? BatchId { get; set; }                                 // → Batches，可空
    public string TriggeredBy { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "queued";
    // queued / parsing-source / transforming / writing-followup
    // / running-source / running-followup / parsing-outputs / asserting
    // / ok / anomaly / error / timeout / cancelled
    public string CatalogVersionSha { get; set; } = string.Empty;
    public string SutVersionSnapshot { get; set; } = string.Empty;
    public string MetbenchVersion { get; set; } = string.Empty;
    public string ArtifactsDirectory { get; set; } = string.Empty;     // 物理路径
    public string? ErrorMessage { get; set; }
}
```

**索引**：
- `MRInstanceId`
- `BatchId`
- `(Status, QueuedAt)`
- `TriggeredBy`
- `SutVersionSnapshot`（趋势分析用）

#### `Results`

```csharp
public class Result
{
    [BsonId] public Guid IdResult { get; set; }
    public Guid ExecutionId { get; set; }                              // → Executions
    public double? SourceValue { get; set; }
    public double? SourceStd { get; set; }
    public double? FollowupValue { get; set; }
    public double? FollowupStd { get; set; }
    public Dictionary<string, double> SourceMetrics { get; set; } = new();   // 完整字典含其他度量
    public Dictionary<string, double> FollowupMetrics { get; set; } = new();
    public bool AssertionPassed { get; set; }
    public string AssertionExpression { get; set; } = string.Empty;
    // 例 "0.50781 < 1.13306 - max(0.0, 0.0)"
    public double? ObservedDelta { get; set; }
    public double? ExpectedThreshold { get; set; }
    public string? FailureReason { get; set; }                         // 来自 FA AssertionFailedException.Message
    public TimeSpan SourceElapsed { get; set; }
    public TimeSpan FollowupElapsed { get; set; }
    public int SourceExitCode { get; set; }
    public int FollowupExitCode { get; set; }
}
```

**索引**：
- `ExecutionId` 唯一
- `AssertionPassed`

#### `Anomalies`

```csharp
public class Anomaly
{
    [BsonId] public Guid IdAnomaly { get; set; }
    public Guid ResultId { get; set; }                                 // → Results
    public string Severity { get; set; } = "minor";                    // noise / minor / major / critical
    public string Category { get; set; } = string.Empty;
    // basin / mc-floor / cross-program / single-point-anomaly / ...
    public int ReplayCount { get; set; }
    public string Status { get; set; } = "new";
    // new / investigating / known / confirmed-bug / false-positive / fixed-upstream
    public string? Notes { get; set; }
    public int? LinkedKnownBugId { get; set; }                         // → KnownBugs
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public string? DiscoveredBy { get; set; }
    public DateTime? LastReplayedAt { get; set; }
}
```

**索引**：
- `ResultId` 唯一
- `(Status, Severity)`
- `LinkedKnownBugId`
- `Category`

### 2.4 新增 — Discovery 子系统（4 个）

#### `DiscoveryMethods`

```csharp
public class DiscoveryMethod
{
    [BsonId] public int IdMethod { get; set; }
    public string Name { get; set; } = string.Empty;                   // "MetaPattern-Structural" / "LLM-Native"
    public string Version { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";                     // 方法专属参数
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
}
```

**索引**：`(Name, Version)` 唯一。

#### `DiscoveryRuns`

```csharp
public class DiscoveryRun
{
    [BsonId] public Guid IdRun { get; set; }
    public int MethodId { get; set; }
    public int? TargetApplicationId { get; set; }                      // 可空（全局识别）
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "running";                    // running / ok / error / cancelled
    public string? InvocationDetails { get; set; }                     // CLI / prompt SHA
    public int CandidatesProduced { get; set; }
}
```

**索引**：`(MethodId, StartedAt)`、`TargetApplicationId`。

#### `CandidateMRs`

```csharp
public class CandidateMR
{
    [BsonId] public Guid IdCandidate { get; set; }
    public Guid DiscoveryRunId { get; set; }
    public string ProposedCode { get; set; } = string.Empty;
    public string ProposedName { get; set; } = string.Empty;
    public string? SuggestedTransformationName { get; set; }
    public string? SuggestedAssertionTypeCode { get; set; }
    public string ProposedValueName { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;              // 一段解释
    public double Confidence { get; set; }                             // 0-1
    public string Status { get; set; } = "pending";
    // pending / validated / promoted / rejected / duplicate
    public int? PromotedToMRId { get; set; }                           // 若 promoted → MetamorphicRelations.IdMR
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**索引**：
- `DiscoveryRunId`
- `Status`
- `PromotedToMRId`

#### `ValidationRuns`

```csharp
public class ValidationRun
{
    [BsonId] public Guid IdValidation { get; set; }
    public Guid CandidateMRId { get; set; }
    public string ValidatorName { get; set; } = string.Empty;
    // "empirical" / "theoretical-llm" / "adversarial-mutmut"
    public DateTime RunAt { get; set; }
    public bool Passed { get; set; }
    public string DetailsJson { get; set; } = "{}";
}
```

**索引**：`CandidateMRId`、`(ValidatorName, Passed)`。

### 2.5 新增 — Mutation 子系统（4 个）

#### `MutationOperators`

```csharp
public class MutationOperator
{
    [BsonId] public int IdOperator { get; set; }
    public string Code { get; set; } = string.Empty;                   // "Mut02-runner-sigt-from-siga"
    public string Category { get; set; } = string.Empty;
    // "runner-level" / "input-adapter-level" / "code-level"
    public string TargetFileType { get; set; } = string.Empty;
    public string ApplicationSpec { get; set; } = string.Empty;
    // patch script / regex / AST rule 的描述
    public string PredictedClass { get; set; } = "semantic";
    // "semantic" / "equivalent" / "pathological"
    public string? RelatedMetaPattern { get; set; }
    public string Description { get; set; } = string.Empty;
}
```

**索引**：`Code` 唯一、`Category`。

#### `Mutants`

```csharp
public class Mutant
{
    [BsonId] public int IdMutant { get; set; }
    public int OperatorId { get; set; }                                // → MutationOperators
    public int? ApplicationId { get; set; }                            // → Applications（可空 = 通用）
    public string AppliedDiff { get; set; } = string.Empty;            // unified diff 或 patch 内容
    public string Status { get; set; } = "active";                     // active / deprecated / experimental
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**索引**：`(OperatorId, ApplicationId)`、`Status`。

#### `MutationCampaigns`

```csharp
public class MutationCampaign
{
    [BsonId] public Guid IdCampaign { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScopeSpecJson { get; set; } = "{}";
    // {mutants: [...], mrBindings: [...], sampleCases: [...]}
    public string CatalogVersionSha { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "running";
    public string? CreatedBy { get; set; }
}
```

**索引**：`(Status, StartedAt)`。

#### `MutationResults`

```csharp
public class MutationResult
{
    [BsonId] public Guid IdMutationResult { get; set; }
    public Guid CampaignId { get; set; }                               // → MutationCampaigns
    public int MutantId { get; set; }                                  // → Mutants
    public int MRBindingId { get; set; }                               // → MRBindings
    public Guid ExecutionId { get; set; }                              // → Executions
    public string Outcome { get; set; } = string.Empty;
    // "detected" / "missed" / "error" / "not-affected"
    public double? ObservedDelta { get; set; }
    public double? ExpectedDelta { get; set; }
    public string? Notes { get; set; }
}
```

**索引**：
- `CampaignId`
- `(MutantId, MRBindingId)` 复合唯一
- `Outcome`

### 2.6 新增 — 已知 bug + 审计 + 批次 + 报告（4 个）

#### `KnownBugs`

```csharp
public class KnownBug
{
    [BsonId] public int IdBug { get; set; }
    public string Code { get; set; } = string.Empty;                   // "R-Case-6"
    public string Title { get; set; } = string.Empty;
    public int? RelatedApplicationId { get; set; }
    public string? UpstreamFixCommit { get; set; }
    public string Status { get; set; } = "open";
    // open / fixed-upstream / fixed-metbench / wontfix
    public string Description { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
```

**索引**：`Code` 唯一、`Status`、`RelatedApplicationId`。

#### `AuditLog`

```csharp
public class AuditLog
{
    [BsonId] public Guid IdLog { get; set; }
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    // execution.start / execution.complete / catalog.update / config.change /
    // mr.promote / anomaly.status-change / ...
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string DetailsJson { get; set; } = "{}";
}
```

**索引**：`(Timestamp, Action)`、`TargetEntityId`。

#### `Batches` + `BatchPlans`

```csharp
public class Batch
{
    [BsonId] public Guid IdBatch { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? PlanId { get; set; }                                   // → BatchPlans，可空（ad-hoc）
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";                     // queued / running / ok / failed / cancelled
}

public class BatchPlan
{
    [BsonId] public int IdPlan { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";
    // {mrBindings: "all" | [...], samplingSpec: {...}, ...}
    public string? Schedule { get; set; }                              // cron expr
    public bool Enabled { get; set; } = true;
}
```

**索引**：
- `Batches.PlanId`、`Batches.(Status, CreatedAt)`
- `BatchPlans.Name` 唯一

#### `Reports`

```csharp
public class Report
{
    [BsonId] public Guid IdReport { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Scope { get; set; } = string.Empty;
    // single-execution / single-campaign / weekly / monthly / ad-hoc / paper-package
    public string? ScopeRefId { get; set; }                            // ExecutionId / CampaignId / 日期范围
    public string Format { get; set; } = "html";                       // html / pdf
    public string ContentPath { get; set; } = string.Empty;            // 渲染产物路径
    public string? Notes { get; set; }
}
```

**索引**：`(Scope, GeneratedAt)`。

---

## 3. Collection 总数与分组

| 分组 | Collection 数 | 列表 |
|------|------------|------|
| **既有扩展** | 3 | MetamorphicRelations, Applications, Domains |
| **基础设施** | 1 | Runtimes |
| **反模式修正** | 2 | MRBindings, ApplicationDomains |
| **MR Instance 与执行** | 4 | MRInstances, Executions, Results, Anomalies |
| **Discovery** | 4 | DiscoveryMethods, DiscoveryRuns, CandidateMRs, ValidationRuns |
| **Mutation** | 4 | MutationOperators, Mutants, MutationCampaigns, MutationResults |
| **已知 bug + 审计 + 批次 + 报告** | 3 | KnownBugs, AuditLog, Batches, BatchPlans, Reports — 算 5 个 |

**实际总数**：3 + 1 + 2 + 4 + 4 + 4 + 5 = **23 个** （修正前述 21 的数字 — BatchPlans 与 Batches 分开计；KnownBugs / AuditLog / Reports 各一个）

正式 collection 注册见 §4。

---

## 4. LiteDB DbConfig 注册（扩展既有 `DbConfig`）

```csharp
public sealed class DbConfig
{
    // ===== v1 既有（保留） =====
    public readonly string MetamorphicRelations_Collection_Key = "MetamorphicRelations";
    public readonly string Applications_Collection_Key = "Applications";
    public readonly string Domains_Collection_Key = "Domains";

    // ===== v2 新增 =====
    public readonly string Runtimes_Key = "Runtimes";
    public readonly string MRBindings_Key = "MRBindings";
    public readonly string ApplicationDomains_Key = "ApplicationDomains";
    public readonly string MRInstances_Key = "MRInstances";
    public readonly string Executions_Key = "Executions";
    public readonly string Results_Key = "Results";
    public readonly string Anomalies_Key = "Anomalies";
    public readonly string DiscoveryMethods_Key = "DiscoveryMethods";
    public readonly string DiscoveryRuns_Key = "DiscoveryRuns";
    public readonly string CandidateMRs_Key = "CandidateMRs";
    public readonly string ValidationRuns_Key = "ValidationRuns";
    public readonly string MutationOperators_Key = "MutationOperators";
    public readonly string Mutants_Key = "Mutants";
    public readonly string MutationCampaigns_Key = "MutationCampaigns";
    public readonly string MutationResults_Key = "MutationResults";
    public readonly string KnownBugs_Key = "KnownBugs";
    public readonly string AuditLog_Key = "AuditLog";
    public readonly string Batches_Key = "Batches";
    public readonly string BatchPlans_Key = "BatchPlans";
    public readonly string Reports_Key = "Reports";

    // 共 3 v1 + 20 v2 = 23 collections
}
```

---

## 5. Repository 模式

每个 collection 一对 Repository（接口 + 实现）：

```
MetBench_IDAL/
├── IMRBindingRepository.cs
├── IRuntimeRepository.cs
├── IMRInstanceRepository.cs
├── IExecutionRepository.cs
├── IResultRepository.cs
├── IAnomalyRepository.cs
├── ...

MetBench_DAL/
├── LiteDbMRBindingRepository.cs
├── LiteDbRuntimeRepository.cs
├── ...
```

复用既有 `IRepository<T>` 基接口约定：

```csharp
public interface IRepository<T>
{
    int Add(T entity);
    bool Update(T entity);
    bool Delete(int id);
    T? GetById(int id);
    ObservableCollection<T> GetAll();
}
```

对 Guid PK 的 Execution/Result/Anomaly/... 提供新基接口：

```csharp
public interface IGuidRepository<T>
{
    Guid Add(T entity);
    bool Update(T entity);
    bool Delete(Guid id);
    T? GetById(Guid id);
    IReadOnlyList<T> GetAll(int? limit = null);
}
```

---

## 6. 关键查询模式

### 6.1 列出某 MR 在所有 SUT 上的 Binding

```csharp
db.GetCollection<MRBinding>("MRBindings")
  .Find(b => b.MRId == mrId && b.IsActive)
  .ToList();
```

### 6.2 列出最近 30 天某 SUT 的异常

```csharp
var since = DateTime.UtcNow.AddDays(-30);
var executions = db.GetCollection<Execution>("Executions")
  .Find(e => e.SutVersionSnapshot.StartsWith(sutName) && e.QueuedAt >= since && e.Status == "anomaly")
  .Select(e => e.IdExecution)
  .ToList();

var results = db.GetCollection<Result>("Results")
  .Find(r => executions.Contains(r.ExecutionId))
  .ToList();

var anomalies = db.GetCollection<Anomaly>("Anomalies")
  .Find(a => results.Select(r => r.IdResult).Contains(a.ResultId))
  .ToList();
```

### 6.3 覆盖率：SUT × MR 矩阵

```csharp
var bindings = db.GetCollection<MRBinding>("MRBindings")
  .Find(b => b.IsActive)
  .ToList();

var matrix = bindings
  .GroupBy(b => new { b.MRId, b.ApplicationId })
  .ToDictionary(g => g.Key, g => g.Count() > 0);

// 覆盖率 = matrix.Count / (MRs.Count * Applications.Count)
```

### 6.4 跨 SUT 同 MR 差分（Mutation 子系统）

```csharp
var pairs = db.GetCollection<MutationResult>("MutationResults")
  .Find(r => r.CampaignId == campaignId)
  .GroupBy(r => r.MutantId)
  .Where(g => g.Select(r => r.MRBindingId).Distinct().Count() >= 2)
  .ToList();

// 分析每个 pair：是否在所有 MR Binding 上 detection 一致
```

---

## 7. 存储估算

| 实体 | 单行大小 | 行数（典型） | 总占用 |
|------|---------|-----------|--------|
| MetamorphicRelations | 5-20 KB（含图像） | 几百 | 数 MB |
| Applications | 几 MB（含 Code zip） | 10-50 | 数百 MB |
| MRBindings | < 2 KB | 几百-几千 | 数 MB |
| Executions | 1-2 KB | 几万-几十万 | < 1 GB |
| Results | 2-5 KB | 同上 | 1-2 GB |
| Anomalies | < 1 KB | 几百 | < 1 MB |
| MutationResults | < 1 KB | 几千 | 几 MB |
| AuditLog | < 1 KB | 几万 | 几十 MB |

**主库目标**：≤ 10 GB；超过触发归档（参见 [`migration-plan.md`](migration-plan.md) §归档策略）。

**Artifacts 文件系统**：每 Execution ~30-100 KB（输入输出 JSON）。1 万 Execution ~ 几 GB；按 yyyy/mm/dd 分目录自动管理。

---

## 8. 跨 LiteDB 文件隔离

| 文件 | 内容 |
|------|------|
| `MR.litedb` | v1 方法级 MT 数据（既有，**不动**） |
| `System-MT.litedb` | v2 系统级 MT 数据（本文档定义的 23 collection） |

两文件用各自隔离的 `BsonMapper`（参见既有 `LiteDbSystemMtResultRepository` 模式）。method-level 与 system-level 互不见面。

---

**本文档与 `glossary.md` 同步维护。每次 schema 变更**：
1. 改本文件实体定义
2. 加 [`migration-plan.md`](migration-plan.md) 迁移脚本
3. 改 `MetBench_Domain/` C# 类
4. 更新 `DbConfig.cs` collection 键 + 索引
5. 跑单元测试 + 回滚验证
