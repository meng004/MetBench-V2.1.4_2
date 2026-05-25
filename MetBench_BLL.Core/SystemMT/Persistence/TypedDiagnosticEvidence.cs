namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Persistence-shaped projection of
/// <c>MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerificationDiagnostic</c>.
/// Stored on <see cref="TypedVerificationEvidence.Diagnostic"/> when the typed
/// verifier produced a numeric diagnostic (Passed / Failed states); null for
/// Skipped*/InvalidSpec states.
/// </summary>
public sealed class TypedDiagnosticEvidence
{
    public double Expected { get; set; }

    public double Actual { get; set; }

    public double Residual { get; set; }

    public double Tolerance { get; set; }
}
