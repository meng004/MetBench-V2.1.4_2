# MetBench v2 术语表（Glossary）

> 本文件是术语真理源。任何文档、代码、UI 标签、commit message 中术语使用必须严格遵循本表。
> 术语 PR review 检查项：
> 1. 不能单独说"MR"——必须带级别限定词
> 2. 不能混用 adapter / parser / mapping
> 3. 不能混用 schema / binding / instance / execution

---

## 1. MR 的 4 级语义层次

> **这是术语表中最容易出错的部分。**
> "MR" 在历史代码与对话中至少有 4 种不同语义；本节明文区分。

| 级别 | 中文 | 英文 | 缩写 | 实体？ | 例子 |
|------|------|------|------|-------|------|
| L1 | **元模式** | MetaPattern | MP | 嵌入式枚举 | `m_mono` |
| L2 | **MR 模板** | MR Schema | MRS | ✅ `MetamorphicRelations` collection | "RaiseFuelTemperature" |
| L3 | **MR 绑定** | MR Binding | MRB | ✅ `MRBindings` collection | MR-T 绑到 OpenMOC |
| L4 | **MR 实例** | MR Instance | MRI | ✅ `MRInstances` collection | factor=1.5 + seed=42 跑一次 |

### 1.1 MetaPattern（L1）

NOETHER 框架定义的 8 个代数性质模板：

| 代码 | 含义 | 数学形式（示意） |
|------|------|---------------|
| `m_inv` | 不变性 / 对称变换 | Y(s) = Y(τ(s)) |
| `m_mono` | 单调性 | s.x ≤ s'.x ⇒ Y(s) op Y(s'), op ∈ {<, >} |
| `m_conv` | 收敛速率 | σ(Y) ∝ 1/√N |
| `m_cmp` | 程序间比较 | Y_A(s) ≈ Y_B(s) |
| `m_adj` | 自伴随 | A* = A |
| `m_rev` | 时间可逆 | s ↔ τ_rev(s) |
| `m_dyn` | 定性动力学 | 轨迹拓扑保持 |
| `m_rel` | 关系等价 | s ~ s' ⇒ Y(s) ~ Y(s') |

**实体形态**：嵌入式枚举（字符串常量集），不需要独立 LiteDB collection。

### 1.2 MR Schema（L2）

参数化的 MR 类型，**未绑定具体 SUT**。

**核心字段**（来自既有 `MetamorphicRelation` 类扩展）：
- `Code`：简码（"MR-T"）
- `Name`：人类可读名（"RaiseFuelTemperature"）
- `MetaPatternCode`：所属 MetaPattern
- `TransformationName`：使用哪个 `IMRTransformation`（如 `ScaleField`）
- `AssertionTypeCode`：用哪种断言（如 `less-noise-aware`）
- `ValueName`：检查哪个输出值（如 `k_eff`）
- `NoiseAware`、`ToleranceRel`、`NoiseMultiplier`
- `Description`：物理 / 数学推导
- `FeatureFilePath`：对应 `.feature` 文件路径

**例子**：
```
MR-T = m_mono + ScaleField + less-noise-aware + k_eff
       + "fuel.temperature 升高，k_eff 下降"
```

### 1.3 MR Binding（L3）

MR Schema **绑定到具体 SUT**，配齐 ParameterMapping、默认采样案例、默认容差、默认 SUT 超参。

**核心字段**：
- `MRId` → MetamorphicRelations
- `ApplicationId` → Applications（SUT）
- `ParameterMappings`：abstract param → SUT field path 映射列表
- `DefaultSampleCasePath`
- `DefaultTolerance`、`DefaultHyperparams`

**例子**：
```
Binding("MR-T", "openmoc-prod-2026q2")
  ParameterMappings = [
    {abstract: "fuel.temperature",
     concrete: "materials.fuel.temperature_kelvin",
     range: [273, 3000]}
  ]
  DefaultTolerance = {NoiseAware: false, ToleranceRel: 0.0}
  DefaultHyperparams = {Particles: null, Seed: null}  // 确定性 SUT
  DefaultSampleCasePath = "SUT/openmoc/sample/pincell.json"
```

**关键洞察**：同一 MR Schema 可以有多个 Binding（每个 SUT 一个）。同 MetBench v1 的 `ApplicationName` 多值字符串（":" 分隔）反模式，由 `MRBindings` collection 取代。

### 1.4 MR Instance（L4）

MR Binding **加上具体参数 + 采样策略 + SUT 超参 override**，得到可执行配置。

**核心字段**：
- `MRBindingId`
- `ParameterOverrides`：实际参数值（`{factor: "1.5"}`）
- `Sampling`：采样策略（`{Distribution: "log-uniform", SampleCount: 5}`，可空）
- `HyperparamsOverride`：SUT 超参 override（`{Seed: 42, Particles: 50000}`）
- `ToleranceOverride`：容差 override
- `SampleCaseOverridePath`：替换默认 sample case

