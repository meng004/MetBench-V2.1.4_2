namespace MetBench_BLL.SystemMT;

/// <summary>
/// 等式 MR 断言 —— 源输出与衍生输出在容差内相等。用于 MP_inv（不变性 / 守恒）类
/// MR：变换后输出应与源输出严格相等（几何对称、守恒律、自伴算子等）。
/// </summary>
/// <remarks>
/// 判定（numpy <c>isclose</c> 风格，绝对 + 相对合一）：
/// <code>|flw - src| &lt;= atol + rtol * |src|</code>
/// 容差由 <see cref="EqualityThresholds"/> 注入；未注入回退 <see cref="EqualityThresholds.Default"/>。
/// 缩放等式（<c>flw ≈ k·src</c>，齐次类 MR）需变换因子 k，超出本断言职责 —— 见
/// 计划 <c>2026-05-22-mr-program-metadata-persistence-plan.md</c> DP-2。
/// </remarks>
public sealed class ApproxEqualAssertion : IMrAssertion
{
    private readonly EqualityThresholds _thresholds;

    public ApproxEqualAssertion(EqualityThresholds? thresholds = null)
        => _thresholds = thresholds ?? EqualityThresholds.Default;

    public string Name => "ApproxEqual";

    public SystemMtAssertionResult Evaluate(string valueName, ParsedOutput source, ParsedOutput followUp)
    {
        if (!source.Values.TryGetValue(valueName, out var sourceValue))
        {
            return new SystemMtAssertionResult(
                Name, valueName, double.NaN, double.NaN, false,
                $"Assertion failure: Missing parsed value '{valueName}' in source output");
        }

        if (!followUp.Values.TryGetValue(valueName, out var followUpValue))
        {
            return new SystemMtAssertionResult(
                Name, valueName, sourceValue, double.NaN, false,
                $"Assertion failure: Missing parsed value '{valueName}' in follow-up output");
        }

        var tolerance = _thresholds.AbsoluteTolerance
            + _thresholds.RelativeTolerance * Math.Abs(sourceValue);
        var delta = Math.Abs(followUpValue - sourceValue);
        var passed = delta <= tolerance;

        return new SystemMtAssertionResult(
            Name, valueName, sourceValue, followUpValue, passed,
            passed
                ? string.Empty
                : $"Assertion failure: |{followUpValue} - {sourceValue}| = {delta} exceeds tolerance " +
                  $"{tolerance} (atol={_thresholds.AbsoluteTolerance} + rtol={_thresholds.RelativeTolerance}·|src|) " +
                  $"for '{valueName}'");
    }
}
