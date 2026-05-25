using System;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class PredicateDispatcher : IPredicateDispatcher
{
    private readonly BinaryComparisonKernel _binary = new();
    private readonly ScaledEqualityKernel _scaled = new();

    public VerificationResult Dispatch(PredicateSpec predicate, VerificationContext context) =>
        predicate switch
        {
            BinaryComparisonPredicate binary => _binary.Evaluate(binary, context),
            ScaledEqualityPredicate scaled => _scaled.Evaluate(scaled, context),
            _ => throw new ArgumentException($"Unsupported predicate type '{predicate.GetType().Name}'.", nameof(predicate))
        };
}
