# MR 验证统一设计 v1.2：Typed Semantic Model + Fail-Closed Validator + Codex 派工版

> **日期**：2026-05-25  
> **版本**：v1.2 Codex-ready  
> **状态**：替代 v1.1。任何 MR / Property catalog 接入、schema 校验、runtime verifier 实现、legacy migration、golden fixtures、CI gate、Codex PR 派发，以本文档为准。  
> **范围**：PWR 反应堆物理软件的系统级 MT 验证模型；不包含 method-level MT。  
> **驱动**：`PWR_MR_Analysis_Report.md` 的 47 个报告条目：43 条严格 MR + 4 条 Property。  
> **最终裁决**：保留 **typed `PredicateSpec` 判别联合 + `IVerifierKernel<TPredicate>` runtime**；恢复并强化 **catalog 装载期 fail-closed validator**。typed union 负责结构类型安全，validator 负责引用完整性、语义兼容性与 registry 绑定。

---

## 0. v1.2 相对 v1.1 的修正

v1.1 的主线是正确的：predicate 采用 typed discriminated union，runtime 采用泛型分派，catalog 禁止 `KernelCode + Dictionary<string,string>` 退化形式。

v1.1 的错误是把 typed union 的能力说过头：

```text
错误说法 A：删除装载期 ValidateSpec 子系统。
错误说法 B：role / projection / parameter 引用由类型系统自动保证。
```

v1.2 修正为：

```text
正确说法 A：删除 KernelCode + Dictionary 时代的 string-slot ValidateSpec；
          但必须保留 typed predicate validator。

正确说法 B：类型系统只保证 predicate 结构、字段存在、字段类型、oneOf discriminator；
          role / projection / derived metric / parameter / transform / tolerance compatibility
          必须由 MrSpec.Validate() / PropertySpec.Validate() 在 catalog 装载期 fail-closed 检查。
```

### 0.1 v1.2 的四条硬裁决

1. **主线不变**：`PredicateSpec` 必须是 typed discriminated union；禁止回退到 `KernelCode + RoleBindings + ProjectionBindings + Dictionary Parameters`。
2. **validator 必须保留**：`MrSpec.Validate()` / `PropertySpec.Validate()` 是 catalog 装载 gate，任何失败不得进入 runtime。
3. **validator 与 runtime 分离**：`IPredicateValidator<TPredicate>` 做 spec 层校验；`IVerifierKernel<TPredicate>` 做 execution 后判定。kernel 不负责修补坏 catalog。
4. **Codex PR 计划采用 PR-0 + PR-1...PR-10**：PR-0 上 YAML DSL / schema / generator；PR-9 实现 `ExponentialGrowth`；PR-10 做 43 MR + 4 Property migration + golden fixtures。

### 0.2 语义分层

```text
Catalog semantic layer:
  MrSpec
    ├── Parameters
    ├── Applicability
    ├── Roles
    ├── Projections
    ├── DerivedMetrics
    ├── Predicates          // typed discriminated union, 10 MR predicate classes
    ├── DefaultTolerance
    └── Tags

Property semantic layer:
  PropertySpec
    ├── Parameters
    ├── Case
    ├── Projections
    ├── DerivedMetrics
    ├── Assertions          // typed discriminated union, 2 property predicate classes
    ├── DefaultTolerance
    └── Tags

Validation layer:
  ISpecValidator
    ├── IPredicateValidator<TPredicate>
    ├── IPropertyPredicateValidator<TPredicate>
    ├── transform registry checks
    ├── derived metric registry checks
    ├── parameter reference resolution
    ├── metric / observation kind compatibility
    └── tolerance compatibility

Runtime layer:
  IMrExecutionEngine
    ├── IRunPlanner
    ├── ISutRunner
    ├── IObservationExtractor
    ├── IDerivedMetricEvaluator
    ├── IPredicateDispatcher
    └── IVerifierKernel<TPredicate>

Property runtime layer:
  IPropertyChecker
    ├── IObservationExtractor
    ├── IDerivedMetricEvaluator
    └── IPropertyPredicateChecker<TPredicate>
```

---

## 1. 核心原则

### 1.1 Predicate 是 catalog 语义；Kernel 是 runtime 后端

```text
PredicateSpec 子类声明“要验证什么关系”。
IVerifierKernel<TPredicate> 实现“如何在观测值上判定”。
```

catalog 不持有 runtime 类名；runtime 不反过来定义 MR 语义。

禁止形式：

```json
{
  "kernel_code": "m_scaled_equality",
  "role_bindings": { "left": "followup", "right": "source" },
  "projection_bindings": { "metric": "k_eff" },
  "parameters": { "k": "$mr.k" }
}
```

正确形式：

```json
{
  "kind": "ScaledEquality",
  "predicate_id": "flux-scales-with-normalization",
  "actual_role": "followup",
  "reference_role": "source",
  "metric": "phi_forward",
  "factor": { "kind": "MrParameterRef", "name": "alpha" },
  "exponent": 1.0,
  "override_tolerance": {
    "kind": "FieldNormTolerance",
    "norm": "RelativeL2",
    "atol": 0.0,
    "rtol": 1e-5
  }
}
```

### 1.2 Typed union 的真实边界

Typed union 能保证：

```text
1. predicate kind 与字段集合匹配；
2. required 字段存在；
3. 字段类型正确；
4. ParameterExpression / ToleranceSpec / ShapeSpec / FieldPairing 有 discriminator；
5. 不再出现 role slot / projection slot / parameter key 的裸 dictionary。
```

Typed union 不能单独保证：

```text
1. LeftRole = "coarse" 是否存在于 MrSpec.Roles；
2. Metric = "k_eff" 是否存在于 Projections 或 DerivedMetrics；
3. Metric 的 ObservationKind 是否满足 predicate 要求；
4. MrParameterRef("alpha") 是否存在；
5. RunParameterRef 是否由 role / runner contract 提供；
6. RoleTransformParameterRef 是否指向真实 transform step；
7. Transform operator / DerivedMetric operator 是否已注册；
8. FieldPairing 是否与 Field2D layout 兼容；
9. ToleranceSpec 子类是否与 predicate / observation kind 兼容。
```

因此，v1.2 的基本安全模型是：

```text
schema/type safety + load-time semantic validation + runtime verifier
```

三者缺一不可。

### 1.3 MR 与 Property 严格分离

```text
MR       = 多次执行之间的输入—输出关系，通常 N ≥ 2 个 run。
Property = 单次执行输出内部的性质，恰好 1 个 run。
```

必须分离：

```text
catalog/mr/*.yaml
catalog/property/*.yaml
```

不允许：

```text
1. 把 Property 塞进 MrSpec。
2. 把 MR 伪装成 Property。
3. 把 43 MR + 4 Property 混算成 47 条 MR coverage。
4. 让 Property 触发 follow-up run 生成。
```

### 1.4 N 元 role 是一等公民

`source/followup` 只是特例。v1.2 支持任意 N 元 role：

```text
二元 MR:       source, followup
收敛 MR:       coarse, fine, reference
遮蔽 MR:       baseline, rod_A, rod_B, rod_AB
轨迹 MR:       sweep[0], sweep[1], ..., sweep[n]
跨方法 MR:     diffusion, transport / CRAM, TTA / NEM, FDM
MC 统计 MR:    low_sample, high_sample
```

### 1.5 一条 MR 可有多条谓词

`MrSpec.Predicates` 是 AND 列表。

例如 `Bol-Phy-01`：

