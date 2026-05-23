using SysMath = System.Math;

namespace MetBench_BLL.MethodMT.Assertions;

/// <summary>
/// 方法级 MT 断言评估器 — 支持三种基础断言：<c>less</c> / <c>greater</c> / <c>approx</c>。
/// 噪声感知及系统级专属断言代码一律拒绝（抛 <see cref="ArgumentException"/>）。
/// </summary>
/// <remarks>
/// 设计见 docs/design/mr-architecture.md §3.3（断言层纪律）：方法级 MT 不含噪声模型，
/// 使用 FluentAssertions 原生对比即可，不应引用系统级的概率断言扩展。
/// </remarks>
public sealed class MethodAssertionEvaluator
{
    private static readonly IReadOnlySet<string> Allowed =
        new HashSet<string> { "less", "greater", "approx" };

    /// <summary>
    /// 评估方法级断言。
    /// </summary>
    /// <param name="assertionTypeCode">断言类型代码（仅接受 less/greater/approx）。</param>
    /// <param name="sourceValue">源执行结果。</param>
    /// <param name="followupValue">衍生执行结果。</param>
    /// <param name="tolerance">approx 的绝对容差；less/greater 忽略此参数。</param>
    /// <returns>评估结果。</returns>
    /// <exception cref="ArgumentException">传入不支持的断言代码时抛出。</exception>
    public MethodMtAssertionResult Evaluate(
        string assertionTypeCode,
        double sourceValue,
        double followupValue,
        double tolerance = 0.0)
    {
        if (!Allowed.Contains(assertionTypeCode))
            throw new ArgumentException(
                $"MethodAssertionEvaluator does not support '{assertionTypeCode}'. " +
                $"Allowed: [{string.Join(", ", Allowed)}]. " +
                "Noise-aware and system-level assertions are reserved for system-level MT " +
                "(mr-architecture §3.3).",
                nameof(assertionTypeCode));

        var passed = assertionTypeCode switch
        {
            "less"    => followupValue < sourceValue,
            "greater" => followupValue > sourceValue,
            "approx"  => SysMath.Abs(followupValue - sourceValue) <= tolerance,
            _         => throw new InvalidOperationException($"Unhandled code: {assertionTypeCode}"),
        };

        return new MethodMtAssertionResult(passed, assertionTypeCode, sourceValue, followupValue);
    }
}

/// <summary>方法级 MT 断言评估结果。</summary>
public readonly record struct MethodMtAssertionResult(
    bool Passed,
    string Code,
    double SourceValue,
    double FollowupValue);
