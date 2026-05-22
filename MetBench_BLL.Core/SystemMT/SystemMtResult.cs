namespace MetBench_BLL.SystemMT;

public sealed record SystemMtResult(
    CliRunResult SourceRun,
    CliRunResult FollowUpRun,
    ParsedOutput SourceOutput,
    ParsedOutput FollowUpOutput,
    SystemMtAssertionResult Assertion,
    bool Passed,
    string FailureReason,
    InputGenerationResult? InputGeneration = null,
    IReadOnlyList<InputSamplePoint>? InputSamples = null);