```text
Predicate 1: k_eff invariant → BinaryComparisonPredicate(Equal)
Predicate 2: flux scales with normalization → ScaledEqualityPredicate or FieldEqualityPredicate + ScaledPairing
```

任一 predicate failed，则 MR failed。

### 1.6 Applicability 与 Predicate 分离

```text
Applicability false → SkippedNotApplicable，不消耗 SUT。
Predicate false     → Failed。
```

Applicability 不能藏进 predicate，也不能写成注释。

### 1.7 Projection 与 Predicate 正交

```text
Projection 描述从输出取什么。
Predicate 描述取出来以后满足什么关系。
```

predicate 内部不得偷偷读取未声明路径。projection 内部不得偷偷比较。

### 1.8 不引入任意表达式 DSL

不引入：

```text
Roslyn
SymPy
Lua
JavaScript expression
自由字符串数学表达式求值
```

允许的是白名单 typed expression：

```text
ParameterExpression
DerivedExpression
ConditionExpr
ShapeSpec
FieldPairing
```

这些表达式都是判别联合，不是自由字符串。

---

## 2. 能力边界

### 2.1 In-scope

| 能力 | Typed predicate / 机制 | 代表条目 |
|---|---|---|
| 标量二元比较 | `BinaryComparisonPredicate` | Dif-Phy-01/02/04/10 |
| 标量近似相等 | `BinaryComparisonPredicate(Operator=Equal)` | Dif-Phy-07, Dif-Alg-05 |
| 缩放等式 | `ScaledEqualityPredicate` | Bol-Phy-01 场段 |
| 跨方法比较 | `CrossMethodComparisonPredicate` + `MethodBinding` | Dif-Phy-12/13, Bol-Phy-04, Cpl-App-08 |
| N 元参考解收敛 | `ErrorMonotonicPredicate` | Dif-Alg-01/02 等 |
| MC 方差缩放 | `VarianceRatioPredicate` + `StatisticalProjectionSpec` | Bol-Alg-02 |
| 序列形状 | `SequenceShapePredicate` + `ShapeSpec` | Dif-Phy-09, Cpl-App-01/04/05/07 |
| 三元次可加 | `SubadditivePredicate` | Dif-Phy-05 |
| 场量对称 | `FieldEqualityPredicate` + `FieldPairing` | Dif-Phy-06 |
| 场量比例拟合 | `FieldProportionalityPredicate` | Dif-Phy-03 |
| 派生量守恒 | `DerivedInvariantPredicate` | Bur-Phy-01 |
| 条件化 MR | `ApplicabilitySpec` | Dif-Phy-11 |
| Property 值域 | `BoundPropertyPredicate` | Res-Alg-03 |
| Property 单次轨迹形态 | `ShapePropertyPredicate` | Bur-Phy-04, Kin-Phy-02 |
| Field derived ordering | `DerivedMetricSpec` + `BoundPropertyPredicate` | Dif-Phy-08 |
| 5 态诊断 | `VerifyStatus` | 全部 |
| Typed tolerance | 4 个 `ToleranceSpec` 子类 | deterministic / statistical / relative / field norm |
| 47 条覆盖 | 43 MR + 4 Property | 全部 schema 覆盖；PR-10 后全部 validate |

### 2.2 Out-of-scope

| 不做 | 理由 | 触发重审条件 |
|---|---|---|
| 3D / 4D 场比较 | 当前 43 MR 无需 | 出现 3D 张量 MR 时扩 `Field3DValue` |
| 连续时间轨迹 | 当前使用离散 `SequenceValue` | Surrogate / PINN 接入时 |
| 跨 MR 依赖 / 组合 | 单 MR 独立验证 | 出现组合 MR 时设计 `MetaMrSpec` |
| 任意表达式 DSL | 类型、安全、调试成本不可控 | 永不做 |
| Bayesian / posterior MR | 当前无此类 MR | Stage 9+ 视需求 |
| MetaPattern 自动发现 | v1.2 是下游执行 IR | Stage 9 实现 compiler |
| 自适应 verification | MR 是单向断言 | 不做 |
| Method-level MT | 与系统级 MR 分离 | 另建测试轨道 |
| Common Random Numbers / 协方差 | v1.2 假设独立样本 | v2 视 MC 需求 |
| UI / 报表 / 持久化细节 | 应用层 | 不进本设计 |

---

## 3. 顶层模型

### 3.1 `MrSpec`

```csharp
public sealed record MrSpec(
    string MrId,
    string Name,
    string Description,
    FiveDTags Tags,
    MrParameterSet Parameters,
    ApplicabilitySpec? Applicability,
    IReadOnlyDictionary<string, RunRoleSpec> Roles,
    IReadOnlyDictionary<string, ProjectionSpec> Projections,
    IReadOnlyList<DerivedMetricSpec> DerivedMetrics,
    IReadOnlyList<PredicateSpec> Predicates,
    ToleranceSpec DefaultTolerance)
{
    public ValidationResult Validate(
        ITransformRegistry transforms,
        IDerivedMetricRegistry derivedMetrics,
        IPredicateValidatorRegistry predicateValidators,
        IPathResolver pathResolver,
        IRunParameterContract runParameterContract);
}
```

字段规则：

| 字段 | 规则 |
|---|---|
| `MrId` | 全局唯一，kebab-case，不能与 PropertyId 冲突 |
| `Tags` | 必填，供 5D coverage 统计 |
| `Parameters` | MR 级参数；可由 `MrParameterRef` 引用 |
| `Applicability` | 可空；存在时必须在 run plan 前执行 |
| `Roles` | 至少 2 个；自比较 MR 也必须显式声明 role |
| `Projections` | 至少 1 个 |
| `DerivedMetrics` | 可空；operator 必须注册 |
| `Predicates` | 至少 1 个；全部 AND |
| `DefaultTolerance` | 必填；predicate 可 override |

### 3.2 `PropertySpec`

```csharp
public sealed record PropertySpec(
    string PropertyId,
    string Name,
    string Description,
    FiveDTags Tags,
    MrParameterSet Parameters,
    InputCaseRef Case,
    IReadOnlyDictionary<string, ProjectionSpec> Projections,
    IReadOnlyList<DerivedMetricSpec> DerivedMetrics,
    IReadOnlyList<PropertyPredicateSpec> Assertions,
    ToleranceSpec? DefaultTolerance)
{
    public ValidationResult Validate(
        IDerivedMetricRegistry derivedMetrics,
        IPropertyPredicateValidatorRegistry propertyValidators,
        IPathResolver pathResolver,
        IRunParameterContract runParameterContract);
}
```

Property 与 MR 差异：

| 维度 | MrSpec | PropertySpec |
|---|---|---|
| 执行次数 | N ≥ 2；或显式多 role / 自比较 | 1 |
| 顶层输入 | `Roles` | `Case` |
| 谓词类型 | `PredicateSpec` | `PropertyPredicateSpec` |
| follow-up run | 有 | 无 |
| coverage | MR coverage，43 条分母 | Property coverage，4 条分母 |
| runtime | `IMrExecutionEngine` | `IPropertyChecker` |

### 3.3 `FiveDTags`

```csharp
public sealed record FiveDTags(
    EquationKey EquationKey,
    ProgramType ProgramType,
    MetaPattern Pattern,
    SourceLevel SourceLevel,
    FailureCorrelation FailureCorrelation);
```

---

## 4. Predicate 判别联合

### 4.1 MR `PredicateSpec`：10 个子类

