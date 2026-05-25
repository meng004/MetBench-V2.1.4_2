using MetBench_BLL.SystemMT.Assertions;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed record VerificationResult(
    SystemMtAssertionResultV2 Assertion,
    VerificationDiagnostic Diagnostic);
