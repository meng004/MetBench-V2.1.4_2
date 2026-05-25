using System;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Derived;

public static class CoefficientOfVariation
{
    public static double Compute(double[] values)
    {
        if (values.Length == 0)
        {
            return 0.0;
        }

        var mean = 0.0;
        foreach (var value in values)
        {
            mean += value;
        }

        mean /= values.Length;
        if (Math.Abs(mean) < 1e-12)
        {
            return 0.0;
        }

        var variance = 0.0;
        foreach (var value in values)
        {
            var delta = value - mean;
            variance += delta * delta;
        }

        variance /= values.Length;
        return Math.Sqrt(variance) / Math.Abs(mean);
    }
}