```csharp
public abstract record PredicateSpec(
    string PredicateId,
    ToleranceSpec? OverrideTolerance = null);

public sealed record BinaryComparisonPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string Metric,
    ComparisonOperator Operator,
    ToleranceSpec? OverrideTolerance = null,
    ParameterExpression? Margin = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record ScaledEqualityPredicate(
    string PredicateId,
    string ActualRole,
    string ReferenceRole,
    string Metric,
    ParameterExpression Factor,
    double Exponent,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record CrossMethodComparisonPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string Metric,
    ComparisonOperator Operator,
    ToleranceSpec? OverrideTolerance = null,
    ParameterExpression? Margin = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record ErrorMonotonicPredicate(
    string PredicateId,
    IReadOnlyList<string> OrderedRoles,
    string ReferenceRole,
    string Metric,
    NormKind Norm,
    bool Strict,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record VarianceRatioPredicate(
    string PredicateId,
    string LowSampleRole,
    string HighSampleRole,
    string StdMetric,
    ParameterExpression SampleRatio,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record SequenceShapePredicate(
    string PredicateId,
    string SequenceRole,
    string Metric,
    ShapeSpec Shape,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record SubadditivePredicate(
    string PredicateId,
    IReadOnlyList<string> PartRoles,
    string CombinedRole,
    string Metric,
    bool Strict,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record FieldEqualityPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string Field,
    FieldPairing Pairing,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record FieldProportionalityPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string Field,
    ConstantEstimator Estimator,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);

public sealed record DerivedInvariantPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string DerivedMetric,
    ToleranceSpec? OverrideTolerance = null
) : PredicateSpec(PredicateId, OverrideTolerance);
```

### 4.2 Property `PropertyPredicateSpec`：2 个子类

```csharp
public abstract record PropertyPredicateSpec(
    string PredicateId,
    ToleranceSpec? OverrideTolerance = null);

public sealed record BoundPropertyPredicate(
    string PredicateId,
    string Metric,
    ComparisonOperator Operator,
    ParameterExpression Bound,
    ToleranceSpec? OverrideTolerance = null,
    ParameterExpression? Margin = null
) : PropertyPredicateSpec(PredicateId, OverrideTolerance);

public sealed record ShapePropertyPredicate(
    string PredicateId,
    string Metric,
    ShapeSpec Shape,
    ToleranceSpec? OverrideTolerance = null
) : PropertyPredicateSpec(PredicateId, OverrideTolerance);
```

`Dif-Phy-08` 不新增 `RelativeOrderingPropertyPredicate`。表达方式是：

```text
DerivedMetric: edge_minus_center = field_region_mean(edge) - field_region_mean(center)
Assertion: BoundPropertyPredicate(metric=edge_minus_center, operator=Less, bound=0)
```

这样保持 Property predicate 词表封闭为 2 个子类。

---

## 5. 参数、容差、形状、场配对

### 5.1 `ParameterExpression`

```csharp
public abstract record ParameterExpression;

public sealed record ConstantParameter(ParameterValue Value) : ParameterExpression;
public sealed record MrParameterRef(string Name) : ParameterExpression;
public sealed record RunParameterRef(string RoleName, string Name) : ParameterExpression;
public sealed record RoleTransformParameterRef(
    string RoleName,
    string TransformName,
    string ParamName,
    int? StepIndex = null) : ParameterExpression;
```

v1.2 可选增加受限算术表达式，用于 `ExponentialGrowth.ExpectedRate`：

```csharp
public sealed record BinaryParameterExpression(
    ParameterExpression Left,
    ArithmeticOperator Operator,     // Add / Subtract / Multiply / Divide
    ParameterExpression Right) : ParameterExpression;
```

这不是任意表达式 DSL：它无变量查找、无函数调用、无字符串求值、无反射，只是 typed AST。

### 5.2 `ToleranceSpec`

```csharp
public abstract record ToleranceSpec;

public sealed record DeterministicToleranceSpec(
    double Atol,
    double Rtol,
    double? AbsFloor = null) : ToleranceSpec;

public sealed record StatisticalToleranceSpec(
    double SigmaMultiplier,
    int MinSamples,
    bool RequireBothInBand = true) : ToleranceSpec;

public sealed record RelativeToleranceSpec(
    double Rtol,
    double? AbsFloor = null) : ToleranceSpec;

public sealed record FieldNormToleranceSpec(
    FieldNormKind Norm,
    double Atol,
    double Rtol) : ToleranceSpec;
```

### 5.3 `ShapeSpec`

```csharp
public abstract record ShapeSpec;

public sealed record BellShape(
    double PeakLocationTolerance,
    double SymmetryThreshold) : ShapeSpec;

public sealed record SShape(
    SCurveDirection Direction,
    double InflectionTolerance,
    double EndpointPlateauThreshold) : ShapeSpec;

public sealed record SignChange(
    int ExpectedChanges,
    int Tolerance) : ShapeSpec;

public sealed record NonMonotonic(
    int MinDirectionChanges) : ShapeSpec;

public sealed record ConstantSlope(
    double CovThreshold,
    double? ExpectedSlope = null) : ShapeSpec;

public sealed record ExponentialGrowth(
    ParameterExpression ExpectedRate,
    double RateTolerance,
    double ResidualRelTolerance,
    double MinRSquared = 0.95) : ShapeSpec;
```

`ExponentialGrowth` 的 runtime 留给 PR-9；schema / validator 在 PR-0/PR-1 即可支持。

### 5.4 `FieldPairing`

```csharp
public abstract record FieldPairing;

public sealed record IdentityPairing() : FieldPairing;

public sealed record SymmetryPairing(
    SymmetryAxis Axis,
    Field2DLayout Layout) : FieldPairing;

public sealed record PermutationPairing(
    IReadOnlyList<IndexMapping> Mappings) : FieldPairing;

public sealed record ScaledPairing(
    ParameterExpression Factor,
    double Exponent) : FieldPairing;
```

---

## 6. Projection、Observation、DerivedMetric

### 6.1 `ProjectionSpec`

```csharp
public abstract record ProjectionSpec(string Name, string Path);

public sealed record ScalarProjectionSpec(
    string Name,
    string Path,
    string? Unit = null) : ProjectionSpec(Name, Path);

public sealed record StatisticalProjectionSpec(
    string Name,
    string MeanPath,
    string StdErrorPath,
    string? Unit = null) : ProjectionSpec(Name, MeanPath);

public sealed record VectorProjectionSpec(
    string Name,
    string Path,
    string? Unit = null) : ProjectionSpec(Name, Path);

public sealed record Field2DProjectionSpec(
    string Name,
    string Path,
    AxisSpec XAxis,
    AxisSpec YAxis,
    string? Unit = null) : ProjectionSpec(Name, Path);

public sealed record SequenceProjectionSpec(
    string Name,
    string SweepRole,
    string ScalarPathPerSample,
    string? XUnit = null,
    string? YUnit = null) : ProjectionSpec(Name, ScalarPathPerSample);
```

### 6.2 `ObservationValue`

```csharp
public abstract record ObservationValue;

public sealed record ScalarValue(double Value, string? Unit = null) : ObservationValue;
public sealed record StatisticalValue(double Mean, double StdError, int Samples, string? Unit = null) : ObservationValue;
public sealed record VectorValue(IReadOnlyList<double> Values, string? Unit = null) : ObservationValue;
public sealed record Field2DValue(double[,] Values, AxisSpec XAxis, AxisSpec YAxis, string? Unit = null) : ObservationValue;
public sealed record SequenceValue(IReadOnlyList<double> X, IReadOnlyList<ObservationValue> Y) : ObservationValue;
```

### 6.3 `DerivedMetricSpec`

