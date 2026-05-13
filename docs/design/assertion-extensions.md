# MetBench v2 断言扩展方法 API 参考

> MT 特有断言通过 FluentAssertions 扩展方法实现。
> API 风格与 FluentAssertions 原生 100% 一致 — `value.Should().BeXxx(...)`。
> 不引入新接口（如 `IMrAssertion`），不引入 wrapper 类。
> 失败时 throw `AssertionFailedException`，pipeline 端 catch 包装成 `Result.FailureReason`。

---

## 1. 依赖

```xml
<!-- MetBench_BLL.Core/MetBench_BLL.Core.csproj -->
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="MathNet.Numerics" Version="5.0.0" />
```

体积：FA ~700 KB；MathNet ~3 MB。

---

## 2. Namespace 与文件组织

```
MetBench_BLL.Core/SystemMT/Assertions/
├── MetbenchAssertionExtensions.cs          ← 扩展方法集中实现
├── AssertionInput.cs                        ← 输入 record
├── ToleranceConfig.cs                       ← 容差配置 record
├── AssertionEvaluator.cs                    ← 调度器（按 AssertionTypeCode 分派）
└── AssertionTypeCodes.cs                    ← 字符串常量集
```

```csharp
namespace MetBench_BLL.Core.SystemMT.Assertions;
```

---

## 3. 输入 / 配置 record

```csharp
public record AssertionInput(
    double SourceValue,
    double SourceStd,
    double FollowupValue,
    double FollowupStd,
    string ValueName,
    Dictionary<string, double>? ExtraValues = null
);

public record ToleranceConfig(
    bool NoiseAware = false,
    double ToleranceRel = 0.0,
    double ToleranceAbs = 0.0,
    double NoiseMultiplier = 3.0
);
```

---

## 4. 扩展方法清单

| 方法 | 用途 | MetaPattern |
|------|------|------------|
| `BeLessThan` | 普通小于（FA 原生） | m_mono |
| `BeGreaterThan` | 普通大于（FA 原生） | m_mono |
| `BeApproximately` | 普通近似（FA 原生） | m_inv |
| **`BeLessThanWithNoiseFloor`** | MT 单调性（向下，含噪声底） | m_mono / 概率 SUT |
| **`BeGreaterThanWithNoiseFloor`** | MT 单调性（向上，含噪声底） | m_mono / 概率 SUT |
| **`BeApproximatelyEqualUnderTransform`** | MT 不变性（含容差 + 噪声底） | m_inv |
| **`HaveVarianceRatio`** | MT 收敛性（σ ∝ 1/√N） | m_conv |
| **`BePointwiseApproximately`** | MT 数组逐元素近似 | m_inv tally |
| **`AgreeWithReference`** | MT 跨程序一致性（k_A ≈ k_B） | m_cmp |

加粗 = MetBench 新增。

---

## 5. 扩展方法实现

### 5.1 完整代码 — `MetbenchAssertionExtensions.cs`

