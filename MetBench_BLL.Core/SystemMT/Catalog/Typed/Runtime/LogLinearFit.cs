using System;
using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public static class LogLinearFit
{
    public static LogLinearFitResult Compute(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        if (x.Count != y.Count)
        {
            throw new ArgumentException("x and y must have the same number of samples.");
        }

        if (x.Count < 2)
        {
            throw new ArgumentException("At least two samples are required for log-linear fitting.");
        }

        var z = new double[y.Count];
        for (var i = 0; i < y.Count; i++)
        {
            z[i] = Math.Log(y[i]);
        }

        var meanX = Mean(x);
        var meanZ = Mean(z);

        var sxx = 0.0;
        var sxz = 0.0;
        for (var i = 0; i < x.Count; i++)
        {
            var dx = x[i] - meanX;
            var dz = z[i] - meanZ;
            sxx += dx * dx;
            sxz += dx * dz;
        }

        var slope = sxx == 0.0 ? 0.0 : sxz / sxx;
        var intercept = meanZ - (slope * meanX);

        var ssTot = 0.0;
        var ssRes = 0.0;
        var worstIndex = 0;
        var worstResidual = double.MinValue;

        for (var i = 0; i < x.Count; i++)
        {
            var fitted = intercept + (slope * x[i]);
            var residual = z[i] - fitted;
            var absResidual = Math.Abs(residual);
            if (absResidual > worstResidual)
            {
                worstResidual = absResidual;
                worstIndex = i;
            }

            var centered = z[i] - meanZ;
            ssTot += centered * centered;
            ssRes += residual * residual;
        }

        var rSquared = ssTot == 0.0 ? 1.0 : 1.0 - (ssRes / ssTot);
        return new LogLinearFitResult(intercept, slope, rSquared, worstIndex, worstResidual, ssRes, ssTot);
    }

    private static double Mean(IReadOnlyList<double> values)
    {
        var sum = 0.0;
        for (var i = 0; i < values.Count; i++)
        {
            sum += values[i];
        }

        return sum / values.Count;
    }
}

public sealed record LogLinearFitResult(
    double Intercept,
    double Slope,
    double RSquared,
    int WorstPointIndex,
    double WorstLogResidual,
    double SumSquaredResidual,
    double SumSquaredTotal);
