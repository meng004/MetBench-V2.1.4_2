# MetBench v2 架构图与类图

> 用 Mermaid 渲染。在 GitHub / VS Code Markdown Preview / mermaid.live 都可直接渲染。
> 配套文档：[`v2-system-mt-architecture.md`](v2-system-mt-architecture.md) +
> [`entity-model.md`](entity-model.md) + [`glossary.md`](glossary.md)。

---

## 1. 整体架构图（部署层视图）

```mermaid
flowchart TB
    subgraph L3["L3 — 表现层 (Windows only)"]
        direction LR
        WPF["MetBench_Client (WPF)<br/>• SUT/MR/Adapter 管理页<br/>• Execution 启动 + 进度<br/>• Anomaly 列表/详情/Replay<br/>• Coverage + Trend Dashboard"]
        WebView2["WebView2 嵌入<br/>dashboard.html (Plotly)"]
        WPF -.embeds.-> WebView2
    end

    subgraph L2["L2 — 业务编排层 (MetBench_BLL.Core, net8.0 跨平台)"]
        direction TB
        Pipeline["SystemMtPipeline<br/>9 状态机"]
        Discovery["Discovery 子系统<br/>(IMRDiscoverer + 2 实现 + 3 Validator)"]
        Mutation["Mutation 子系统<br/>(Campaign + Result 分析)"]
        Replay["ReplayService"]
        Coverage["Coverage / Trend / Reports"]
        Pipeline --> |feeds| Replay
        Pipeline --> |feeds| Mutation
    end

    subgraph L1Data["L1 — 数据层"]
        direction LR
        LiteDB[("LiteDB<br/>23 collections (3NF)<br/>MR/Application/Runtime/<br/>Execution/Result/Anomaly/<br/>Mutation/Discovery/...")]
        FS[("Artifacts 文件系统<br/>runtime/artifacts/<br/>yyyy/mm/dd/&lt;exec_id&gt;/")]
    end

    subgraph L1SUT["L1 — SUT 边界"]
        direction LR
        Python["Python Parsers + Runners<br/>SUT/openmoc/<br/>SUT/openmc/<br/>SUT/heat_equation/"]
        SUTs["实际 SUT 进程<br/>OpenMOC / OpenMC /<br/>heat_equation / 未来 MATLAB/C++/<br/>Java/Fortran"]
        Python -->|subprocess| SUTs
    end

    WPF -->|in-proc call| L2
    L2 -->|Repository CRUD| LiteDB
    L2 -->|read/write artifacts| FS
    L2 -->|subprocess invoke| Python

    style WPF fill:#dde7f7
    style Pipeline fill:#fde6c4
    style LiteDB fill:#d6f5dd
    style Python fill:#fbe5e5
```

---

## 2. 模块清单（C# Namespace 视图）

```mermaid
flowchart LR
    subgraph WPFLayer["MetBench_Client (WPF)"]
        Pages["Views/Pages/*.xaml"]
        VM["ViewModels/*.cs"]
        Pages --> VM
    end

    subgraph BLLCore["MetBench_BLL.Core (net8.0 跨平台)"]
        direction TB
        subgraph SystemMT["SystemMT/*"]
            Assertions["Assertions/<br/>AssertionEvaluator +<br/>6 FA 扩展方法"]
            PipelineNs["Pipeline/<br/>SystemMtPipeline + Context +<br/>IProcessExecutor + ReplayService"]
            Transformations["Transformations/<br/>IMRTransformation +<br/>6 实现 + Registry"]
            PathMapping["ParameterMapping/<br/>IFieldPathResolver +<br/>JsonPointer/Mcnp/Namelist"]
        end
        Modules["Discovery/ Mutation/ Coverage/<br/>Trend/ Reports/ ...<br/>(P6-P8)"]
    end

    subgraph DAL["MetBench_DAL (LiteDB 实现)"]
        DbConfig["DbConfig 单例<br/>23 collection 注册"]
        Repos["V2/LiteDb*Repository<br/>(20 个 v2 + 3 个 v1)"]
        DIExt["ServiceCollectionExtensions<br/>AddSystemMtRepositories()"]
    end

    subgraph IDAL["MetBench_IDAL (接口)"]
        IRepo["IRepository&lt;T&gt; (int PK)"]
        IGuidRepo["IGuidRepository&lt;T&gt; (Guid PK)"]
        ISpecific["20 个实体专属接口"]
    end

    subgraph Domain["MetBench_Domain (实体)"]
        V1Ent["v1: MetamorphicRelation /<br/>Application / Domain<br/>(扩展了 v2 字段)"]
        V2Ent["V2/: 18 个新实体 +<br/>5 个 value object"]
    end

    WPFLayer --> BLLCore
    BLLCore --> IDAL
    DAL -->|实现| IDAL
    DAL --> Domain
    BLLCore -.consumes.-> Domain
```

---

## 3. ER 图 — LiteDB 实体关系总览

> 23 collection 之间的外键关系（实线 = 强 FK；虚线 = 可空 FK）。