```csharp
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Numeric;
using FluentAssertions.Collections;

namespace MetBench_BLL.Core.SystemMT.Assertions;

/// <summary>
/// FluentAssertions extension methods carrying MetBench-specific MT semantics.
/// All methods preserve the FluentAssertions API style:
/// <c>value.Should().BeXxx(...)</c>, throw <see cref="AssertionFailedException"/>
/// on failure, return <see cref="AndConstraint{T}"/> for chaining.
/// </summary>
public static class MetbenchAssertionExtensions
{
    // ===== Noise-aware monotonicity =====

    /// <summary>
    /// MT single-direction monotonicity (decreasing).
    /// <para>Asserts: followup value &lt; source value − noise_floor,
    /// where noise_floor = max(<paramref name="noiseMultiplier"/>·√(σ_src² + σ_flw²),
    /// <paramref name="toleranceRel"/>·|source|).</para>
    /// </summary>
    /// <param name="assertions">Followup value assertions.</param>
    /// <param name="source">Source value.</param>
    /// <param name="sourceStd">Source stdev (use 0 for deterministic SUTs).</param>
    /// <param name="followupStd">Followup stdev.</param>
    /// <param name="toleranceRel">Relative tolerance fraction (e.g. 0.001).</param>
    /// <param name="noiseMultiplier">Sigma multiplier (default 3 for 3σ).</param>
    public static AndConstraint<NumericAssertions<double>> BeLessThanWithNoiseFloor(
        this NumericAssertions<double> assertions,
        double source,
        double sourceStd = 0.0,
        double followupStd = 0.0,
        double toleranceRel = 0.0,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
        var noise = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(source));
        var threshold = source - noise;
        var actual = (double)assertions.Subject!;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual < threshold)
            .FailWith(
                "Expected {context:followup value} to be less than {0:F5} " +
                "(= source {1:F5} − noise {2:F5}) {reason}, but found {3:F5}.",
                threshold, source, noise, actual);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }

    /// <summary>Symmetric counterpart: followup &gt; source + noise.</summary>
    public static AndConstraint<NumericAssertions<double>> BeGreaterThanWithNoiseFloor(
        this NumericAssertions<double> assertions,
        double source,
        double sourceStd = 0.0,
        double followupStd = 0.0,
        double toleranceRel = 0.0,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
        var noise = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(source));
        var threshold = source + noise;
        var actual = (double)assertions.Subject!;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual > threshold)
            .FailWith(
                "Expected {context:followup value} to be greater than {0:F5} " +
                "(= source {1:F5} + noise {2:F5}) {reason}, but found {3:F5}.",
                threshold, source, noise, actual);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }

    // ===== Invariance under transform =====

    /// <summary>
    /// MT invariance: |followup − source| ≤ max(3σ, toleranceRel·|source|).
    /// </summary>
    public static AndConstraint<NumericAssertions<double>> BeApproximatelyEqualUnderTransform(
        this NumericAssertions<double> assertions,
        double source,
        double sourceStd = 0.0,
        double followupStd = 0.0,
        double toleranceRel = 0.001,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
        var bound = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(source));
        var actual = (double)assertions.Subject!;
        var delta = actual - source;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Math.Abs(delta) <= bound)
            .FailWith(
                "Expected {context:followup value} ≈ {0:F5} within ±{1:F5} {reason}, " +
                "but found {2:F5} (Δ = {3:F5}).",
                source, bound, actual, delta);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }

    // ===== Variance ratio (convergence) =====

    /// <summary>
    /// MT convergence rate: σ_followup / σ_source ≈ 1/√(refinementFactor),
    /// within <paramref name="tolerance"/> (absolute).
    /// <para>Used by m_conv MRs such as MR12 (RefineParticles for OpenMC).</para>
    /// </summary>
    /// <param name="followupStdAssertions">Followup stdev assertions.</param>
    /// <param name="sourceStd">Source stdev (must be &gt; 0).</param>
    /// <param name="refinementFactor">Refinement ratio (e.g. 4 for 4× more particles).</param>
    /// <param name="tolerance">Absolute tolerance on σ-ratio.</param>
    public static AndConstraint<NumericAssertions<double>> HaveVarianceRatio(
        this NumericAssertions<double> followupStdAssertions,
        double sourceStd,
        double refinementFactor,
        double tolerance = 0.1,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(sourceStd > 0)
            .FailWith("HaveVarianceRatio requires source stdev > 0, got {0}.", sourceStd);

        var expectedRatio = 1.0 / Math.Sqrt(refinementFactor);
        var actualRatio = (double)followupStdAssertions.Subject! / sourceStd;
        var delta = Math.Abs(actualRatio - expectedRatio);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(delta <= tolerance)
            .FailWith(
                "Expected σ-ratio ≈ {0:F3} (= 1/√{1}) within ±{2:F3} {reason}, " +
                "but found {3:F3} (Δ = {4:F3}).",
                expectedRatio, refinementFactor, tolerance, actualRatio, delta);

        return new AndConstraint<NumericAssertions<double>>(followupStdAssertions);
    }

    // ===== Pointwise array approximation =====

    /// <summary>
    /// MT pointwise invariance on arrays (e.g. per-cell flux tally).
    /// </summary>
    public static AndConstraint<GenericCollectionAssertions<double>> BePointwiseApproximately(
        this GenericCollectionAssertions<double> assertions,
        IEnumerable<double> source,
        double toleranceRel = 0.01,
        double toleranceAbs = 0.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sourceArr = source.ToArray();
        var actualArr = assertions.Subject.ToArray();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(sourceArr.Length == actualArr.Length)
            .FailWith(
                "Pointwise approx requires equal lengths {reason}, got {0} vs {1}.",
                sourceArr.Length, actualArr.Length);

        for (int i = 0; i < sourceArr.Length; i++)
        {
            var bound = Math.Max(toleranceAbs, toleranceRel * Math.Abs(sourceArr[i]));
            var delta = Math.Abs(actualArr[i] - sourceArr[i]);
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(delta <= bound)
                .FailWith(
                    "Pointwise mismatch at index {0} {reason}: expected ≈ {1:F5} ± {2:F5}, " +
                    "found {3:F5} (Δ = {4:F5}).",
                    i, sourceArr[i], bound, actualArr[i], delta);
        }

        return new AndConstraint<GenericCollectionAssertions<double>>(assertions);
    }

    // ===== Cross-program agreement (m_cmp) =====

    /// <summary>
    /// MT cross-program agreement: |actual − reference| ≤ max(3σ_composite, toleranceRel·|reference|).
    /// Designed for m_cmp MRs (e.g. OpenMOC vs OpenMC k_eff agreement).
    /// </summary>
    public static AndConstraint<NumericAssertions<double>> AgreeWithReference(
        this NumericAssertions<double> assertions,
        double reference,
        double actualStd = 0.0,
        double referenceStd = 0.0,
        double toleranceRel = 0.01,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(actualStd * actualStd + referenceStd * referenceStd);
        var bound = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(reference));
        var actual = (double)assertions.Subject!;
        var delta = actual - reference;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Math.Abs(delta) <= bound)
            .FailWith(
                "Expected {context:value} ≈ reference {0:F5} within ±{1:F5} " +
                "(toleranceRel={2:F4}, 3σ={3:F5}) {reason}, but found {4:F5} (Δ = {5:F5}).",
                reference, bound, toleranceRel, noiseMultiplier * sigmaComposite, actual, delta);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }
}
```