```csharp
public sealed record DerivedMetricSpec(
    string Name,
    DerivedExpression Expression,
    ObservationKind OutputKind);

public abstract record DerivedExpression;
```

白名单 derived operator：

| Operator | Input | Output | 用途 |
|---|---|---|---|
| `finite_difference` | sequence | sequence/scalar | 斜率 / 反应性系数 |
| `l2_norm` | vector/field | scalar | 收敛误差 |
| `linf_norm` | vector/field | scalar | worst error |
| `field_region_mean` | field + mask | scalar | Dif-Phy-08 |
| `field_difference` | field, field | field | 场量残差 |
| `mass_number_sum` | nuclide vector | scalar | Bur-Phy-01 |
| `delta_reactivity` | scalar pair | scalar | rod shadowing |
| `coefficient_of_variation` | sequence | scalar | ConstantSlope |
| `scalar_subtract` | scalar, scalar | scalar | derived bound / ordering |

任何未注册 derived operator 都必须被 `Validate()` 拒绝。

---

## 7. 装载期校验：v1.2 的关键修正

### 7.1 Validation pipeline

```text
1. Raw YAML / JSON parse
2. JSON schema validation with oneOf discriminator
3. Polymorphic deserialization to typed records
4. MrSpec.Validate() / PropertySpec.Validate()
5. Registry resolution
6. Golden fixture preflight
7. Runtime execution
```

第 4 步是强制的。不得因为 typed union 存在而省略。

### 7.2 Validator interfaces

```csharp
public interface ISpecValidator
{
    ValidationResult Validate(MrSpec spec, ValidationContext context);
    ValidationResult Validate(PropertySpec spec, ValidationContext context);
}

public interface IPredicateValidator<TPredicate>
    where TPredicate : PredicateSpec
{
    ValidationResult Validate(
        TPredicate predicate,
        MrSpec spec,
        ValidationContext context);
}

public interface IPropertyPredicateValidator<TPredicate>
    where TPredicate : PropertyPredicateSpec
{
    ValidationResult Validate(
        TPredicate predicate,
        PropertySpec spec,
        ValidationContext context);
}
```

### 7.3 Mandatory validation checks

`MrSpec.Validate()` 必须检查：

```text
1. MrId 唯一、格式正确；Predicates 非空。
2. Roles 非空，MR 至少两个有效 role 或显式自比较设计。
3. 每个 predicate 引用的 role 存在。
4. 每个 predicate 引用的 metric 存在于 Projections 或 DerivedMetrics。
5. metric 的 ObservationKind 与 predicate 要求兼容。
6. 每个 ParameterExpression 可解析且类型可转换。
7. RoleTransformParameterRef 指向真实 role / transform / parameter。
8. RunParameterRef 符合 runner contract。
9. Transform operator 已注册。
10. DerivedMetric operator 已注册。
11. ToleranceSpec 子类与 predicate / observation kind 兼容。
12. FieldPairing 与 Field2D layout / shape 兼容。
13. CrossMethodComparisonPredicate 的左右 role 必须有 MethodBinding。
14. SequenceShapePredicate 指向 SequenceProjectionSpec 或 Derived sequence。
15. ErrorMonotonicPredicate 的 OrderedRoles 长度 >= 2，ReferenceRole 存在且不混入 OrderedRoles 除非显式允许。
16. VarianceRatioPredicate 的 StdMetric 是 StatisticalValue 或 std-error projection。
17. No legacy fields: kernel_code / role_bindings / projection_bindings / assertion_name。
```

`PropertySpec.Validate()` 必须检查：

```text
1. PropertyId 唯一、格式正确；Assertions 非空。
2. 不存在 Roles；不存在 Applicability；不存在 follow-up run。
3. 每个 assertion 引用的 metric 存在。
4. BoundPropertyPredicate 的 bound 可解析为 scalar。
5. ShapePropertyPredicate 的 metric 是 SequenceValue。
6. Property 不进入 MR coverage。
7. No legacy implicit $only role。
```

### 7.4 ValidationResult

```csharp
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors)
{
    public static ValidationResult Success { get; }
}

public sealed record ValidationError(
    string SpecId,
    string? PredicateId,
    string Path,
    string Code,
    string Message,
    ValidationSeverity Severity);
```

错误必须包含字段路径，例如：

```text
predicates[2].actual_role
projections.k_eff.path
assertions[0].metric
parameters.alpha
roles.followup.transforms[1].parameters.scale
```

---

## 8. Runtime 设计

### 8.1 `VerificationContext`

```csharp
public sealed record VerificationContext(
    string MrId,
    IReadOnlyDictionary<string, RoleOutput> OutputsByRole,
    IReadOnlyDictionary<string, ObservationValue> Observations,
    IReadOnlyDictionary<string, ObservationValue> DerivedMetrics,
    IReadOnlyDictionary<string, RunParameterSet> RoleParameters,
    TransformTrace TransformTrace,
    DiagnosticContext Diagnostics);
```

Observation key convention：

```text
projection:   "{role}.{projectionName}"
derived:      "{role}.{derivedMetricName}"
sequence:     "{sequenceRole}.{metric}"
```

### 8.2 Kernel interface

```csharp
public interface IVerifierKernel<TPredicate>
    where TPredicate : PredicateSpec
{
    VerifyResult Verify(
        TPredicate predicate,
        VerificationContext context);
}
```

v1.2 不要求 runtime kernel 暴露 `ValidateSpec()`。若仓库已有该方法，可以保留为 adapter，但真正 gate 必须由 `IPredicateValidator<TPredicate>` 在 catalog load 阶段完成。

### 8.3 Dispatcher

```csharp
public interface IPredicateDispatcher
{
    VerifyResult Verify(PredicateSpec predicate, VerificationContext context);
}

public interface IVerifierKernelRegistry
{
    IVerifierKernel<TPredicate> Resolve<TPredicate>()
        where TPredicate : PredicateSpec;
}
```

Mapping：

| Predicate | Runtime kernel |
|---|---|
| `BinaryComparisonPredicate` | `BinaryComparisonKernel` |
| `ScaledEqualityPredicate` | `ScaledEqualityKernel` |
| `CrossMethodComparisonPredicate` | `CrossMethodComparisonKernel` |
| `ErrorMonotonicPredicate` | `ErrorMonotonicKernel` |
| `VarianceRatioPredicate` | `VarianceRatioKernel` |
| `SequenceShapePredicate` | `SequenceShapeKernel` |
| `SubadditivePredicate` | `SubadditiveKernel` |
| `FieldEqualityPredicate` | `FieldEqualityKernel` |
| `FieldProportionalityPredicate` | `FieldProportionalityKernel` |
| `DerivedInvariantPredicate` | `DerivedInvariantKernel` |

### 8.4 Property checker

```csharp
public interface IPropertyChecker
{
    Task<PropertyResult> CheckAsync(
        PropertySpec spec,
        InputCase inputCase,
        CancellationToken ct);
}

public interface IPropertyPredicateChecker<TPredicate>
    where TPredicate : PropertyPredicateSpec
{
    PropertyPredicateResult Check(
        TPredicate predicate,
        PropertyVerificationContext context);
}
```

`IPropertyChecker` 可以复用 extractor、derived evaluator、tolerance evaluator、shape evaluator，但不得经过 `IMrExecutionEngine`。

---

## 9. Result 与 5 态诊断

```csharp
public enum VerifyStatus
{
    Passed,
    Failed,
    SkippedNotApplicable,
    SkippedMissingObservable,
    InvalidSpec
}

public enum PropertyStatus
{
    Held,
    Violated,
    SkippedMissingObservable,
    InvalidSpec
}
```