```mermaid
erDiagram
    Domain ||--o{ ApplicationDomain : "1:N"
    Application ||--o{ ApplicationDomain : "1:N"
    Runtime ||--o{ Application : "RuntimeId (1:N)"

    MetamorphicRelation ||--o{ MRBinding : "MRId (1:N)"
    Application ||--o{ MRBinding : "ApplicationId (1:N)"
    MRBinding ||--o{ MRInstance : "MRBindingId (1:N)"
    MRInstance ||--o{ Execution : "MRInstanceId (1:N)"
    Execution ||--|| Result : "1:1"
    Result ||--o| Anomaly : "0..1"
    KnownBug ||--o{ Anomaly : "LinkedKnownBugId (0..*)"

    Batch ||--o{ Execution : "BatchId (0..*)"
    BatchPlan ||--o{ Batch : "PlanId (0..*)"

    DiscoveryMethod ||--o{ DiscoveryRun : "MethodId (1:N)"
    DiscoveryRun ||--o{ CandidateMR : "DiscoveryRunId (1:N)"
    CandidateMR ||--o{ ValidationRun : "CandidateMRId (1:N)"
    CandidateMR }o..|| MetamorphicRelation : "PromotedToMRId (when promoted)"
    Application ||--o{ DiscoveryRun : "TargetApplicationId (0..*)"

    MutationOperator ||--o{ Mutant : "OperatorId (1:N)"
    Application ||--o{ Mutant : "ApplicationId (0..*)"
    MutationCampaign ||--o{ MutationResult : "CampaignId (1:N)"
    Mutant ||--o{ MutationResult : "MutantId (1:N)"
    MRBinding ||--o{ MutationResult : "MRBindingId (1:N)"
    Execution ||--o{ MutationResult : "ExecutionId (1:N)"

    Application ||--o{ KnownBug : "RelatedApplicationId (0..*)"

    MetamorphicRelation {
        int IdMR PK
        string Code
        string MetaPatternCode
        string TransformationName
        string AssertionTypeCode
        string ValueName
        bool NoiseAware
        double ToleranceRel
        string Kind "method/system"
    }

    Application {
        int IdApplication PK
        string Name UK
        string Version
        int RuntimeId FK
        string RunnerEntryPath
        string InputParserPath
        string OutputParserPath
        string Kind "method/system"
    }

    Runtime {
        int IdRuntime PK
        string Name UK
        string Kind "python/matlab/cpp/java/fortran"
        string InvokeTemplate
    }

    Domain {
        int IdDomain PK
        string Name UK
        string Description
    }

    ApplicationDomain {
        int IdJunction PK
        int ApplicationId FK
        int DomainId FK
    }

    MRBinding {
        int IdMRBinding PK
        int MRId FK
        int ApplicationId FK
        string DefaultSampleCasePath
        bool IsActive
    }

    MRInstance {
        int IdInstance PK
        int MRBindingId FK
        string ParameterOverridesJson
        bool IsReusable
    }

    Execution {
        guid IdExecution PK
        int MRInstanceId FK
        guid BatchId FK
        string Status
        string CatalogVersionSha
        string SutVersionSnapshot
        DateTime QueuedAt
    }

    Result {
        guid IdResult PK
        guid ExecutionId FK
        double SourceValue
        double FollowupValue
        bool AssertionPassed
    }

    Anomaly {
        guid IdAnomaly PK
        guid ResultId FK
        string Severity
        string Category
        string Status
        int LinkedKnownBugId FK
    }

    KnownBug {
        int IdBug PK
        string Code UK
        int RelatedApplicationId FK
        string Status
    }

    Batch {
        guid IdBatch PK
        int PlanId FK
        string Status
    }

    BatchPlan {
        int IdPlan PK
        string Name UK
        string Schedule
        bool Enabled
    }

    Report {
        guid IdReport PK
        string Scope
        DateTime GeneratedAt
    }

    DiscoveryMethod {
        int IdMethod PK
        string Name
        string Version
        bool Enabled
    }

    DiscoveryRun {
        guid IdRun PK
        int MethodId FK
        int TargetApplicationId FK
        string Status
    }

    CandidateMR {
        guid IdCandidate PK
        guid DiscoveryRunId FK
        string Status
        int PromotedToMRId FK
    }

    ValidationRun {
        guid IdValidation PK
        guid CandidateMRId FK
        string ValidatorName
        bool Passed
    }

    MutationOperator {
        int IdOperator PK
        string Code UK
        string Category
        string PredictedClass
    }

    Mutant {
        int IdMutant PK
        int OperatorId FK
        int ApplicationId FK
        string Status
    }

    MutationCampaign {
        guid IdCampaign PK
        string Name
        string CatalogVersionSha
        string Status
    }

    MutationResult {
        guid IdMutationResult PK
        guid CampaignId FK
        int MutantId FK
        int MRBindingId FK
        guid ExecutionId FK
        string Outcome
    }

    AuditLog {
        guid IdLog PK
        DateTime Timestamp
        string Action
        string TargetEntityId
    }
```

---

## 4. 类图 — 核心实体（4 级 MR 语义层次 + 执行链）

