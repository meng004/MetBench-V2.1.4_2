using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class ErrorMonotonicKernel : IVerifierKernel<ErrorMonotonicPredicate>
{
    public VerificationResult Evaluate(ErrorMonotonicPredicate predicate, VerificationContext context)
    {
        var reference = context.GetMetric(predicate.ReferenceRole, predicate.Metric);
        var previousRole = predicate.OrderedRoles[0];
        var previousError = ComputeError(context.GetMetric(previousRole, predicate.Metric), reference, predicate.NormKind);

        for (var i = 1; i < predicate.OrderedRoles.Count; i++)
        {
            var currentRole = predicate.OrderedRoles[i];
            var currentError = ComputeError(context.GetMetric(currentRole, predicate.Metric), reference, predicate.NormKind);

            if (currentError > previousError)
            {
                var assertion = new SystemMtAssertionResultV2(
                    "ErrorMonotonic",
                    false,
                    previousError,
                    currentError,
                    currentError - previousError,
                    0.0,
                    $"{currentRole}.{predicate.Metric} error <= {previousRole}.{predicate.Metric} error",
                    $"ErrorMonotonic failed at {previousRole}->{currentRole}: previous_error={previousError}, current_error={currentError}");

                return VerificationResult.FromAssertion(
                    assertion,
                    new VerificationDiagnostic(previousError, currentError, currentError - previousError, 0.0));
            }

            previousRole = currentRole;
            previousError = currentError;
        }

        var success = new SystemMtAssertionResultV2(
            "ErrorMonotonic",
            true,
            previousError,
            previousError,
            0.0,
            0.0,
            $"{predicate.Metric} error sequence is monotonic toward {predicate.ReferenceRole}",
            null);

        return VerificationResult.FromAssertion(
            success,
            new VerificationDiagnostic(previousError, previousError, 0.0, 0.0));
    }

    private static double ComputeError(double value, double reference, NormKind normKind)
    {
        var absolute = Math.Abs(value - reference);

        return normKind switch
        {
            NormKind.Absolute => absolute,
            NormKind.L2 => absolute,
            NormKind.Linf => absolute,
            NormKind.Relative => Math.Abs(reference) < 1e-12 ? absolute : absolute / Math.Abs(reference),
            _ => throw new ArgumentOutOfRangeException(nameof(normKind), normKind, "Unsupported norm kind.")
        };
    }
}
