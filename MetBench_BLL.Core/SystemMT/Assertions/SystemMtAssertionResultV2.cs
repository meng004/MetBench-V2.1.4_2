namespace MetBench_BLL.SystemMT.Assertions;

/// <summary>
/// v2 断言结果（不与 Stage 4 SystemMtAssertionResult 冲突；
/// 后者由 v1 IMrAssertion 体系产出）。
/// </summary>
/// <param name="AssertionTypeCode">用了哪种 AssertionTypeCode。</param>
/// <param name="Passed">是否通过。</param>
/// <param name="SourceValue">源值副本。</param>
/// <param name="FollowupValue">后继值副本。</param>
/// <param name="ObservedDelta">观察到的 Δ (followup - source)。</param>
/// <param name="ExpectedThreshold">期望阈值（如 noise floor）；可空。</param>
/// <param name="Expression">人类可读断言表达式。</param>
/// <param name="FailureReason">失败时 FA 的错误消息；通过则 null。</param>
public sealed record SystemMtAssertionResultV2(
    string AssertionTypeCode,
    bool Passed,
    double? SourceValue,
    double? FollowupValue,
    double? ObservedDelta,
    double? ExpectedThreshold,
    string Expression,
    string? FailureReason)
{
    public static SystemMtAssertionResultV2 PassedResult(
        AssertionInput input, string code) =>
        new(
            code,
            true,
            input.SourceValue,
            input.FollowupValue,
            input.FollowupValue - input.SourceValue,
            null,
            $"PASS: {code} on '{input.ValueName}'",
            null);

    public static SystemMtAssertionResultV2 FailedResult(
        AssertionInput input, string code, string failureMessage) =>
        new(
            code,
            false,
            input.SourceValue,
            input.FollowupValue,
            input.FollowupValue - input.SourceValue,
            null,
            $"FAIL: {code} on '{input.ValueName}'",
            failureMessage);

    public static SystemMtAssertionResultV2 UnknownType(string code) =>
        new(
            code,
            false,
            null,
            null,
            null,
            null,
            $"UNKNOWN: assertion type '{code}' not registered",
            $"Unknown assertion type code: {code}");
}