```mermaid
classDiagram
    class MetaPattern {
        <<enum>>
        m_inv
        m_mono
        m_conv
        m_cmp
        m_adj / m_rev / m_dyn / m_rel
    }

    class MetamorphicRelation {
        +int IdMR
        +string Code
        +string MetaPatternCode
        +string TransformationName
        +string AssertionTypeCode
        +string ValueName
        +bool NoiseAware
        +double ToleranceRel
        +double NoiseMultiplier
        +string FeatureFilePath
        +string Kind «method/system»
        +DateTime CreatedAt
        ~v1 fields preserved~
    }
    note for MetamorphicRelation "Level 2: MR Schema\n抽象 MR 模板\nv1 字段 + v2 扩展共存"

    class Application {
        +int IdApplication
        +string Name
        +string Version
        +int? RuntimeId
        +string RunnerEntryPath
        +string InputParserPath
        +string OutputParserPath
        +int DefaultTimeoutSeconds
        +string Kind «method/system»
        ~v1 fields preserved~
    }
    note for Application "SUT 实体\nv1+v2 共用，Kind 区分"

    class Runtime {
        +int IdRuntime
        +string Name
        +string Kind «python/matlab/cpp/java/fortran»
        +string InvokeTemplate
        +Dict EnvVars
    }

    class MRBinding {
        +int IdMRBinding
        +int MRId «FK»
        +int ApplicationId «FK»
        +List~ParameterMapping~ ParameterMappings
        +string DefaultSampleCasePath
        +ToleranceConfig DefaultTolerance
        +SutHyperparams DefaultHyperparams
        +bool IsActive
    }
    note for MRBinding "Level 3: MR Binding\nM:N junction\n替代 v1 ApplicationName 多值反模式"

    class MRInstance {
        +int IdInstance
        +int MRBindingId «FK»
        +Dict ParameterOverrides
        +SamplingSpec? Sampling
        +SutHyperparams? HyperparamsOverride
        +ToleranceConfig? ToleranceOverride
        +bool IsReusable
        +string? Name
    }
    note for MRInstance "Level 4: MR Instance\n含参数 + 采样 + SUT 超参 override"

    class Execution {
        +Guid IdExecution
        +int MRInstanceId «FK»
        +Guid? BatchId
        +string Status
        +string CatalogVersionSha
        +string SutVersionSnapshot
        +string ArtifactsDirectory
        +DateTime QueuedAt/StartedAt/FinishedAt
    }
    note for Execution "Level 5: Execution\n状态机 + 版本快照"

    class Result {
        +Guid IdResult
        +Guid ExecutionId «FK»
        +double? SourceValue / SourceStd
        +double? FollowupValue / FollowupStd
        +Dict SourceMetrics / FollowupMetrics
        +bool AssertionPassed
        +string AssertionExpression
        +string? FailureReason
    }

    class Anomaly {
        +Guid IdAnomaly
        +Guid ResultId «FK»
        +string Severity «noise/minor/major/critical»
        +string Category «basin/mc-floor/cross-program/...»
        +int ReplayCount
        +string Status «new/investigating/known/confirmed-bug/false-positive»
        +int? LinkedKnownBugId «FK»
    }

    class KnownBug {
        +int IdBug
        +string Code «R-Case-N»
        +string Title
        +int? RelatedApplicationId
        +string? UpstreamFixCommit
        +string Status
    }

    MetamorphicRelation --> MetaPattern : MetaPatternCode
    Runtime <-- Application : RuntimeId
    MetamorphicRelation "1" --> "0..*" MRBinding : MRId
    Application "1" --> "0..*" MRBinding : ApplicationId
    MRBinding "1" --> "0..*" MRInstance : MRBindingId
    MRInstance "1" --> "0..*" Execution : MRInstanceId
    Execution "1" --> "1" Result : ExecutionId
    Result "1" --> "0..1" Anomaly : ResultId
    Anomaly --> "0..1" KnownBug : LinkedKnownBugId
```

---

## 5. 类图 — Adapter 概念分解（重要）

> v2 设计里**没有单独的 Adapter 实体**。"Adapter" 的职责被显式拆解到 3 个独立位置：

```mermaid
classDiagram
    class Adapter概念 {
        <<deprecated as single entity>>
        在 v2 拆解为三层
    }
    note for Adapter概念 "v1 'Adapter' = 文件解析 + 字段映射 + MR 变换 三件套.\nv2 设计把这三件事拆开存放，避免反模式."

    class Application {
        <<L1 — 文件 IO 解析>>
        +string InputParserPath
        +string OutputParserPath
        +string RunnerEntryPath
        +int RuntimeId
        每 SUT 一份；不感知 MR 含义
    }
    note for Application "Layer 1: Parser\n• Python 脚本，read/write SUT 原生文件\n• per-SUT 共享（同 SUT 的所有 MR 复用同一 parser）"

    class MRBinding {
        <<L2 — 抽象 → 具体字段映射>>
        +List~ParameterMapping~ ParameterMappings
        per-MR-per-SUT
    }
    note for MRBinding "Layer 2: ParameterMapping\n• 数据驱动（存 LiteDB，可在 UI 编辑）\n• 不知道 SUT 文件格式\n• 不知道变换逻辑"

    class ParameterMapping {
        +string AbstractParamName
        +string ConcreteFieldPath
        +string PathSyntax
        +ValueRange? ValueRange
        +string Unit
    }
    note for ParameterMapping "embedded record:\nMR 视角 'fuel.temperature' →\nSUT 视角 'materials.fuel.temperature_kelvin'"

    class IMRTransformation {
        <<L3 — MR 输入变换>>
        +string Name
        +Apply(dict, fieldPath, params) Dict
        per-transformation-type
    }
    note for IMRTransformation "Layer 3: Transformation\n• C# 内存 dict 操作\n• 不接触文件，不知道 SUT 格式\n• 跨 SUT 复用（Identity/ScaleField/Permute/Mirror/...）"

    class Pipeline协作 {
        <<编排 — SystemMtPipeline>>
        1. Parser.parse(source.file) → source_dict
        2. Transformation.Apply(source_dict, fieldPath, params) → followup_dict
        3. Parser.write(followup_dict) → followup.file
        4. Runtime.invoke(SUT) → output.file
        5. Parser.parse(output.file) → values
        6. AssertionEvaluator.Evaluate(values)
    }
    note for Pipeline协作 "Pipeline 通过 ParameterMapping 把 abstract field path\n解析成 concrete path 传给 Transformation"

    Application ..> ParameterMapping : "Application 提供 Parser；\nParameterMapping 提供字段定位"
    MRBinding *-- ParameterMapping : embeds
    Pipeline协作 ..> Application : "调 Input/Output Parser"
    Pipeline协作 ..> IMRTransformation : "调 Apply"
    Pipeline协作 ..> ParameterMapping : "读字段路径"
```

