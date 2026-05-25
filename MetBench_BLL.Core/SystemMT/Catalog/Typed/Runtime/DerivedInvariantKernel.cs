using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Derived;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class DerivedInvariantKernel : IVerifierKernel<DerivedInvariantPredicate>
{
    private readonly DeterministicScalarToleranceEvaluator _tolerance = new();

    public VerificationResult Evaluate(DerivedInvariantPredicate predicate, VerificationContext context)
    {
        var leftField = context.GetField(predicate.LeftRole, predicate.LeftMetric);
        var rightField = context.GetField(predicate.RightRole, predicate.RightMetric);
        var left = ComputeDerivedValue(predicate.DerivedMetric, leftField);
        var right = ComputeDerivedValue(predicate.DerivedMetric, rightField);
        var residual = Math.Abs(left - right);
        var tolerance = _tolerance.ComputeTolerance(right, predicate.Tolerance);
        var passed = residual <= tolerance;

        var assertion = new SystemMtAssertionResultV2(
            "DerivedInvariant",
            passed,
            right,
            left,
            residual,
            tolerance,
            $"{predicate.DerivedMetric}({predicate.LeftRole}.{predicate.LeftMetric}) ~= {predicate.DerivedMetric}({predicate.RightRole}.{predicate.RightMetric})",
            passed ? null : $"DerivedInvariant failed for {predicate.DerivedMetric}: left={left}, right={right}, residual={residual}, tolerance={tolerance}");

        return VerificationResult.FromAssertion(assertion, new VerificationDiagnostic(right, left, residual, tolerance));
    }

    private static double ComputeDerivedValue(string derivedMetric, Field2DValue field) =>
        derivedMetric switch
        {
            "mass_number_sum" => MassNumberSum.Compute(field.Values),
            "l2_norm" => L2Norm.Compute(field),
            "linf_norm" => LinfNorm.Compute(field),
            "field_region_mean" => FieldRegionMean.Compute(field),
            _ => throw new ArgumentException($"Unsupported derived metric '{derivedMetric}'.", nameof(derivedMetric))
        };
}