统计规则：

```text
MR coverage denominator = 43。
Property coverage denominator = 4。
SkippedNotApplicable 与 SkippedMissingObservable 不计入 Passed/Failed 比例。
InvalidSpec 在 PR-10 后必须为 0。
```

Result 必须包含：

```text
1. mr_id / property_id
2. predicate_id
3. predicate kind
4. involved roles
5. metric
6. actual / expected / residual
7. tolerance
8. worst offenders for field / sequence
9. raw diagnostic values needed for paper/report
```

---

## 10. `ExponentialGrowth` runtime 规范（PR-9 / v1.2）

用于 `Kin-Phy-02`。

输入：

```text
SequenceValue X = time samples
SequenceValue Y = positive scalar observations, e.g. power(t)
ExpectedRate = ParameterExpression, typically (rho - beta) / Lambda
```

算法：

```text
1. Validate all y_i > 0。若存在 y_i <= 0，返回 Failed，diagnostic code = NonPositiveSample。
2. Transform z_i = log(y_i)。
3. Fit z_i = a + b * x_i by least squares。
4. estimated_rate = b。
5. rate_residual = abs(estimated_rate - expected_rate)。
6. fit_residual_rel = norm(z - z_hat) / max(norm(z), eps)。
7. r_squared = 1 - ss_res / ss_tot。
8. Passed iff:
     rate_residual <= RateTolerance
     fit_residual_rel <= ResidualRelTolerance
     r_squared >= MinRSquared
```

Diagnostics：

```text
expected_rate
estimated_rate
rate_residual
fit_residual_rel
r_squared
worst_point_index
worst_point_x
worst_point_y
worst_log_residual
```

---

## 11. 入库自检表

新 MR / Property 申请入库时逐条勾选，任一不过即拒绝，记 deferred 或提交 RFC。

```text
□ 类型选择正确？MR = 多次执行关系；Property = 单次执行性质。
□ 每个输出可降为 Scalar / Statistical / Vector / Field2D / Sequence？
□ MR predicate 可拆成 10 个 typed PredicateSpec 中的一种或多种？
□ Property assertion 可拆成 2 个 PropertyPredicateSpec 中的一种或多种？
□ 涉及的 transform operator 已注册？
□ 涉及的 derived operator 已注册？
□ Applicability 可降为 ConditionExpr？
□ 所有 role 引用都存在？
□ 所有 metric 引用都存在于 Projections 或 DerivedMetrics？
□ 所有 ParameterExpression 可解析？
□ 所有 tolerance 与 predicate / observation kind 兼容？
□ Cross-method MR 使用 MethodBinding，而不是 transform 暗示 solver 差异？
□ Property 没有进入 MR catalog？
□ MR 没有进入 Property catalog？
□ Golden fixture 已覆盖 pass / fail / missing observable / invalid spec 中必要分支？
```

第 7—11 项不是“由类型系统自动保证”，而是由 `Validate()` fail-closed 保证。

---

## 12. Coverage matrix

### 12.1 43 MR coverage

| MR | Predicate | Notes |
|---|---|---|
| Dif-Phy-01 / 02 / 04 / 10 | `BinaryComparisonPredicate` | scalar monotonic |
| Dif-Phy-03 | `FieldProportionalityPredicate` | field proportionality |
| Dif-Phy-05 | `SubadditivePredicate` | rod shadowing, derived `delta_rho` |
| Dif-Phy-06 | `FieldEqualityPredicate(SymmetryPairing)` | symmetric field self-comparison |
| Dif-Phy-07 | `BinaryComparisonPredicate(Equal)` + optional `FieldEqualityPredicate` | two predicates if field also checked |
| Dif-Phy-09 | `SequenceShapePredicate(BellShape)` | sequence shape |
| Dif-Phy-11 | `ApplicabilitySpec` + `BinaryComparisonPredicate` | conditional MR |
| Dif-Phy-12 / 13 | `CrossMethodComparisonPredicate` | method comparison |
| Dif-Alg-01 / 02 | `ErrorMonotonicPredicate` | reference convergence |
| Dif-Alg-03 | `CrossMethodComparisonPredicate` | NEM vs FDM |
| Dif-Alg-04 | `BinaryComparisonPredicate` | scalar monotonic |
| Dif-Alg-05 | `BinaryComparisonPredicate(Equal)` + optional `FieldEqualityPredicate` | equality |
| Bol-Phy-01 | `BinaryComparisonPredicate(Equal)` + `ScaledEqualityPredicate` | k_eff invariant + flux scaling |
| Bol-Phy-02 / 03 / 05 | `BinaryComparisonPredicate` | scalar monotonic |
| Bol-Phy-04 | `CrossMethodComparisonPredicate` | P0 vs P1 |
| Bol-Alg-01 / 03 | `ErrorMonotonicPredicate` | convergence |
| Bol-Alg-02 | `VarianceRatioPredicate` | MC variance |
| Bur-Phy-01 | `DerivedInvariantPredicate` | mass number conservation |
| Bur-Phy-02 | `BinaryComparisonPredicate(Equal)` or `DerivedInvariantPredicate` | vector / invariant |
| Bur-Phy-03 | `BinaryComparisonPredicate` on derived `pit_depth` | scalar derived |
| Bur-Alg-01 | `ErrorMonotonicPredicate` | convergence |
| Bur-Alg-02 | `CrossMethodComparisonPredicate` | CRAM vs TTA |
| Res-Alg-01 | `ErrorMonotonicPredicate` | convergence |
| Res-Alg-02 / 04 | `BinaryComparisonPredicate` | scalar monotonic |
| Kin-Phy-01 / 03 | `BinaryComparisonPredicate` | scalar monotonic |
| Kin-Alg-01 | `ErrorMonotonicPredicate` | convergence |
| Cpl-App-01 | `SequenceShapePredicate(SShape DownThenUp)` | sequence shape |
| Cpl-App-02 / 03 / 06 | `BinaryComparisonPredicate` | scalar monotonic |
| Cpl-App-04 | `SequenceShapePredicate(SignChange)` | sign change |
| Cpl-App-05 | `SequenceShapePredicate(NonMonotonic)` | non-monotonic |
| Cpl-App-07 | `SequenceShapePredicate(ConstantSlope)` | near constant slope |
| Cpl-App-08 | `CrossMethodComparisonPredicate` | coupled cross-method |

Summary：43/43 MR schema-covered。

### 12.2 4 Property coverage

| Property | Property assertion | Notes |
|---|---|---|
| Dif-Phy-08 | `DerivedMetricSpec(field_region_mean / scalar_subtract)` + `BoundPropertyPredicate` | edge-center ordering encoded as scalar bound `< 0` |
| Bur-Phy-04 | `ShapePropertyPredicate(SShape DownThenUp)` | iodine pit |
| Res-Alg-03 | two `BoundPropertyPredicate` | `0 <= Dancoff <= 1` |
| Kin-Phy-02 | `ShapePropertyPredicate(ExponentialGrowth)` | PR-9 log-linear fit |

Summary：4/4 Property schema-covered；PR-9 后 4/4 executable。

---

## 13. Catalog authoring：YAML DSL（PR-0）

目标：作者写短 YAML，工具生成 typed JSON / C# record，并运行 schema + validator。

### 13.1 MR YAML example