**为什么不要单独 Adapter 实体**：

| 反模式（v1 风格） | v2 设计 |
|---------------|--------|
| `Adapter` 同时承担 read/write + 字段映射 + MR 变换 | 三件事分离到 3 层 |
| 每个 (SUT × MR) 一个 adapter .py 文件 | Parser 仅按 SUT 一份；MR 变换是 C# |
| `openmoc_input_adapter_fuel_temperature.py` (~50 行) | `openmoc_input_parser.py` (~20 行通用) + `ScaleField` C# (复用全部 m_mono MR) |
| 加新 MR 加 1 新 .py 文件 | 加新 MR 加 1 行 ParameterMapping JSON |

详见 [`glossary.md`](glossary.md) §2 与 [`v2-system-mt-architecture.md`](v2-system-mt-architecture.md) §3.2。

---

## 6. 类图 — 值对象 (Value Objects，嵌入式)

```mermaid
classDiagram
    class ValueRange {
        +double? Min
        +double? Max
    }

    class ToleranceConfig {
        +bool NoiseAware
        +double ToleranceRel
        +double ToleranceAbs
        +double NoiseMultiplier «default 3.0»
    }

    class SutHyperparams {
        +long? Seed
        +int? Particles
        +int? Batches
        +int? MaxIterations
        +string? FitnessFunction
        +Dict OtherParams
    }

    class SamplingSpec {
        +string Distribution «fixed-list/uniform/log-uniform/normal»
        +int SampleCount
        +Dict DistributionParams
    }

    class ParameterMapping {
        +string AbstractParamName
        +string ConcreteFieldPath
        +string FieldType «number/array/string»
        +ValueRange? ValueRange
        +string Unit
        +string PathSyntax «json-pointer/mcnp-card/namelist-key»
        +string? Notes
    }

    class MRBinding {
        +int IdMRBinding
        +List~ParameterMapping~ ParameterMappings
        +ToleranceConfig DefaultTolerance
        +SutHyperparams DefaultHyperparams
    }

    class MRInstance {
        +int IdInstance
        +SamplingSpec? Sampling
        +SutHyperparams? HyperparamsOverride
        +ToleranceConfig? ToleranceOverride
    }

    ParameterMapping --> ValueRange
    MRBinding *-- ParameterMapping : embeds
    MRBinding *-- ToleranceConfig
    MRBinding *-- SutHyperparams
    MRInstance *-- SamplingSpec
    MRInstance *-- ToleranceConfig
    MRInstance *-- SutHyperparams
```

---

## 7. 类图 — Repository 模式（CRUD 层）

```mermaid
classDiagram
    class IRepository~T~ {
        <<interface>>
        +ObservableCollection~T~ GetAll()
        +T Get(int id)
        +ObservableCollection~T~ Get(T template)
        +bool Add(T)
        +bool Modify(T)
        +bool Remove(T)
    }
    note for IRepository "v1: int PK"

    class IGuidRepository~T~ {
        <<interface>>
        +ObservableCollection~T~ GetAll()
        +T? Get(Guid id)
        +bool Add(T)
        +bool Modify(T)
        +bool Remove(T)
        +ObservableCollection~T~ GetPage(int, int)
        +int Count()
    }
    note for IGuidRepository "v2 新增: Guid PK"

    class IRuntimeRepository {
        <<interface>>
        +Runtime? GetByName(string)
    }

    class IMRBindingRepository {
        <<interface>>
        +ObservableCollection GetByMR(int)
        +ObservableCollection GetByApplication(int)
        +ObservableCollection GetActive()
    }

    class IExecutionRepository {
        <<interface>>
        +ObservableCollection GetByMRInstance(int)
        +ObservableCollection GetByBatch(Guid)
        +ObservableCollection GetByStatus(string, int, int)
        +ObservableCollection GetByDateRange(DateTime, DateTime)
    }

    class IAnomalyRepository {
        <<interface>>
        +Anomaly? GetByResult(Guid)
        +ObservableCollection GetByStatus(string)
        +ObservableCollection GetByLinkedBug(int)
    }

    class LiteDbIntPkRepositoryBase~T~ {
        <<abstract>>
        #DbConfig _dbConfig
        #string _conn
        #string CollectionKey «abstract»
        +GetAll() / Get(int) / Add / Modify / Remove
    }

    class LiteDbGuidPkRepositoryBase~T~ {
        <<abstract>>
        #DbConfig _dbConfig
        #string _conn
        #string CollectionKey
        +GetAll() / Get(Guid) / Add / Modify / Remove
        +GetPage(int, int) / Count()
    }

    class LiteDbRuntimeRepository {
        +Runtime? GetByName(string)
    }

    class LiteDbExecutionRepository {
        +GetByMRInstance(int)
        +GetByBatch(Guid)
        +GetByStatus(string, int, int)
        +GetByDateRange(DateTime, DateTime)
    }

    IRepository <|-- IRuntimeRepository
    IRepository <|-- IMRBindingRepository
    IGuidRepository <|-- IExecutionRepository
    IGuidRepository <|-- IAnomalyRepository

    LiteDbIntPkRepositoryBase <|-- LiteDbRuntimeRepository
    LiteDbGuidPkRepositoryBase <|-- LiteDbExecutionRepository

    LiteDbRuntimeRepository ..|> IRuntimeRepository : implements
    LiteDbExecutionRepository ..|> IExecutionRepository : implements
```

