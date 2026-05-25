using MetBench_BLL.SystemMT.Assertions;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed record VerificationResult(
    VerifyStatus Status,
    SystemMtAssertionResultV2? Assertion,
    VerificationDiagnostic? Diagnostic,
    DiagnosticContext? Context)
{
    public static VerificationResult FromAssertion(
        SystemMtAssertionResultV2 assertion,
        VerificationDiagnostic diagnostic) =>
        new(
            assertion.Passed ? VerifyStatus.Passed : VerifyStatus.Failed,
            assertion,
            diagnostic,
            null);

    public static VerificationResult SkippedMissingObservable(string reason) =>
        new(VerifyStatus.SkippedMissingObservable, null, null, new DiagnosticContext(reason));

    public static VerificationResult SkippedNotApplicable(string reason) =>
        new(VerifyStatus.SkippedNotApplicable, null, null, new DiagnosticContext(reason));

    public static VerificationResult InvalidSpec(string reason) =>
        new(VerifyStatus.InvalidSpec, null, null, new DiagnosticContext(reason));
}
