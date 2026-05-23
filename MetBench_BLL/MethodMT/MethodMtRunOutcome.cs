namespace MetBench_BLL.MethodMT;

/// <summary>方法级 MT 单次执行结果。</summary>
public sealed record MethodMtRunOutcome(
    bool Passed,
    string MrCode,
    string AssertionTypeCode,
    double SourceValue,
    double FollowupValue,
    IReadOnlyDictionary<string, object?> SourceInput,
    IReadOnlyDictionary<string, object?> FollowupInput,
    string? ErrorMessage);
