# MetBench 图集 — 功能 / 架构 / 类 / 时序

> 用 Mermaid 绘制。功能图依据 [`CLAUDE.md`](../CLAUDE.md) §2 的 T0–T4 分层；
> 架构/类/时序图依据当前项目结构与 `MetBench_BLL.Core/SystemMT` 实际代码。
> 图为概念快照，随代码演进需同步更新。

---

## 1. 功能图

T0 为核心 MT 流程；T1–T4 为围绕核心的功能层。

```mermaid
flowchart TB
    subgraph T0["T0 · 核心 — 系统级 MT 流程"]
        direction LR
        A1["测试输入生成"] --> A2["衍生输入转换"] --> A3["执行 SUT 被测程序"] --> A4["验证蜕变关系"]
    end
    subgraph T1["T1 · 直接支撑"]
        B1["SUT 执行适配器"]
        B2["MR 识别与验证"]
        B3["同源异构差分测试"]
    end
    subgraph T2["T2 · 呈现与交互"]
        C1["可视化与报表"]
        C2["CRUD"]
        C3["WPF 客户端"]
    end
    subgraph T3["T3 · 消费核心产出"]
        D1["缺陷封存与异常调查"]
    end
    subgraph T4["T4 · 评估 MR 集质量"]
        E1["变异模块"]
        E2["覆盖分析"]
    end
    C3 -->|"发起 MT"| A1
    B1 -.->|"落地执行步"| A3
    B2 -.->|"提供候选 MR"| A4
    B3 -.->|"跨程序一致性"| A4
    A4 -.->|"失败 run"| D1
    A4 -.->|"结果落库"| C1
    E1 -.->|"评估查错能力"| B2
    E2 -.->|"度量覆盖"| B2
```

---

## 2. 架构图

6 个工程分 4 层；箭头为编译期工程引用（`Domain` 为叶子）。WPF 经门面
`ISystemMtMrLauncher` 使用 System-MT；引擎以子进程调用 Python SUT。

```mermaid
flowchart TD
    subgraph L4["表示层"]
        CLIENT["MetBench_Client<br/>net8.0-windows · WPF (Wpf.Ui)"]
    end
    subgraph L3["业务层"]
        BLLCORE["MetBench_BLL.Core<br/>net8.0 · System-MT 引擎 / 门面 / 子系统群"]
        BLLLEGACY["MetBench_BLL<br/>net8.0 · 方法级 MT + 报表生成"]
    end
    subgraph L2["数据访问层"]
        DAL["MetBench_DAL<br/>net8.0 · LiteDB 仓储实现"]
    end
    subgraph L1["契约层"]
        IDAL["MetBench_IDAL<br/>仓储契约"]
        DOMAIN["MetBench_Domain<br/>实体"]
    end
    DB[("LiteDB<br/>MR.Litedb · SystemMT.Litedb")]
    SUT["Python SUT 进程<br/>OpenMOC · OpenMC · 热传导"]

    CLIENT --> BLLLEGACY
    CLIENT -.->|"DI 注入门面"| BLLCORE
    BLLLEGACY --> BLLCORE
    BLLLEGACY --> DAL
    DAL --> BLLCORE
    BLLCORE --> IDAL
    DAL --> DOMAIN
    IDAL --> DOMAIN
    BLLCORE --> DOMAIN
    DAL --> DB
    BLLCORE -.->|"子进程调用"| SUT
```

---

## 3. 类图

System-MT 运行时核心协作 —— 门面、引擎、适配器、结果记录与异常分类。

