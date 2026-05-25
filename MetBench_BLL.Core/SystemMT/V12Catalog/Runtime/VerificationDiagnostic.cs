namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed record VerificationDiagnostic(
    double Expected,
    double Actual,
    double Residual,
    double Tolerance);