**例子**：
```
Instance(Binding("MR-T", "openmoc"))
  ParameterOverrides = {factor: "1.5"}
  HyperparamsOverride = {Seed: 42}
  // 其他 null → 用 Binding 默认
```

### 1.5 Execution（L5）

MR Instance 的**一次具体运行**。包含状态机字段（QueuedAt/StartedAt/FinishedAt/Status）+ 版本快照（CatalogVersionSha + SutVersionSnapshot + MetbenchVersion）。

一个 MRInstance **可以被多次 Execution**（用于 Replay / 不同时刻重跑）。

---

## 2. 输入输出处理相关术语

| 术语 | 含义 | 实施 | 不是 |
|------|------|------|------|
| **Input Parser** | per-SUT Python 脚本，把 SUT 原生输入文件转 in-memory dict（双向：read + write） | `SUT/<sut>/<sut>_input_parser.py` | 不做 MR 变换 |
| **Output Parser** | per-SUT Python 脚本，把 SUT 原生输出文件解析为 in-memory dict | `SUT/<sut>/<sut>_output_parser.py` | 不做 MR 断言 |
| **Parameter Mapping** | per-Binding 的结构化数据：MR 抽象参数名 ↔ SUT 字段路径 | LiteDB `MRBindings.ParameterMappings` 嵌入字段 | 不是 Python 代码 |
| **MR Transformation** | MR 输入变换逻辑（如"scale field by factor"） | C# `IMRTransformation` 实现 | **不**在 Python adapter 里 |
| **Runtime** | SUT 执行时所需的运行环境（Python venv / MATLAB / C++ binary / ...） | LiteDB `Runtimes` collection | 不是 SUT 本身 |

### 关键边界陈述

> **MR Transformation 是 MT pipeline 的职责，不是 Parser 的职责。**
> Parser 只做 SUT 原生格式 ↔ in-memory dict 转换，不知道 MR 含义。
> Transformation 在 dict 上操作，不知道 SUT 文件格式。

---

## 3. 持久化相关术语

| 术语 | 含义 |
|------|------|
| **Execution** | Pipeline 的一次运行（状态机 + 版本快照） |
| **Result** | Execution 的数值产物（source/followup values + 断言判定 + 失败原因） |
| **Anomaly** | Result 中 `AssertionPassed=false` 的子集，进入异常调查工作流 |
| **ExecutionArtifacts** | Execution 留下的物理文件（source/followup 输入输出 + stdout/stderr 日志），存文件系统不存 DB |
| **CatalogVersionSha** | Execution 触发时 MR catalog 的 git commit SHA |
| **SutVersionSnapshot** | Execution 触发时 SUT 自报的版本字符串（git sha / release tag） |

---

## 4. Discovery 子系统术语

| 术语 | 含义 |
|------|------|
| **Discovery Method** | MR 识别方法（如 MetaPattern-Structural、LLM-Native） |
| **Discovery Run** | 某 Method 的一次执行 |
| **Candidate MR** | Discovery 产物 — 待验证的 MR 候选（未入 MRSchemas） |
| **Validator** | 验证候选 MR 的工具：empirical / theoretical / adversarial |
| **Validation Run** | 一次具体的验证 |
| **Promotion** | 候选 MR 通过 ≥2 个 validator 后写入 MRSchemas 表的过程 |

---

## 5. Mutation 子系统术语

| 术语 | 含义 |
|------|------|
| **Mutation Operator** | 变异算子（如 "scatter-transpose"） |
| **Mutant** | Operator 的一次具体应用（含 diff patch） |
| **Mutation Campaign** | 一次活动（mutants × MRBindings × sample cases） |
| **Mutation Result** | Campaign 中单 cell 结果（mutant × MRBinding → detected/missed/error） |
| **Mut00** | 身份变异（identity）— 不改变 SUT，用作假阳性控制（false-positive sanity check） |
| **Detection Rate** | 某 MR 检出多少 mutant / 总 mutant 数 |
| **Cohen's κ** | 跨 SUT 检测一致性度量（matched pair 上同 MR 在两 SUT 是否同样敏感） |

---

## 6. 覆盖率维度术语

| 术语 | 计算 |
|------|------|
| **MetaPattern Coverage** | `count(distinct MR.MetaPatternCode) / 8` |
| **SUT × MR Coverage** | `count(MRBindings) / (#Applications × #MRSchemas)` |
| **Bug Coverage** | `count(Anomalies.LinkedKnownBugId distinct) / count(KnownBugs)` |
| **Mutation Coverage** | `mutants detected by ≥1 MR / total mutants` |

---

## 7. 错误约定（Adapter / Parser subprocess）

| Exit Code | 含义 |
|-----------|------|
| 0 | OK |
| 1 | Generic failure（含 stack trace） |
| 64 | Source file not found / unreadable |
| 65 | Parameter mapping invalid（字段不存在 / 类型不匹配） |
| 66 | Value out of declared range |
| 67 | Transformation unsupported on this dict shape |
| 73 | Output file write failure |