---

## 8. 类图 — Pipeline 子系统（MT 编排核心）

```mermaid
classDiagram
    class ISystemMtPipeline {
        <<interface>>
        +ExecuteAsync(PipelineContext, IProgress, CT) Task~PipelineOutcome~
    }

    class SystemMtPipeline {
        -IProcessExecutor _processExecutor
        -AssertionEvaluator _assertionEvaluator
        +ExecuteAsync(...) Task~PipelineOutcome~
        -ConvertJsonValue(JsonElement) object
        -ParseOutputDict(...) Dict
        -ExtractMetrics(Dict) Dict~string,double~
    }

    class IProcessExecutor {
        <<interface>>
        +RunAsync(string cmd, string wd, int timeout, CT) Task~ProcessResult~
    }

    class DefaultProcessExecutor {
        +RunAsync(...) Task~ProcessResult~
        -PlatformShell(string) tuple
    }

    class ProcessResult {
        +int ExitCode
        +string Stdout / Stderr
        +TimeSpan Elapsed
        +bool TimedOut
    }

    class PipelineContext {
        <<record>>
        +string MrCode / TransformationName
        +string AssertionTypeCode / ValueName
        +string TargetFieldPath / PathSyntax
        +Dict Parameters
        +AssertionTolerance Tolerance
        +Dict? ExtraAssertionValues
        +string SutName / SourceCasePath
        +string WorkingDirectory
        +string InputParserCommand
        +string OutputParserCommand
        +string RunnerCommand
        +int TimeoutSeconds
        +string CatalogVersionSha
        +string SutVersionSnapshot
        +string MetbenchVersion
        +string TriggeredBy
    }

    class PipelineOutcome {
        <<record>>
        +string FinalStatus
        +string? ErrorMessage
        +DateTime StartedAt / FinishedAt
        +string ArtifactsDirectory
        +string SourceInputPath / FollowupInputPath
        +string SourceOutputPath / FollowupOutputPath
        +Dict? SourceMetrics / FollowupMetrics
        +SystemMtAssertionResultV2? AssertionResult
        +TimeSpan SourceElapsed / FollowupElapsed
        +int SourceExitCode / FollowupExitCode
    }

    class PipelineStatus {
        <<constants>>
        +Queued / ParsingSource / Transforming
        +WritingFollowup / RunningSource / RunningFollowup
        +ParsingOutputs / Asserting
        +Ok / Anomaly / Error / Timeout / Cancelled
        +IsTerminal(string) bool
    }

    class ReplayService {
        -ISystemMtPipeline _pipeline
        +ReplayAsync(PipelineContext, PipelineOutcome, ...) Task~ReplayResult~
        -Classify(orig, new, ctx) ReplayClassification
    }

    class ReplayClassification {
        <<enum>>
        Reproduced
        FixedOrFlaky
        RegressionOnReplay
        StillPassing
        MismatchedFailure
        NotComparable
    }

    class ReplayResult {
        <<record>>
        +PipelineOutcome OriginalOutcome
        +PipelineOutcome ReplayOutcome
        +ReplayClassification Classification
    }

    ISystemMtPipeline <|.. SystemMtPipeline : implements
    IProcessExecutor <|.. DefaultProcessExecutor
    SystemMtPipeline o-- IProcessExecutor : uses
    SystemMtPipeline o-- AssertionEvaluator : uses
    SystemMtPipeline ..> PipelineContext : reads
    SystemMtPipeline ..> PipelineOutcome : returns
    SystemMtPipeline ..> PipelineStatus : reports state
    DefaultProcessExecutor ..> ProcessResult : returns

    ReplayService o-- ISystemMtPipeline : uses
    ReplayService ..> ReplayResult : returns
    ReplayResult --> ReplayClassification
```

---

## 9. 类图 — 断言子系统（FluentAssertions 扩展）