---

## 6. AssertionTypeCodes 常量

```csharp
public static class AssertionTypeCodes
{
    public const string Less                = "less";
    public const string Greater             = "greater";
    public const string Approx              = "approx";
    public const string LessNoiseAware      = "less-noise-aware";
    public const string GreaterNoiseAware   = "greater-noise-aware";
    public const string ApproxInvariant     = "approx-invariant";
    public const string VarianceRatio       = "variance-ratio";
    public const string FluxPointwiseApprox = "flux-pointwise-approx";
    public const string CrossProgramAgree   = "cross-program-agree";
}
```

LiteDB `MetamorphicRelation.AssertionTypeCode` 字段使用这些常量；UI 下拉框、`.feature` tag 都引用同一份。

---

## 7. AssertionEvaluator — 调度器

```csharp
public sealed class AssertionEvaluator
{
    public SystemMtAssertionResult Evaluate(
        AssertionInput input,
        ToleranceConfig tolerance,
        string assertionTypeCode,
        string? becauseReason = null)
    {
        try
        {
            switch (assertionTypeCode)
            {
                case AssertionTypeCodes.Less:
                    input.FollowupValue.Should().BeLessThan(input.SourceValue,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.Greater:
                    input.FollowupValue.Should().BeGreaterThan(input.SourceValue,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.Approx:
                    var basicBound = Math.Max(tolerance.ToleranceAbs,
                        tolerance.ToleranceRel * Math.Abs(input.SourceValue));
                    input.FollowupValue.Should().BeApproximately(input.SourceValue, basicBound,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.LessNoiseAware:
                    input.FollowupValue.Should().BeLessThanWithNoiseFloor(
                        source: input.SourceValue,
                        sourceStd: input.SourceStd,
                        followupStd: input.FollowupStd,
                        toleranceRel: tolerance.ToleranceRel,
                        noiseMultiplier: tolerance.NoiseMultiplier,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.GreaterNoiseAware:
                    input.FollowupValue.Should().BeGreaterThanWithNoiseFloor(
                        source: input.SourceValue,
                        sourceStd: input.SourceStd,
                        followupStd: input.FollowupStd,
                        toleranceRel: tolerance.ToleranceRel,
                        noiseMultiplier: tolerance.NoiseMultiplier,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.ApproxInvariant:
                    input.FollowupValue.Should().BeApproximatelyEqualUnderTransform(
                        source: input.SourceValue,
                        sourceStd: input.SourceStd,
                        followupStd: input.FollowupStd,
                        toleranceRel: tolerance.ToleranceRel,
                        noiseMultiplier: tolerance.NoiseMultiplier,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.VarianceRatio:
                    var refinementFactor = input.ExtraValues?["refinement_factor"]
                        ?? throw new ArgumentException("variance-ratio requires extra 'refinement_factor'");
                    input.FollowupStd.Should().HaveVarianceRatio(
                        sourceStd: input.SourceStd,
                        refinementFactor: refinementFactor,
                        tolerance: tolerance.ToleranceRel,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.FluxPointwiseApprox:
                    var srcFlux = ExtractArrayFromExtras(input.ExtraValues, "source_flux");
                    var flwFlux = ExtractArrayFromExtras(input.ExtraValues, "followup_flux");
                    flwFlux.Should().BePointwiseApproximately(
                        source: srcFlux,
                        toleranceRel: tolerance.ToleranceRel,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.CrossProgramAgree:
                    var reference = input.ExtraValues?["reference_value"]
                        ?? throw new ArgumentException("cross-program-agree requires 'reference_value'");
                    var referenceStd = input.ExtraValues?.GetValueOrDefault("reference_std", 0.0) ?? 0.0;
                    input.FollowupValue.Should().AgreeWithReference(
                        reference: reference,
                        actualStd: input.FollowupStd,
                        referenceStd: referenceStd,
                        toleranceRel: tolerance.ToleranceRel,
                        noiseMultiplier: tolerance.NoiseMultiplier,
                        because: becauseReason ?? "");
                    break;

                default:
                    return SystemMtAssertionResult.UnknownType(assertionTypeCode);
            }

            return SystemMtAssertionResult.Passed(input, assertionTypeCode);
        }
        catch (Exception ex) when (ex.GetType().Name == "AssertionFailedException")
        {
            return SystemMtAssertionResult.Failed(input, assertionTypeCode, ex.Message);
        }
    }

    private static IEnumerable<double> ExtractArrayFromExtras(
        Dictionary<string, double>? extras, string keyPrefix)
    {
        if (extras is null) throw new ArgumentException($"{keyPrefix} array missing");
        var result = new List<double>();
        int i = 0;
        while (extras.TryGetValue($"{keyPrefix}_{i}", out var v))
        {
            result.Add(v);
            i++;
        }
        return result;
    }
}
```