```yaml
kind: MrSpec
mr_id: dif-phy-doppler-negative
name: Negative Doppler coefficient
description: T_fuel increase broadens resonance absorption and decreases k_eff.
tags:
  equation_key: Diffusion
  program_type: Deterministic
  pattern: P2_Mono
  source_level: Phy
  failure_correlation: WithinEquation
parameters:
  delta_t:
    kind: Constant
    value: { kind: Double, value: 50.0 }
roles:
  baseline:
    kind: Baseline
    input_source: { kind: Base }
  hotter:
    kind: Followup
    input_source: { kind: DerivedFromRole, from_role: baseline }
    transforms:
      - transformation_name: IncreaseFuelTemperature
        target_path: /fuel/temperature
        parameters:
          delta_t: { kind: MrParameterRef, name: delta_t }
projections:
  k_eff:
    kind: ScalarProjection
    path: /results/k_eff
predicates:
  - kind: BinaryComparison
    predicate_id: hotter-k-eff-lower
    left_role: hotter
    right_role: baseline
    metric: k_eff
    operator: Less
    override_tolerance:
      kind: DeterministicTolerance
      atol: 1.0e-6
      rtol: 1.0e-4
default_tolerance:
  kind: DeterministicTolerance
  atol: 1.0e-8
  rtol: 1.0e-5
```

### 13.2 Property YAML example

```yaml
kind: PropertySpec
property_id: res-alg-dancoff-range
name: Dancoff factor bounded range
description: Dancoff factor must be within [0, 1].
tags:
  equation_key: Resonance
  program_type: Deterministic
  pattern: P_Range
  source_level: Alg
  failure_correlation: WithinEquation
case:
  kind: Base
projections:
  dancoff:
    kind: ScalarProjection
    path: /results/dancoff_factor
assertions:
  - kind: BoundProperty
    predicate_id: dancoff-lower-bound
    metric: dancoff
    operator: GreaterOrEqual
    bound: { kind: ConstantParameter, value: { kind: Double, value: 0.0 } }
  - kind: BoundProperty
    predicate_id: dancoff-upper-bound
    metric: dancoff
    operator: LessOrEqual
    bound: { kind: ConstantParameter, value: { kind: Double, value: 1.0 } }
default_tolerance:
  kind: DeterministicTolerance
  atol: 0.0
  rtol: 0.0
```

---

## 14. Legacy migration

旧模型：

```text
IMrAssertion + assertion_name + EqualityThresholds + source/followup
```

迁移规则：

| 旧字段 / 类型 | 新结构 |
|---|---|
| `ApproxEqual` | `BinaryComparisonPredicate(Operator=Equal)` |
| `GreaterThan` | `BinaryComparisonPredicate(Operator=Greater)` |
| `LessThan` | `BinaryComparisonPredicate(Operator=Less)` |
| `ScaledApproxEqual` | `ScaledEqualityPredicate` |
| `value_name` | `ProjectionSpec.Name` |
| `EqualityThresholds(atol, rtol)` | `DeterministicToleranceSpec` |
| `NoiseAware` | `StatisticalToleranceSpec` |
| `source/followup` | `Roles` entries |
| `transformParams["k"]` | `MrParameterRef` or `RoleTransformParameterRef` |

Compatibility policy：

```text
1. LegacyAssertionAdapter may exist for one migration cycle only.
2. New MR catalog must not use assertion_name.
3. New MR catalog must not use KernelCode + Dictionary bindings.
4. All legacy catalog entries must be migrated through a migrator.
5. After migration, runtime kernels only target typed predicate.
```

---

## 15. Codex 实施约束

### 15.1 绝对禁止

```text
1. 不要把 PredicateSpec 实现成 KernelCode + Dictionary。
2. 不要删除装载期 validator；typed union 不能替代 MrSpec.Validate / PropertySpec.Validate。
3. 不要声称 role / projection / parameter 引用由类型系统自动保证。
4. 不要引入任意表达式 DSL。
5. 不要把 PropertySpec 混入 MrSpec。
6. 不要把 Applicability 写成普通 predicate。
7. 不要把 MethodBinding 藏进 transform 链。
8. 不要让 transformParams["k"] 这类字符串约定进入新 schema。
9. 不要让 transform 与 predicate 各自复制同一参数值；必须用 ParameterExpression 引用同一来源。
10. 不要让 runtime kernel 负责修补 catalog schema 错误；validator 必须 fail-closed。
11. 不要让 JSON/YAML 中出现未注册的 derived operator 或 transform operator。
12. 不要把 source/followup 二元接口作为核心模型。
13. 不要让 Property 参与 MR coverage 统计。
14. 不要默默忽略缺失 projection；必须返回 SkippedMissingObservable。
15. 不要在 PR-10 前宣称 47/47 完成。
```

### 15.2 必须做

```text
1. 所有 polymorphic spec 必须有 discriminator kind。
2. 所有 predicate / assertion 必须有 PredicateId。
3. 所有 Validate() 必须可在 catalog load 阶段运行。
4. 所有 validator failure 必须包含 spec id、predicate id、字段路径、错误原因。
5. 所有 kernel test 必须有 pass 和 fail fixture。
6. field/sequence kernel 必须报告 worst offender。
7. Property checker 必须独立于 MR execution engine。
8. Legacy migrator 必须有 snapshot tests。
9. PR-10 前必须有 CI gate 拒绝 legacy predicate fields。
10. PR-10 后 InvalidSpec 数量必须为 0。
```

---

## 16. Codex PR 派工计划

### PR-0：Catalog DSL + schema + generator + anti-legacy lint

**目标**：建立 catalog authoring 与 schema 基础。

**实现范围**：

```text
YAML DTO model
JSON schema generator or checked-in schema
System.Text.Json polymorphic discriminator mapping
oneOf + discriminator for PredicateSpec / PropertyPredicateSpec / ParameterExpression / ToleranceSpec / ShapeSpec / FieldPairing / ProjectionSpec / DerivedExpression
anti-legacy lint: reject kernel_code / role_bindings / projection_bindings / assertion_name
sample mr yaml
sample property yaml
```

**验收**：

```text
1. 一份 MR YAML 能 deserialize 成 typed MrSpec。
2. 一份 Property YAML 能 deserialize 成 typed PropertySpec。
3. dictionary predicate 被 schema/lint 拒绝。
4. missing discriminator kind 被 schema 拒绝。
5. CI 可运行 schema validation。
```

**Codex prompt**：

```text
Implement PR-0 from docs/2026-05-25-mr-verification-v1.2-codex-ready.md.
Create the catalog YAML/JSON schema foundation with oneOf discriminator support.
Reject legacy fields kernel_code, role_bindings, projection_bindings, assertion_name.
Do not implement runtime kernels in this PR.
Add one MR sample and one Property sample that deserialize into typed records.
```

### PR-1：Core typed model + fail-closed validators

**目标**：实现 typed IR 与装载期 validator。

**实现范围**：

```text
MrSpec / PropertySpec / FiveDTags
RunRoleSpec / MethodBinding / TransformStepSpec
ProjectionSpec subclasses
PredicateSpec 10 subclasses
PropertyPredicateSpec 2 subclasses
ParameterExpression subclasses
ToleranceSpec subclasses
ShapeSpec subclasses
FieldPairing subclasses
ValidationResult / ValidationError
ISpecValidator
IPredicateValidator<TPredicate>
IPropertyPredicateValidator<TPredicate>
registry interfaces
```

**首批 validator**：

```text
BinaryComparisonPredicateValidator
BoundPropertyPredicateValidator
Shared reference resolver
Tolerance compatibility checker
ParameterExpression resolver skeleton
```

**验收**：

