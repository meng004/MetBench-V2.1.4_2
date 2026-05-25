using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class SubadditiveKernel
{
    public VerificationResult Evaluate(SubadditivePredicate predicate, object? _)
    {
        var expectedBound = predicate.DeltaA + predicate.DeltaB;
        var passed = predicate.DeltaAB <= expectedBound;
        var residual = predicate.DeltaAB - expectedBound;

        var assertion = new SystemMtAssertionResultV2(
            "Subadditive",
            passed,
            expectedBound,
            predicate.DeltaAB,
            residual,
            0.0,
            "delta_ab <= delta_a + delta_b",
            passed ? null : $"Subadditive failed: actual={predicate.DeltaAB}, bound={expectedBound}");

        return VerificationResult.FromAssertion(
            assertion,
            new VerificationDiagnostic(expectedBound, predicate.DeltaAB, residual, 0.0));
    }
}