---

## 8. SystemMtAssertionResult — 返回类型

```csharp
public record SystemMtAssertionResult(
    string AssertionTypeCode,
    bool Passed,
    double? SourceValue,
    double? FollowupValue,
    double? ObservedDelta,
    double? ExpectedThreshold,
    string Expression,                 // 人类可读表达式
    string? FailureReason               // 失败时 FA 的 message
)
{
    public static SystemMtAssertionResult Passed(AssertionInput input, string code) =>
        new(
            code,
            true,
            input.SourceValue,
            input.FollowupValue,
            input.FollowupValue - input.SourceValue,
            null,
            $"PASS: {code} on '{input.ValueName}'",
            null
        );

    public static SystemMtAssertionResult Failed(AssertionInput input, string code, string failureMessage) =>
        new(
            code,
            false,
            input.SourceValue,
            input.FollowupValue,
            input.FollowupValue - input.SourceValue,
            null,
            $"FAIL: {code} on '{input.ValueName}'",
            failureMessage
        );

    public static SystemMtAssertionResult UnknownType(string code) =>
        new(
            code,
            false,
            null,
            null,
            null,
            null,
            $"UNKNOWN: assertion type '{code}' not registered",
            $"Unknown assertion type code: {code}"
        );
}
```

---

## 9. 单元测试示例

