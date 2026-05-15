using FluentAssertions;

namespace MetBench_BLL.SystemMT.Assertions;

/// <summary>
/// MT 断言调度器 — 按 AssertionTypeCode 字符串分派到具体 FA 扩展方法，
/// 捕获失败异常包装为 <see cref="SystemMtAssertionResultV2"/>。
/// </summary>
/// <remarks>
/// 设计选择 switch 而非 dictionary 注册：见 docs/design/assertion-extensions.md §10.3。
///
/// 加新 AssertionType 改动 ≤3 处：
///   1. AssertionTypeCodes 加常量
///   2. MetbenchAssertionExtensions 加扩展方法
///   3. 本类 switch 加 case
/// </remarks>
public sealed class AssertionEvaluator
{
    /// <summary>
    /// 执行断言。Throw 不会逃逸；失败和成功都返回 <see cref="SystemMtAssertionResultV2"/>。
    /// </summary>
    public SystemMtAssertionResultV2 Evaluate(
        AssertionInput input,
        AssertionTolerance tolerance,
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
                    if (input.ExtraValues is null ||
                        !input.ExtraValues.TryGetValue("refinement_factor", out var refFactor))
                    {
                        throw new ArgumentException(
                            "variance-ratio assertion requires ExtraValues['refinement_factor']");
                    }
                    input.FollowupStd.Should().HaveVarianceRatio(
                        sourceStd: input.SourceStd,
                        refinementFactor: refFactor,
                        tolerance: tolerance.ToleranceRel,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.FluxPointwiseApprox:
                    var srcFlux = ExtractArray(input.ExtraValues, "source_flux");
                    var flwFlux = ExtractArray(input.ExtraValues, "followup_flux");
                    flwFlux.Should().BePointwiseApproximately(
                        source: srcFlux,
                        toleranceRel: tolerance.ToleranceRel,
                        toleranceAbs: tolerance.ToleranceAbs,
                        because: becauseReason ?? "");
                    break;

                case AssertionTypeCodes.CrossProgramAgree:
                    if (input.ExtraValues is null ||
                        !input.ExtraValues.TryGetValue("reference_value", out var refValue))
                    {
                        throw new ArgumentException(
                            "cross-program-agree assertion requires ExtraValues['reference_value']");
                    }
                    var refStd = input.ExtraValues.TryGetValue("reference_std", out var rs) ? rs : 0.0;
                    input.FollowupValue.Should().AgreeWithReference(
                        reference: refValue,
                        actualStd: input.FollowupStd,
                        referenceStd: refStd,
                        toleranceRel: tolerance.ToleranceRel,
                        noiseMultiplier: tolerance.NoiseMultiplier,
                        because: becauseReason ?? "");
                    break;

                default:
                    return SystemMtAssertionResultV2.UnknownType(assertionTypeCode);
            }

            return SystemMtAssertionResultV2.PassedResult(input, assertionTypeCode);
        }
        catch (Exception ex) when (IsAssertionFailure(ex) || ex is ArgumentException)
        {
            return SystemMtAssertionResultV2.FailedResult(input, assertionTypeCode, ex.Message);
        }
    }

    /// <summary>
    /// 识别 FluentAssertions 的断言失败异常类型。
    /// FA 6 在不同测试框架下的 fallback 类型不同 (XunitException / NUnitException /
    /// AssertionFailedException / etc.)，统一用 type name 判断避免引入测试框架依赖。
    /// </summary>
    private static bool IsAssertionFailure(Exception ex)
    {
        var name = ex.GetType().Name;
        return name.Contains("AssertionFailed", StringComparison.Ordinal)
            || name.Contains("XunitException", StringComparison.Ordinal)
            || name.Contains("NUnitException", StringComparison.Ordinal)
            || name == "Exception";  // FA 在无测试框架时 throw new Exception
    }

    private static IEnumerable<double> ExtractArray(
        IReadOnlyDictionary<string, double>? extras, string keyPrefix)
    {
        if (extras is null)
            throw new ArgumentException($"{keyPrefix} array missing from ExtraValues");
        var result = new List<double>();
        int i = 0;
        while (extras.TryGetValue($"{keyPrefix}_{i}", out var v))
        {
            result.Add(v);
            i++;
        }
        if (result.Count == 0)
            throw new ArgumentException(
                $"{keyPrefix} array must have at least 1 element (keyed {keyPrefix}_0...)");
        return result;
    }
}