```text
1. Missing role 被 MrSpec.Validate 拒绝。
2. Missing metric 被 MrSpec.Validate / PropertySpec.Validate 拒绝。
3. Bad MrParameterRef 被 Validate 拒绝。
4. Bad tolerance kind 被 Validate 拒绝。
5. dictionary predicate 被拒绝。
6. 至少 3 个 valid catalog samples 通过 validation。
```

**Codex prompt**：

```text
Implement the v1.2 typed semantic model and fail-closed validators.
Do not delete load-time validation. Typed records are not enough.
Implement MrSpec.Validate and PropertySpec.Validate with role, metric, parameter, transform, derived operator, and tolerance checks.
Add tests for missing role, missing metric, bad parameter ref, bad tolerance, and legacy dictionary predicate rejection.
```

### PR-2：Execution runtime + scalar kernels

**目标**：执行基础 MR。

**实现范围**：

```text
IRunPlanner
ISutRunner abstraction if absent
RoleOutput
VerificationContext
IPredicateDispatcher
IVerifierKernel<TPredicate>
BinaryComparisonKernel
ScaledEqualityPredicate + ScaledEqualityKernel
Deterministic scalar tolerance evaluator
```

**验收**：

```text
1. BinaryComparison pass/fail fixtures。
2. Equal comparison uses tolerance。
3. ScaledEquality computes expected = factor^exponent * reference。
4. Diagnostics include actual / expected / residual / tolerance。
5. Runtime refuses unvalidated spec or clearly requires prior Validate gate。
```

**Codex prompt**：

```text
Implement typed predicate dispatcher and scalar kernels.
Use IVerifierKernel<TPredicate>, not string KernelCode dispatch.
Implement BinaryComparisonKernel and ScaledEqualityKernel with deterministic tolerance.
Ensure runtime assumes Validate has already passed and never repairs spec errors.
Add pass/fail tests and diagnostics assertions.
```

### PR-3：Applicability + 5 态诊断

**目标**：前置条件和结果分桶。

**实现范围**：

```text
ApplicabilitySpec
ConditionExpr
RefExpr
Applicability evaluator
VerifyStatus complete enum
SkippedNotApplicable path
SkippedMissingObservable path
InvalidSpec propagation
DiagnosticContext
```

**验收**：

```text
1. Dif-Phy-11 高硼输入 → SkippedNotApplicable，不调用 SUT。
2. 低硼输入 → 正常执行。
3. 缺失 projection → SkippedMissingObservable。
4. InvalidSpec 不进入 runtime execution。
```

**Codex prompt**：

```text
Implement ApplicabilitySpec evaluation before run planning.
Add VerifyStatus Passed, Failed, SkippedNotApplicable, SkippedMissingObservable, InvalidSpec.
Ensure missing observations are not treated as Failed.
Add tests proving SUT is not invoked when applicability is false.
```

### PR-4：Reference role + convergence

**目标**：G4 收敛 MR。

**实现范围**：

```text
Reference role support
ReferenceArtifact input source
ErrorMonotonicPredicate validator and kernel
NormKind Absolute / Relative / L2 / Linf
ReferenceFractional / Relative tolerance support if needed
```

**验收**：

```text
1. OrderedRoles length >= 2 validation。
2. ReferenceRole validation。
3. Dif-Alg-01 style coarse/fine/reference fixture passes。
4. Non-monotonic error fixture fails and reports offending pair。
```

**Codex prompt**：

```text
Implement ErrorMonotonicPredicate validation and kernel.
Support ordered roles and reference role.
Do not rely on dictionary iteration order.
Report each adjacent error and the offending pair on failure.
```

### PR-5：Sequence、5 个 ShapeSpec、Subadditive、finite difference

**目标**：G6 / G7 / G12，不含 `ExponentialGrowth` runtime。

**实现范围**：

```text
SequenceProjectionSpec
SequenceValue
SequenceShapePredicate
ShapeSpec: BellShape, SShape, SignChange, NonMonotonic, ConstantSlope
SubadditivePredicate
SubadditiveKernel
FiniteDifferenceDerivedExpression
coefficient_of_variation derived operator
```

**验收**：

```text
1. Dif-Phy-09 BellShape sample。
2. Cpl-App-01 SShape sample。
3. Cpl-App-04 SignChange sample。
4. Cpl-App-05 NonMonotonic sample。
5. Cpl-App-07 ConstantSlope sample。
6. Dif-Phy-05 Subadditive sample。
7. ExponentialGrowth validates schema but is not executable until PR-9。
```

**Codex prompt**：

```text
Implement SequenceValue, SequenceProjectionSpec, SequenceShapePredicate for five shapes except ExponentialGrowth.
Implement SubadditivePredicate and finite difference derived metric.
Shape evaluators must return diagnostics with sample_count and worst_point when applicable.
Leave ExponentialGrowth runtime to PR-9.
```

### PR-6：Field、FieldPairing、FieldNormTolerance、DerivedInvariant

**目标**：G8 / G10 + property helper groundwork。

**实现范围**：

```text
Field2DProjectionSpec
Field2DValue
FieldPairing: Identity, Symmetry, Permutation, Scaled
FieldNormToleranceSpec
FieldEqualityPredicate
FieldEqualityKernel
DerivedInvariantPredicate
DerivedInvariantKernel
Derived operators: mass_number_sum, l2_norm, linf_norm, field_region_mean, scalar_subtract
```

**验收**：

```text
1. Dif-Phy-06 symmetry fixture passes/fails。
2. Bur-Phy-01 mass invariant fixture。
3. Field tolerance mismatch rejected by validator。
4. Worst offending field location appears in diagnostics。
5. Dif-Phy-08 derived edge_minus_center can be computed, but Property checker lands in PR-8。
```

**Codex prompt**：

```text
Implement Field2DValue and field predicates.
Field kernels must require FieldNormToleranceSpec unless explicitly overridden by validator policy.
Implement pairings and validate dimensional compatibility.
Add diagnostics with worst offender index and residual.
Implement field_region_mean and scalar_subtract derived operators.
```

### PR-7：Proportional fit、Statistical、Cross-method

**目标**：G3 / G5 / G9 + Bol-Phy-01 完整双谓词。

**实现范围**：

```text
FieldProportionalityPredicate
ConstantEstimator: LeastSquaresThroughOrigin, MedianRatio
VarianceRatioPredicate
StatisticalProjectionSpec
StatisticalValue
StatisticalToleranceSpec
CrossMethodComparisonPredicate
CrossMethodComparisonKernel
MethodBinding validation
```

**验收**：

```text
1. Dif-Phy-03 field proportionality fixture。
2. Bol-Alg-02 variance ratio fixture。
3. Dif-Phy-12/13 cross-method fixture。
4. Cross-method role without MethodBinding rejected。
5. Bol-Phy-01 k_eff invariant + flux scaled fixture passes。
```

**Codex prompt**：

```text
Implement field proportionality, statistical projection/value, variance ratio, and cross-method comparison.
Cross-method predicate must require explicit MethodBinding on both roles.
Do not infer solver difference from transform chain.
```

### PR-8：PropertySpec + 3 executable properties + Kin schema

**目标**：Property 独立路径。

**实现范围**：

```text
IPropertyChecker
PropertyVerificationContext
PropertyPredicateSpec validators
BoundPropertyPredicate checker
ShapePropertyPredicate checker for existing five shapes
PropertyResult / PropertyStatus
Property catalog folder separation
```

**交付 property**：

```text
Dif-Phy-08: derived edge_minus_center < 0 via BoundPropertyPredicate
Bur-Phy-04: ShapePropertyPredicate using existing shape evaluator
Res-Alg-03: two BoundPropertyPredicate assertions for [0,1]
Kin-Phy-02: schema validates, execution pending PR-9
```

