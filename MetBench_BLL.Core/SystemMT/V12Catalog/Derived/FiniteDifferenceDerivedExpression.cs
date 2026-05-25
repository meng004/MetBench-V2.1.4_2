using System;

namespace MetBench_BLL.SystemMT.V12Catalog.Derived;

public static class FiniteDifferenceDerivedExpression
{
    public static double[] Compute(double[] values)
    {
        if (values.Length < 2)
        {
            return Array.Empty<double>();
        }

        var result = new double[values.Length - 1];
        for (var i = 1; i < values.Length; i++)
        {
            result[i - 1] = values[i] - values[i - 1];
        }

        return result;
    }
}
