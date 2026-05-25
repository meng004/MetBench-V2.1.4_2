using System;
using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class FieldProportionalityKernel : IVerifierKernel<FieldProportionalityPredicate>
{
    public VerificationResult Evaluate(FieldProportionalityPredicate predicate, VerificationContext context)
    {
        var left = context.GetField(predicate.LeftRole, predicate.LeftMetric);
        var right = context.GetField(predicate.RightRole, predicate.RightMetric);

        if (left.RowCount != right.RowCount || left.ColumnCount != right.ColumnCount)
        {
            return VerificationResult.InvalidSpec("Field dimensions must match before proportionality evaluation.");
        }

        var ratio = EstimateRatio(left, right, predicate.Estimator);
        var residual = ComputeResidual(left, right, ratio);
        var tolerance = ComputeTolerance(right, predicate.Tolerance);
        var passed = residual <= tolerance;

        var assertion = new SystemMtAssertionResultV2(
            "FieldProportionality",
            passed,
            ratio,
            ratio,
            residual,
            tolerance,
            $"{predicate.RightRole}.{predicate.RightMetric} ~= k * {predicate.LeftRole}.{predicate.LeftMetric}",
            passed ? null : $"FieldProportionality failed: estimated ratio={ratio}, residual={residual}, tolerance={tolerance}");

        return VerificationResult.FromAssertion(assertion, new VerificationDiagnostic(ratio, ratio, residual, tolerance));
    }

    private static double EstimateRatio(Field2DValue left, Field2DValue right, ConstantEstimator estimator)
    {
        return estimator switch
        {
            ConstantEstimator.MedianRatio => MedianRatio(left, right),
            _ => LeastSquaresThroughOrigin(left, right)
        };
    }

    private static double LeastSquaresThroughOrigin(Field2DValue left, Field2DValue right)
    {
        var numerator = 0.0;
        var denominator = 0.0;

        for (var row = 0; row < left.RowCount; row++)
        {
            for (var column = 0; column < left.ColumnCount; column++)
            {
                numerator += left.Values[row, column] * right.Values[row, column];
                denominator += left.Values[row, column] * left.Values[row, column];
            }
        }

        return denominator == 0.0 ? 0.0 : numerator / denominator;
    }

    private static double MedianRatio(Field2DValue left, Field2DValue right)
    {
        var ratios = new List<double>();
        for (var row = 0; row < left.RowCount; row++)
        {
            for (var column = 0; column < left.ColumnCount; column++)
            {
                var source = left.Values[row, column];
                if (Math.Abs(source) > 1e-12)
                {
                    ratios.Add(right.Values[row, column] / source);
                }
            }
        }

        if (ratios.Count == 0)
        {
            return 0.0;
        }

        ratios.Sort();
        return ratios[ratios.Count / 2];
    }

    private static double ComputeResidual(Field2DValue left, Field2DValue right, double ratio)
    {
        var sumSquares = 0.0;
        for (var row = 0; row < left.RowCount; row++)
        {
            for (var column = 0; column < left.ColumnCount; column++)
            {
                var expected = ratio * left.Values[row, column];
                var delta = right.Values[row, column] - expected;
                sumSquares += delta * delta;
            }
        }

        return Math.Sqrt(sumSquares);
    }

    private static double ComputeTolerance(Field2DValue right, FieldNormToleranceSpec tolerance)
    {
        if (string.Equals(tolerance.Norm, "LInf", StringComparison.OrdinalIgnoreCase))
        {
            var max = 0.0;
            for (var row = 0; row < right.RowCount; row++)
            {
                for (var column = 0; column < right.ColumnCount; column++)
                {
                    max = Math.Max(max, Math.Abs(right.Values[row, column]));
                }
            }

            return tolerance.Atol + (tolerance.Rtol * max);
        }

        var sumSquares = 0.0;
        for (var row = 0; row < right.RowCount; row++)
        {
            for (var column = 0; column < right.ColumnCount; column++)
            {
                var value = Math.Abs(right.Values[row, column]);
                sumSquares += value * value;
            }
        }

        return tolerance.Atol + (tolerance.Rtol * Math.Sqrt(sumSquares));
    }
}
