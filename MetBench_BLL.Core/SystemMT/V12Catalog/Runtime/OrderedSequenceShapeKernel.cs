using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class OrderedSequenceShapeKernel : IVerifierKernel<OrderedSequenceShapePredicate>
{
    private readonly SequenceShapeKernel _sequenceShape = new();

    public VerificationResult Evaluate(OrderedSequenceShapePredicate predicate, VerificationContext context)
    {
        var samples = new List<double>(predicate.OrderedRoles.Count);
        foreach (var role in predicate.OrderedRoles)
        {
            if (!context.TryGetMetric(role, predicate.Metric, out var sample))
            {
                return VerificationResult.SkippedMissingObservable($"Missing metric '{predicate.Metric}' for role '{role}'.");
            }

            samples.Add(sample);
        }

        return _sequenceShape.Evaluate(predicate.Shape, new SequenceValue(samples));
    }
}