```mermaid
classDiagram
    class AssertionTypeCodes {
        <<constants>>
        +string Less
        +string Greater
        +string Approx
        +string LessNoiseAware
        +string GreaterNoiseAware
        +string ApproxInvariant
        +string VarianceRatio
        +string FluxPointwiseApprox
        +string CrossProgramAgree
    }

    class AssertionInput {
        <<record>>
        +double SourceValue / SourceStd
        +double FollowupValue / FollowupStd
        +string ValueName
        +Dict~string,double~? ExtraValues
    }

    class AssertionTolerance {
        <<record>>
        +bool NoiseAware
        +double ToleranceRel
        +double ToleranceAbs
        +double NoiseMultiplier
    }

    class SystemMtAssertionResultV2 {
        <<record>>
        +string AssertionTypeCode
        +bool Passed
        +double? SourceValue / FollowupValue
        +double? ObservedDelta / ExpectedThreshold
        +string Expression
        +string? FailureReason
        +PassedResult() SystemMtAssertionResultV2
        +FailedResult() SystemMtAssertionResultV2
        +UnknownType() SystemMtAssertionResultV2
    }

    class MetbenchAssertionExtensions {
        <<static extensions>>
        +BeLessThanWithNoiseFloor(NumericAssertions, ...)
        +BeGreaterThanWithNoiseFloor(NumericAssertions, ...)
        +BeApproximatelyEqualUnderTransform(NumericAssertions, ...)
        +HaveVarianceRatio(NumericAssertions, ...)
        +BePointwiseApproximately(CollectionAssertions, ...)
        +AgreeWithReference(NumericAssertions, ...)
    }

    class AssertionEvaluator {
        +Evaluate(input, tolerance, code) SystemMtAssertionResultV2
        -IsAssertionFailure(Exception) bool
        -ExtractArray(Dict, string) IEnumerable~double~
    }

    AssertionEvaluator ..> AssertionInput : input
    AssertionEvaluator ..> AssertionTolerance : input
    AssertionEvaluator ..> AssertionTypeCodes : dispatches on
    AssertionEvaluator ..> MetbenchAssertionExtensions : delegates
    AssertionEvaluator ..> SystemMtAssertionResultV2 : returns
```

---

## 10. 类图 — Transformation 子系统（MR 输入变换）

```mermaid
classDiagram
    class IMRTransformation {
        <<interface>>
        +string Name
        +string ParametersSchema
        +Apply(IReadOnlyDict source, string fieldPath, IReadOnlyDict params) Dict
    }

    class IdentityTransform {
        +Name = "Identity"
    }
    note for IdentityTransform "Mut00 控制 (false-positive)"

    class ScaleField {
        +Name = "ScaleField"
        -IFieldPathResolver _resolver
        -ScaleAny(value, factor) object?
        -ScaleList(list, factor) List
    }

    class TranslateField {
        +Name = "TranslateField"
        -TranslateAny / TranslateList
    }

    class PermuteIndices {
        +Name = "PermuteIndices"
        -验证 permutation 合法性
    }

    class MirrorAxis {
        +Name = "MirrorAxis"
    }
    note for MirrorAxis "符号反转（m_inv geometry）"

    class CompositeTransform {
        +Name «runtime»
        -List~Step~ _steps
        +Step «record» (Transformation, FieldPath, Params)
    }

    class TransformationRegistry {
        <<static>>
        -Dict factories
        +Get(string name) IMRTransformation
        +Register(string, factory)
        +RegisterIfMissing(string, factory)
        +AvailableNames
    }

    IMRTransformation <|.. IdentityTransform
    IMRTransformation <|.. ScaleField
    IMRTransformation <|.. TranslateField
    IMRTransformation <|.. PermuteIndices
    IMRTransformation <|.. MirrorAxis
    IMRTransformation <|.. CompositeTransform

    CompositeTransform *-- IMRTransformation : N Step.Transformation
    TransformationRegistry ..> IMRTransformation : creates

    ScaleField o-- IFieldPathResolver
    TranslateField o-- IFieldPathResolver
    PermuteIndices o-- IFieldPathResolver
    MirrorAxis o-- IFieldPathResolver
```

---

## 11. 类图 — ParameterMapping 子系统（路径解析）

```mermaid
classDiagram
    class IFieldPathResolver {
        <<interface>>
        +string PathSyntax
        +Get(IReadOnlyDict data, string path) object?
        +Set(IReadOnlyDict data, string path, object? value) Dict
        +Exists(IReadOnlyDict data, string path) bool
    }

    class JsonPointerResolver {
        +PathSyntax = "json-pointer"
        -SplitPath(string) string[]
        -Step(current, segment) object?
        -SetRecursive(current, segments, idx, value)
    }
    note for JsonPointerResolver "RFC 6901 简化版\n支持 /x/y 和 x.y 和 x[0]"

    class McnpCardResolver {
        +PathSyntax = "mcnp-card"
        +ToJsonPointer(string) string
    }
    note for McnpCardResolver "card:m1::tmp →\n/cards/m1/tmp\n委托给 JsonPointer"

    class NamelistKeyResolver {
        +PathSyntax = "namelist-key"
        +ToJsonPointer(string) string
    }
    note for NamelistKeyResolver "&material/T →\n/namelists/material/T"

    class FieldPathResolverFactory {
        <<static>>
        +For(string syntax) IFieldPathResolver
        +SupportedSyntaxes List~string~
    }

    class FieldPathNotFoundException {
        <<exception>>
    }

    IFieldPathResolver <|.. JsonPointerResolver
    IFieldPathResolver <|.. McnpCardResolver
    IFieldPathResolver <|.. NamelistKeyResolver

    McnpCardResolver --> JsonPointerResolver : delegates
    NamelistKeyResolver --> JsonPointerResolver : delegates

    FieldPathResolverFactory ..> JsonPointerResolver : creates
    FieldPathResolverFactory ..> McnpCardResolver : creates
    FieldPathResolverFactory ..> NamelistKeyResolver : creates

    JsonPointerResolver ..> FieldPathNotFoundException : throws
```

---

## 12. 类图 — Discovery 子系统（MR 识别工作流）

