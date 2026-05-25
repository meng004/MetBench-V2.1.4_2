using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class FieldEqualityKernel : IVerifierKernel<FieldEqualityPredicate>
{
    public VerificationResult Evaluate(FieldEqualityPredicate predicate, VerificationContext context)
    {
        var left = context.GetField(predicate.LeftRole, predicate.LeftMetric);
        var right = context.GetField(predicate.RightRole, predicate.RightMetric);

        if (left.RowCount != right.RowCount || left.ColumnCount != right.ColumnCount)
        {
            return VerificationResult.InvalidSpec("Field dimensions must match before runtime evaluation.");
        }

        var (residual, worstRow, worstColumn, worstResidual) = ComputeResidual(left, right, predicate.Tolerance.Norm);
        var tolerance = ComputeTolerance(right, predicate.Tolerance);
        var passed = residual <= tolerance;
        var failureReason = passed
            ? null
            : $"FieldEquality failed at ({worstRow},{worstColumn}): residual={worstResidual}, norm_residual={residual}, tolerance={tolerance}";

        var assertion = new SystemMtAssertionResultV2(
            "FieldEquality",
            passed,
            0.0,
            0.0,
            residual,
            tolerance,
            $"{predicate.LeftRole}.{predicate.LeftMetric} ~= {predicate.RightRole}.{predicate.RightMetric}",
            failureReason);

        return VerificationResult.FromAssertion(assertion, new VerificationDiagnostic(0.0, 0.0, residual, tolerance));
    }

    private static (double Residual, int WorstRow, int WorstColumn, double WorstResidual) ComputeResidual(
        Field2DValue left,
        Field2DValue right,
        string norm)
    {
        var sumSquares = 0.0;
        var worstResidual = 0.0;
        var worstRow = 0;
        var worstColumn = 0;

        for (var row = 0; row < left.RowCount; row++)
        {
            for (var column = 0; column < left.ColumnCount; column++)
            {
                var delta = Math.Abs(left.Values[row, column] - right.Values[row, column]);
                sumSquares += delta * delta;
                if (delta > worstResidual)
                {
                    worstResidual = delta;
                    worstRow = row;
                    worstColumn = column;
                }
            }
        }

        var residual = string.Equals(norm, "LInf", StringComparison.OrdinalIgnoreCase)
            ? worstResidual
            : Math.Sqrt(sumSquares);

        return (residual, worstRow, worstColumn, worstResidual);
    }

    private static double ComputeTolerance(Field2DValue right, FieldNormToleranceSpec tolerance)
    {
        var referenceNorm = 0.0;

        if (string.Equals(tolerance.Norm, "LInf", StringComparison.OrdinalIgnoreCase))
        {
            for (var row = 0; row < right.RowCount; row++)
            {
                for (var column = 0; column < right.ColumnCount; column++)
                {
                    referenceNorm = Math.Max(referenceNorm, Math.Abs(right.Values[row, column]));
                }
            }

            return tolerance.Atol + (tolerance.Rtol * referenceNorm);
        }

        for (var row = 0; row < right.RowCount; row++)
        {
            for (var column = 0; column < right.ColumnCount; column++)
            {
                var value = Math.Abs(right.Values[row, column]);
                referenceNorm += value * value;
            }
        }

        return tolerance.Atol + (tolerance.Rtol * Math.Sqrt(referenceNorm));
    }
}