```mermaid
classDiagram
    class ISystemMtMrLauncher {
        <<interface>>
        +ListAvailableAsync() IReadOnlyList~MrSummary~
        +RunAsync(mrId, overrides) MrRunResult
        +RunBatchAsync(requests) IReadOnlyList~MrRunResult~
    }
    class SystemMtMrLauncher {
        -LauncherOptions options
        -IReadOnlyDictionary catalog
        +RunAsync(mrId) MrRunResult
        ~RecordAnomalyIfFailedAsync()
    }
    class SystemMtRunner {
        +RunAsync(task, valueName) SystemMtResult
    }
    class InputGenerator {
        +GenerateAsync() InputGenerationResult
    }
    class CliProgramRunner {
        +RunAsync(program, case, timeout) CliRunResult
    }
    class PythonInputAdapter {
        +TransformAsync() string
    }
    class PythonOutputAdapter {
        +ParseAsync() ParsedOutput
    }
    class IMrAssertion {
        <<interface>>
        +Name string
        +Evaluate(valueName, src, flw) SystemMtAssertionResult
    }
    class SystemMtResult {
        <<record>>
        +bool Passed
        +string FailureReason
    }
    class AnomalyClassifier {
        <<static>>
        +ClassifySeverity(result, thresholds) string
        +ClassifyCategory(result) string
    }
    class AnomalySeverityThresholds {
        <<record>>
        +double NoiseMaxPercent
        +double MinorMaxPercent
        +double MajorMaxPercent
    }
    class IAnomalyService {
        <<interface>>
        +RecordAnomalyAsync(mrName, resultId, severity, category) Anomaly
    }
    class AnomalyService
    class ISystemMtResultRepository {
        <<interface>>
        +SaveAsync(mrName, result) string
    }

    ISystemMtMrLauncher <|.. SystemMtMrLauncher
    IAnomalyService <|.. AnomalyService
    SystemMtMrLauncher ..> SystemMtRunner : 创建并调用
    SystemMtMrLauncher ..> ISystemMtResultRepository : 落库
    SystemMtMrLauncher ..> IAnomalyService : 失败时记录
    SystemMtMrLauncher ..> AnomalyClassifier : 分类
    SystemMtMrLauncher o-- AnomalySeverityThresholds
    SystemMtRunner *-- CliProgramRunner
    SystemMtRunner *-- PythonOutputAdapter
    SystemMtRunner *-- IMrAssertion
    SystemMtRunner o-- InputGenerator
    InputGenerator *-- PythonInputAdapter
    SystemMtRunner ..> SystemMtResult : 产出
    AnomalyClassifier ..> SystemMtResult : 读取
    AnomalyClassifier ..> AnomalySeverityThresholds : 读取
```

---

## 4. 时序图

一次 `RunAsync(mrId)` 的完整调用 —— 源运行 → 衍生运行 → 解析 → 断言 →
落库 → 失败则记异常。

```mermaid
sequenceDiagram
    actor User as 研究者
    participant WPF as WPF ViewModel
    participant L as SystemMtMrLauncher
    participant R as SystemMtRunner
    participant IG as InputGenerator
    participant CLI as CliProgramRunner
    participant SUT as Python SUT
    participant OA as PythonOutputAdapter
    participant AS as IMrAssertion
    participant REPO as ISystemMtResultRepository
    participant AN as AnomalyService

    User->>WPF: 选择 MR 并运行
    WPF->>L: RunAsync(mrId)
    L->>L: 查 MR catalog, 构建 SystemMtTask
    L->>R: RunAsync(task, valueName)
    R->>IG: GenerateAsync(衍生输入转换)
    IG-->>R: 衍生输入算例
    R->>CLI: RunAsync(源算例)
    CLI->>SUT: 执行源程序
    SUT-->>CLI: 源输出文件
    CLI-->>R: CliRunResult(源)
    R->>CLI: RunAsync(衍生算例)
    CLI->>SUT: 执行衍生程序
    SUT-->>CLI: 衍生输出文件
    CLI-->>R: CliRunResult(衍生)
    R->>OA: ParseAsync(源 / 衍生输出)
    OA-->>R: ParsedOutput ×2
    R->>AS: Evaluate(源值, 衍生值)
    AS-->>R: SystemMtAssertionResult
    R-->>L: SystemMtResult
    L->>REPO: SaveAsync(result)
    REPO-->>L: recordId
    opt result.Passed == false
        L->>L: AnomalyClassifier 分类 severity / category
        L->>AN: RecordAnomalyAsync(severity, category)
    end
    L-->>WPF: MrRunResult
    WPF-->>User: 展示结果 / 异常
```