```mermaid
classDiagram
    class DiscoveryMethod {
        +int IdMethod
        +string Name «MetaPattern-Structural/LLM-Native»
        +string Version
        +string ConfigJson
        +bool Enabled
    }

    class DiscoveryRun {
        +Guid IdRun
        +int MethodId «FK»
        +int? TargetApplicationId «FK»
        +DateTime StartedAt / FinishedAt
        +string Status
        +int CandidatesProduced
    }

    class CandidateMR {
        +Guid IdCandidate
        +Guid DiscoveryRunId «FK»
        +string ProposedCode / ProposedName
        +string? SuggestedTransformationName
        +string? SuggestedAssertionTypeCode
        +string ProposedValueName
        +string Rationale
        +double Confidence
        +string Status «pending/validated/promoted/rejected/duplicate»
        +int? PromotedToMRId «FK to MetamorphicRelation»
        +string? RejectionReason
    }

    class ValidationRun {
        +Guid IdValidation
        +Guid CandidateMRId «FK»
        +string ValidatorName «empirical/theoretical-llm/adversarial-mutmut»
        +DateTime RunAt
        +bool Passed
        +string DetailsJson
    }

    class MetamorphicRelation {
        +int IdMR
        +string Code
        +int? DiscoveryRunId
        +double? DiscoveryConfidence
        +string DiscoveryMethod
    }

    DiscoveryMethod "1" --> "0..*" DiscoveryRun : MethodId
    DiscoveryRun "1" --> "0..*" CandidateMR : DiscoveryRunId
    CandidateMR "1" --> "0..*" ValidationRun : CandidateMRId
    CandidateMR ..> MetamorphicRelation : PromotedToMRId\n(when ≥2 validators pass)
```

---

## 13. 类图 — Mutation 子系统（变异分析）

```mermaid
classDiagram
    class MutationOperator {
        +int IdOperator
        +string Code «Mut02-runner-sigt-from-siga»
        +string Category «runner-level/input-adapter-level/code-level»
        +string TargetFileType
        +string ApplicationSpec
        +string PredictedClass «semantic/equivalent/pathological»
        +string? RelatedMetaPattern
    }

    class Mutant {
        +int IdMutant
        +int OperatorId «FK»
        +int? ApplicationId «FK»
        +string AppliedDiff
        +string Status «active/deprecated/experimental»
    }

    class MutationCampaign {
        +Guid IdCampaign
        +string Name
        +string ScopeSpecJson
        +string CatalogVersionSha
        +DateTime StartedAt / FinishedAt
        +string Status «running/ok/cancelled»
    }

    class MutationResult {
        +Guid IdMutationResult
        +Guid CampaignId «FK»
        +int MutantId «FK»
        +int MRBindingId «FK»
        +Guid ExecutionId «FK»
        +string Outcome «detected/missed/error/not-affected»
        +double? ObservedDelta / ExpectedDelta
    }

    class Execution {
        +Guid IdExecution
        +int MRInstanceId
        +string Status
    }

    MutationOperator "1" --> "0..*" Mutant : OperatorId
    MutationCampaign "1" --> "0..*" MutationResult : CampaignId
    Mutant "1" --> "0..*" MutationResult : MutantId
    MutationResult --> Execution : ExecutionId (复用 Pipeline 跑)
```

---

## 14. MT Pipeline 数据流（顺序图）

```mermaid
sequenceDiagram
    autonumber
    actor User as 用户 (WPF)
    participant Service as Service Layer
    participant Pipeline as SystemMtPipeline
    participant InputParser as Python Input Parser
    participant Transform as IMRTransformation (C#)
    participant SUT as SUT runner
    participant OutputParser as Python Output Parser
    participant Eval as AssertionEvaluator
    participant DB as LiteDB

    User->>Service: ExecuteScenario(mrInstanceId)
    Service->>DB: Load MRInstance + Binding + Schema
    DB-->>Service: PipelineContext
    Service->>Pipeline: ExecuteAsync(ctx)

    Pipeline->>InputParser: parse source.in.json
    InputParser-->>Pipeline: source dict (JSON)
    Note over Pipeline: 状态: parsing-source → transforming

    Pipeline->>Transform: Apply(source dict, fieldPath, params)
    Note over Transform: IMRTransformation.Apply<br/>(纯内存操作，不接触文件)
    Transform-->>Pipeline: followup dict
    Note over Pipeline: 状态: transforming → writing-followup

    Pipeline->>InputParser: write followup dict
    InputParser-->>Pipeline: followup.in.json
    Note over Pipeline: 状态: → running-source

    Pipeline->>SUT: run source.in.json
    SUT-->>Pipeline: source.out.json
    Pipeline->>SUT: run followup.in.json
    SUT-->>Pipeline: followup.out.json
    Note over Pipeline: 状态: → parsing-outputs

    Pipeline->>OutputParser: parse source.out.json
    OutputParser-->>Pipeline: source values
    Pipeline->>OutputParser: parse followup.out.json
    OutputParser-->>Pipeline: followup values
    Note over Pipeline: 状态: → asserting

    Pipeline->>Eval: Evaluate(AssertionInput, ToleranceConfig, AssertionTypeCode)
    Note over Eval: FA 扩展方法 BeLessThanWithNoiseFloor 等
    Eval-->>Pipeline: SystemMtAssertionResultV2

    alt Passed
        Pipeline->>DB: persist Execution.Status=ok + Result
    else Failed
        Pipeline->>DB: persist Execution.Status=anomaly + Result + Anomaly
    end

    Pipeline-->>Service: PipelineOutcome
    Service-->>User: display Result + Replay button
```