```csharp
public class MetbenchAssertionExtensionsTests
{
    [Fact]
    public void BeLessThanWithNoiseFloor_Passes_When_DeltaExceedsNoise()
    {
        var followup = 0.5;
        var source = 1.0;
        var act = () => followup.Should().BeLessThanWithNoiseFloor(
            source: source,
            sourceStd: 0.001,
            followupStd: 0.001,
            toleranceRel: 0.0,
            noiseMultiplier: 3.0);
        act.Should().NotThrow();
    }

    [Fact]
    public void BeLessThanWithNoiseFloor_Fails_When_WithinNoiseFloor()
    {
        var followup = 0.999;
        var source = 1.0;
        var act = () => followup.Should().BeLessThanWithNoiseFloor(
            source: source,
            sourceStd: 0.01,
            followupStd: 0.01,
            toleranceRel: 0.0,
            noiseMultiplier: 3.0);
        act.Should().Throw<Exception>()  // AssertionFailedException
            .WithMessage("*0.999*less than*");
    }

    [Fact]
    public void HaveVarianceRatio_PassesNear_1OverSqrt4_ForFactor4()
    {
        var sourceStd = 0.002;
        var followupStd = 0.001;        // sqrt-ratio = 0.5, expected = 1/√4 = 0.5
        var act = () => followupStd.Should().HaveVarianceRatio(sourceStd, 4.0, 0.1);
        act.Should().NotThrow();
    }

    // ... 更多 case
}
```

---

## 10. 设计权衡

### 10.1 为什么不用 `IMrAssertion` 接口？

| 方案 | 优 | 劣 |
|------|---|-----|
| `IMrAssertion` + 6 个实现类 + DI | C# 工程化，OO 经典 | 重新发明 FA；多写 ~400 行；DI 注册麻烦 |
| **FA 扩展方法 + 1 个 Evaluator** | API 一致；错误消息友好；行数少 | 失去"接口可替换"的形式自由 |

后者胜出：实际上"接口可替换"没有真实需求 — 我们的断言种类是稳定的代数性质集合，不会运行时替换实现。

### 10.2 为什么不直接在 .feature step bindings 里用 `Should()`？

```gherkin
Then the noise-aware "less" assertion holds on "k_eff"
```

step binding：

```csharp
[Then(@"the (noise-aware )?""(.*)"" assertion holds on ""(.*)""")]
public void ThenAssertionHolds(string noiseAware, string assertionType, string value)
{
    var input = _ctx.GetAssertionInput(value);
    var tol = _ctx.GetTolerance();
    var code = noiseAware switch
    {
        "noise-aware " => $"{assertionType}-noise-aware",
        _ => assertionType
    };
    var result = _evaluator.Evaluate(input, tol, code);
    result.Passed.Should().BeTrue(result.FailureReason);
}
```

step binding 调 `AssertionEvaluator.Evaluate()`，**而不是直接调扩展方法** — 因为 step binding 不知道具体哪种 assertion。这就是 Evaluator 存在的价值。

### 10.3 为什么 AssertionEvaluator 用 switch 而不是 dictionary?

| switch | dictionary |
|--------|----------|
| 编译时检查 case 完整性 | 运行时 lookup |
| 强类型每个 case 的参数 | 需要统一参数容器（牺牲类型安全） |
| 加新 assertion 改 1 处 | 加新 assertion 改 2 处（dict 注册 + 实现） |

新 assertion 几乎不会被加（核心代数性质有限），switch 反而更稳。

---

## 11. 加新断言的标准流程

```
1. 在 MetbenchAssertionExtensions.cs 加新扩展方法
   public static AndConstraint<...> BeXxx(this ..., ..., string because = "", ...)

2. 在 AssertionTypeCodes.cs 加新常量
   public const string Xxx = "xxx";

3. 在 AssertionEvaluator.Evaluate switch 加 case

4. 加单元测试

5. 加文档（本文件 §4 表格）
```

无需改 Pipeline / Repository / WPF UI / step bindings。

---

## 12. 与既有 `IMrAssertion` 的迁移

```
旧 (Stage 4)                                    新 (v2)
─────────────────────────                       ─────────────────────────
IMrAssertion 接口                                ☓ 废弃
  GreaterThanAssertion 类                        ☓ 废弃（FA.BeGreaterThan 原生）
  LessThanAssertion 类                           ☓ 废弃（FA.BeLessThan 原生）

SystemMtRunner 内部                              SystemMtPipeline 内部
  _assertions["less"].Evaluate(...)              _evaluator.Evaluate(input, tol, "less")
                                                 → 内部走 BeLessThan 或 BeLessThanWithNoiseFloor
```

迁移工时：~1 工日（约 200 行代码删除 + 300 行新代码 + 单元测试更新）。

---

**本 API 参考与 [`glossary.md`](glossary.md) §8 同步。任何 AssertionTypeCode / 扩展方法签名变更需先改本文件。**
