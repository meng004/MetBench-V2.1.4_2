using System;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

/// <summary>
/// Computes <c>NoiseMultiplier · √(sourceStd² + followupStd²)</c> — the combined-σ
/// tolerance used by <see cref="NoiseAwareBinaryComparisonKernel"/> to widen a directional
/// inequality so it does not flap on sampling noise. Pure / stateless; the three input
/// arguments are validated for finiteness and basic ranges so kernel-side use is total.
/// </summary>
public sealed class NoiseAwareScalarToleranceEvaluator
{
    public double ComputeTolerance(double sourceStd, double followupStd, double noiseMultiplier)
    {
        if (!double.IsFinite(sourceStd) || sourceStd < 0)
            throw new ArgumentException($"sourceStd must be finite and non-negative; got {sourceStd}.", nameof(sourceStd));
        if (!double.IsFinite(followupStd) || followupStd < 0)
            throw new ArgumentException($"followupStd must be finite and non-negative; got {followupStd}.", nameof(followupStd));
        if (!double.IsFinite(noiseMultiplier) || noiseMultiplier <= 0)
            throw new ArgumentException($"noiseMultiplier must be finite and > 0; got {noiseMultiplier}.", nameof(noiseMultiplier));

        return noiseMultiplier * Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
    }
}
