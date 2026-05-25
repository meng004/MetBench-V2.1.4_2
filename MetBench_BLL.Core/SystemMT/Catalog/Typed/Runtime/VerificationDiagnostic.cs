namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed record VerificationDiagnostic(
    double Expected,
    double Actual,
    double Residual,
    double Tolerance);
