using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class DeterministicScalarToleranceEvaluator
{
    public double ComputeTolerance(double expected, DeterministicToleranceSpec tolerance) =>
        Math.Max(tolerance.Atol, tolerance.Rtol * Math.Abs(expected));

    public bool Within(double actual, double expected, DeterministicToleranceSpec tolerance) =>
        Math.Abs(actual - expected) <= ComputeTolerance(expected, tolerance);
}