**Stderr 末行**约定 JSON：`{"error_class": "...", "field": "...", "message": "..."}`

C# pipeline 读 exit code 选 typed exception，读 stderr JSON 填 `Execution.ErrorMessage`。

---

## 8. 断言相关术语

| 术语 | 含义 |
|------|------|
| **AssertionType** | 断言类型码：`less` / `greater` / `less-noise-aware` / `greater-noise-aware` / `approx` / `variance-ratio` / `flux-pointwise-approx` 等 |
| **ToleranceConfig** | 容差配置：`NoiseAware`(bool) + `ToleranceRel`(double) + `NoiseMultiplier`(double, 默认 3.0) |
| **Noise Floor** | 噪声底：`max(NoiseMultiplier × √(σ_src² + σ_flw²), ToleranceRel × |source|)` |
| **AssertionEvaluator** | 调 FluentAssertions 扩展方法 + 包装结果的 C# 类 |
| **AssertionExtension** | FluentAssertions 扩展方法（如 `BeLessThanWithNoiseFloor`） |

---

## 9. 编排相关术语

| 术语 | 含义 |
|------|------|
| **SystemMtPipeline** | C# Pipeline 编排器（§3.1 of v2-system-mt-architecture.md） |
| **Batch** | 一组待执行的 MRInstance（手动 / 定时） |
| **BatchPlan** | Batch 的模板（如 "all MRSchemas × all bound SUTs × default params"），可 cron |
| **ReplayService** | 从 Anomaly 重放原 Execution 的服务 |
| **TrendAnalysisService** | 跨时间窗的 pass rate / anomaly 数变化分析 |
| **CoverageService** | 多维覆盖率计算服务 |

---

## 10. 既有 v1 术语对齐

| v1 既有术语 | v2 含义 / 状态 |
|----------|--------------|
| `MetamorphicRelation`（v1 类） | 直接复用作为 MR Schema 实体，**扩展新字段** |
| `Application`（v1 类） | 直接复用作为 SUT 实体，**扩展新字段**；既有 `InputParameters`/`OutputParameters` 列表保留 |
| `Domain`（v1 类） | 保持不变 |
| `ApplicationName`（v1 字段，":" 分隔多值） | **deprecated**，由 `MRBindings` collection 取代 |
| `DomainName`（v1 字段，":" 分隔多值） | **deprecated**，由 `ApplicationDomains` junction collection 取代 |
| `SystemMtResultRecord`（Stage 4 类） | **deprecated**，由 `Execution + Result + Anomaly` 三表取代 |
| `ScenarioBlueprint`（C# 内部 record） | **deprecated**，scenario 信息走 `MRBindings` 数据驱动 |
| `IMrAssertion`（Stage 4 接口） | **deprecated**，由 FluentAssertions 扩展方法替代 |
| `MrTransformation`（Stage 4 record） | 重命名为 `MRInstanceConfig` 或拆为 `MRInstance.ParameterOverrides` |
| `MrFamily`（Stage 4 string slug） | 由 `MR.MetaPatternCode + MR.Code` 联合表达 |

---

## 11. 命名规则

- **C# 实体类名**：单数名词（`MRSchema` / `MRBinding` / `Execution`）
- **C# 接口**：`I` 前缀（`IMRDiscoverer` / `IMRTransformation`）
- **LiteDB collection 名**：复数（`MRBindings` / `Executions`）
- **C# namespace**：`MetBench_BLL.Core.<Module>`（`Adapters` / `MRs` / `SystemMT.Assertions`）
- **WPF 页面**：`<Name>Page.xaml`（如 `AnomalyDetailPage.xaml`）
- **WPF ViewModel**：`<Name>ViewModel.cs`
- **Python 模块**：`<sut_name>_<role>.py`（`openmoc_input_parser.py` / `openmoc_output_parser.py` / `openmoc_runner.py`）
- **`.feature` 文件路径**：`metbench/catalog/features/<metapattern>/<MR-Code>-<Name>.feature`

---

## 12. 中文 ↔ 英文映射（WPF UI 用）

| 英文 | 中文（UI 标签） |
|------|--------------|
| MetaPattern | MR 元模式 |
| MR Schema | MR 模板 |
| MR Binding | MR 绑定 |
| MR Instance | MR 实例 |
| Execution | 执行 |
| Result | 结果 |
| Anomaly | 异常 |
| Replay | 重放 |
| Discovery | MR 识别 |
| Candidate MR | MR 候选 |
| Validation | 验证 |
| Promotion | 提升入库 |
| Mutation | 变异 |
| Coverage | 覆盖率 |
| Trend | 趋势 |
| Input Parser | 输入解析器 |
| Output Parser | 输出解析器 |
| Parameter Mapping | 参数映射 |
| Sample Case | 样例用例 |
| Runtime | 运行时 |
| SUT | 被测程序（系统级） |

---

**本术语表与代码、文档、UI、commit message 同步。任何术语扩展先改本文件 PR，再改下游。**