---

## 15. .feature ↔ LiteDB 双向同步流程图

```mermaid
flowchart LR
    subgraph Files["metbench/catalog/features/"]
        F1["m_mono/MR-T.feature"]
        F2["m_inv/MR-Rot90.feature"]
        F3["m_conv/MR-RefineParticles.feature"]
    end

    subgraph Tools["Python tools/"]
        FtoDB["feature_to_db.py"]
        DBtoF["db_to_feature.py"]
        Validate["validate_feature_sync.py"]
    end

    subgraph Migrations["tools/migrate_*.py"]
        MS["migrate_python_scenarios_to_v2.py"]
        MM["migrate_mutations_to_v2.py"]
        MR["migrate_real_bugs_to_v2.py"]
    end

    subgraph Catalog["metbench/catalog/migration/"]
        JS["scenarios.json"]
        JM["mutations.json"]
        JR["real-bugs.json"]
    end

    subgraph DB["LiteDB"]
        DB1["MetamorphicRelations"]
        DB2["MRBindings"]
        DB3["MutationOperators / Mutants"]
        DB4["KnownBugs"]
    end

    F1 & F2 & F3 -->|parse| FtoDB
    FtoDB -->|JSON| JS

    MS -->|生成| JS
    MM -->|生成| JM
    MR -->|生成| JR

    JS & JM & JR -->|CSharp import - P8| DB1 & DB2 & DB3 & DB4

    DB1 & DB2 -->|export JSON| DBtoF
    DBtoF -->|生成| F1 & F2 & F3

    F1 & F2 & F3 -.->|input| Validate
    DB1 & DB2 -.->|export| Validate
    Validate -->|diff| Output{In sync?}
    Output -->|✓ CI green| End[OK]
    Output -->|✗ drift| Block[CI fail]
```

---

## 16. 4 级 MR 语义层次（概念视图）

```mermaid
flowchart TD
    L1["Level 1: MetaPattern<br/>m_inv / m_mono / m_conv / m_cmp<br/>NOETHER 框架代数性质"]
    L2["Level 2: MRSchema<br/>RaiseFuelTemperature / ScaleNuSigmaF<br/>已选择 transformation + value + direction<br/>仍然 SUT 无关"]
    L3["Level 3: MRBinding<br/>MR-T @ OpenMOC<br/>已配置 adapter + ParameterMapping<br/>仍无具体参数值"]
    L4["Level 4: MRInstance<br/>factor=1.5 + seed=42 + particles=5000<br/>可执行配置"]
    L5["Level 5: Execution<br/>2026-05-13 15:30:00 跑了一次<br/>结果落盘"]

    L1 -->|实例化（指定 transform / value / direction）| L2
    L2 -->|绑定到 SUT（adapter + ParameterMapping）| L3
    L3 -->|实例化（参数 + 超参 + 采样）| L4
    L4 -->|执行（在某时刻）| L5

    style L1 fill:#ffeb3b
    style L2 fill:#cddc39
    style L3 fill:#8bc34a
    style L4 fill:#4caf50
    style L5 fill:#009688
```

---

## 17. v1 ↔ v2 实体演化对照

```mermaid
flowchart LR
    subgraph V1["v1（保留+扩展）"]
        V1MR["MetamorphicRelation<br/>+ApplicationName «obsolete»<br/>+v2 字段 (Code/MetaPatternCode/...)"]
        V1App["Application<br/>+DomainName «obsolete»<br/>+v2 字段 (Version/RuntimeId/...)"]
        V1Dom["Domain"]
    end

    subgraph V2New["v2 新增"]
        V2Bind["MRBinding<br/>替代 ApplicationName 多值反模式"]
        V2AppDom["ApplicationDomain<br/>替代 DomainName 多值反模式"]
        V2RT["Runtime"]
        V2Inst["MRInstance"]
        V2Exec["Execution"]
        V2Res["Result"]
        V2Ano["Anomaly"]
        V2Disc["DiscoveryMethod / DiscoveryRun /<br/>CandidateMR / ValidationRun"]
        V2Mut["MutationOperator / Mutant /<br/>MutationCampaign / MutationResult"]
        V2Misc["KnownBug / AuditLog /<br/>Batch / BatchPlan / Report"]
    end

    V1MR -.->|"M:N junction"| V2Bind
    V1App -.-> V2Bind
    V1App -.-> V2AppDom
    V1Dom -.-> V2AppDom
    V1App -.->|"RuntimeId"| V2RT
    V2Bind -.-> V2Inst
    V2Inst -.-> V2Exec
    V2Exec -.-> V2Res
    V2Res -.-> V2Ano
```

---

## 参考链接

| 文档 | 用途 |
|------|------|
| [`v2-system-mt-architecture.md`](v2-system-mt-architecture.md) | 整体架构 + 模块清单 + Pipeline 状态机 |
| [`glossary.md`](glossary.md) | 术语表（4 级 MR 语义） |
| [`entity-model.md`](entity-model.md) | 23 collection 完整 schema |
| [`assertion-extensions.md`](assertion-extensions.md) | FA 扩展方法 API |
| [`migration-plan.md`](migration-plan.md) | 8 周路线 + 迁移脚本 |
| [`evolution.md`](evolution.md) | v1.0 → v2 演化纪实 |
