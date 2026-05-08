namespace MetBench_BLL.SystemMT;

public sealed record InputGenerationResult(
    string SourceInputPath,
    string FollowUpInputPath,
    MrTransformation Transformation,
    bool Succeeded,
    string Log,
    string FailureReason);