**验收**：

```text
1. Property does not enter MR coverage。
2. Property checker does not invoke MR run planner。
3. Res-Alg-03 two bound predicates express [0,1]。
4. Dif-Phy-08 derived field_region_mean + scalar_subtract + bound predicate works。
5. Bur-Phy-04 shape property executable。
6. Kin-Phy-02 validates schema and returns pending/InvalidSpec/SkippedMissingObservable according to repository convention until PR-9。
```

**Codex prompt**：

```text
Implement PropertySpec as separate top-level model.
Do not reuse MrSpec with implicit $only role.
Implement BoundPropertyPredicate and ShapePropertyPredicate using existing derived metric and shape evaluators.
Encode Dif-Phy-08 as a derived scalar bound, not a new property predicate kind.
Ensure property coverage is separate from MR coverage.
```

### PR-9：ExponentialGrowth runtime（v1.2）

**目标**：Kin-Phy-02 可执行。

**实现范围**：

```text
ExponentialGrowth ShapeSpec runtime
LogLinearFit utility
Positive sequence validation
ExpectedRate ParameterExpression evaluation
Shape evaluator integration
ShapePropertyPredicate support
SequenceShapePredicate support if MR ever uses ExponentialGrowth
Diagnostics for fit
```

**验收**：

```text
1. Perfect exponential fixture passes。
2. Noisy exponential within tolerance passes。
3. Wrong rate fails。
4. Non-exponential monotonic curve fails。
5. Non-positive power sample fails with clear diagnostic。
6. Kin-Phy-02 property executable。
```

**Codex prompt**：

```text
Implement ExponentialGrowth for SequenceValue of positive scalar observations.
Use log-linear least squares fit.
Compare fitted rate to ExpectedRate ParameterExpression and check residualRelTolerance plus MinRSquared.
Emit expected_rate, estimated_rate, rate_residual, fit_residual_rel, r_squared, and worst point diagnostics.
Add Kin-Phy-02 fixture and pass/fail tests.
Do not parse arbitrary string math expressions.
```

### PR-10：Catalog migration + golden fixtures + coverage gate

**目标**：47/47 完成并上 CI gate。

**实现范围**：

```text
Migrate 43 MR catalog entries
Migrate 4 Property catalog entries
Golden fixtures for validation and execution
Coverage report by MR vs Property
Legacy migration snapshots
CI gate: validate all catalog specs
CI gate: run all runnable fixtures
```

**验收**：

```text
1. 43/43 MR Validate() pass。
2. 4/4 Property Validate() pass。
3. 47/47 schema coverage pass。
4. 47/47 expected executable fixtures pass where SUT fixture is available。
5. MR coverage and Property coverage are reported separately。
6. No catalog entry uses KernelCode + Dictionary predicate。
7. No catalog entry uses legacy assertion_name。
8. No unregistered transform / derived operator remains。
9. Golden fixtures cover pass/fail/missing/invalid-spec relevant cases。
10. InvalidSpec count is 0 after migration。
```

**Codex prompt**：

```text
Migrate the PWR MR and Property catalog to v1.2 typed schema.
Add golden fixtures and CI gates.
Do not mark the migration complete until 43/43 MR and 4/4 Property Validate() pass.
Ensure report separates MR coverage from Property coverage.
Reject legacy assertion_name and KernelCode + Dictionary predicate in tests.
Ensure PR-9 ExponentialGrowth is used for Kin-Phy-02 and is executable.
```

---

## 17. CI gates

Before PR-10 merge, CI must include:

```text
1. Schema deserialization gate:
   - all catalog files deserialize into typed records.

2. Static validation gate:
   - all MrSpec.Validate() and PropertySpec.Validate() pass.

3. Anti-regression schema gate:
   - reject KernelCode / RoleBindings / ProjectionBindings / kernel_code / role_bindings / projection_bindings in predicate nodes.
   - reject assertion_name.

4. Registry gate:
   - all transform operators registered.
   - all derived operators registered.
   - all predicate validators registered.
   - all kernels/checkers registered.

5. Coverage gate:
   - MR coverage denominator = 43.
   - Property coverage denominator = 4.
   - 47/47 catalog entries schema-covered.

6. Fixture gate:
   - all golden validation fixtures pass.
   - all runnable execution fixtures pass.
   - missing observable fixtures return SkippedMissingObservable.
   - bad spec fixtures return InvalidSpec at validation stage.

7. Reporting gate:
   - failure reports include predicate id, kind, roles, metric, residual, tolerance, diagnostics.
   - field / sequence failures include worst offender.
```

---

## 18. Acceptance criteria

Final implementation must satisfy:

```text
1. 43/43 PWR MR can be encoded as typed MrSpec.
2. 4/4 Property can be encoded as typed PropertySpec and do not enter MR coverage.
3. Catalog load rejects wrong role / projection / parameter / predicate / tolerance combinations.
4. Typed union is used for PredicateSpec and PropertyPredicateSpec; no KernelCode + Dictionary.
5. source/followup legacy interface is not the core model.
6. N 元 role、sequence、field、reference、method variant 都有类型化建模。
7. Runtime failure report includes mr_id/property_id, predicate id, predicate kind, roles, metric, actual, expected, residual, tolerance, worst offenders。
8. New MR / Property入库必须通过 §11 自检表。
9. PR-10 跑通后，5 态报表覆盖全部 47 项。
10. v1.1 的“删除装载期 ValidateSpec / 类型系统自动保证引用”说法不得出现在代码注释、docs 或 PR 描述中。
```

---

## 19. Stage 9 / NOETHER 对齐

v1.2 是 Stage 9 MetaPattern / NOETHER compiler 的下游执行 IR。

```text
MetaPattern discovery / compiler
  ↓
Typed MrSpec / PropertySpec
  ↓
MrSpec.Validate / PropertySpec.Validate
  ↓
Runtime verifier kernels / property checkers
  ↓
VerifyResult / PropertyResult
```

Stage 9 不替代本 IR；它只生成本 IR。

Mapping：

| NOETHER / MetaPattern | v1.2 target |
|---|---|
| Invariance | `BinaryComparisonPredicate(Equal)` / `DerivedInvariantPredicate` |
| Monotonicity | `BinaryComparisonPredicate` |
| Homogeneity / scaling | `ScaledEqualityPredicate` |
| Convergence | `ErrorMonotonicPredicate` |
| Trajectory shape | `SequenceShapePredicate` / `ShapePropertyPredicate` |
| Subadditivity | `SubadditivePredicate` |
| Symmetry | `FieldEqualityPredicate` + `FieldPairing` |
| Cross-method consistency | `CrossMethodComparisonPredicate` |
| Range property | `BoundPropertyPredicate` |

---

## 20. Final instruction to Codex

Use this exact instruction when starting a Codex session:

```text
You are implementing docs/2026-05-25-mr-verification-v1.2-codex-ready.md.

The canonical architecture is:
- typed PredicateSpec discriminated union;
- typed PropertyPredicateSpec discriminated union;
- fail-closed MrSpec.Validate and PropertySpec.Validate at catalog load;
- IVerifierKernel<TPredicate> only for runtime verification after validation;
- no KernelCode + Dictionary predicate representation;
- no arbitrary expression DSL;
- MR and Property catalogs are separate;
- PR-0 through PR-10 must be implemented in order unless explicitly directed otherwise.

Do not claim typed records automatically guarantee role/projection/parameter references.
Those checks must be implemented in validators and CI gates.
```

