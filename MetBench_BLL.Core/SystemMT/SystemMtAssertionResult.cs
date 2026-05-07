namespace MetBench_BLL.SystemMT;

public sealed record SystemMtAssertionResult(
    string AssertionName,
    string ValueName,
    double SourceValue,
    double FollowUpValue,
    bool Passed,
    string FailureReason);
