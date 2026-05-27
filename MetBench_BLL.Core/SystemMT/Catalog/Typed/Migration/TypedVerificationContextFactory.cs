using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Migration;

/// <summary>
/// Builds a typed <see cref="VerificationContext"/> from pipeline-captured scalar
/// role outputs so the System MT pipeline assertion stage can dispatch on
/// typed predicates without leaking pipeline types into the typed catalog.
/// </summary>
/// <remarks>
/// Validates the synthesized <see cref="MrSpec"/> up-front and throws when
/// validation fails — the runtime contract is fail-closed.
/// </remarks>
public static class TypedVerificationContextFactory
{
    public static VerificationContext FromScalarOutputs(
        MrSpec spec,
        IReadOnlyDictionary<string, double> sourceScalars,
        IReadOnlyDictionary<string, double> followupScalars,
        IReadOnlyDictionary<string, string> parameterValues)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (sourceScalars is null) throw new ArgumentNullException(nameof(sourceScalars));
        if (followupScalars is null) throw new ArgumentNullException(nameof(followupScalars));
        if (parameterValues is null) throw new ArgumentNullException(nameof(parameterValues));

        var validation = spec.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Typed semantic spec must validate before runtime execution: " +
                string.Join("; ", validation.Errors.Select(e => $"{e.Path}: {e.Message}")));
        }

        var (actualRole, expectedRole) = RolesFromSpec(spec);

        var (expectedStatistics, actualStatistics) = BuildStatisticsForSpec(
            spec, sourceScalars, followupScalars);

        var roles = new Dictionary<string, RoleOutput>(StringComparer.Ordinal)
        {
            [expectedRole] = new RoleOutput(
                expectedRole,
                new Dictionary<string, double>(sourceScalars),
                Fields: null,
                Statistics: expectedStatistics),
            [actualRole] = new RoleOutput(
                actualRole,
                new Dictionary<string, double>(followupScalars),
                Fields: null,
                Statistics: actualStatistics),
        };

        var inputs = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (key, value) in parameterValues)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                inputs[key] = parsed;
            }
        }

        return new VerificationContext(spec, roles, inputs);
    }

    private static (string actualRole, string expectedRole) RolesFromSpec(MrSpec spec)
    {
        if (spec.Predicates is { Count: > 0 })
        {
            switch (spec.Predicates[0])
            {
                case BinaryComparisonPredicate binary:
                    return (binary.LeftRole, binary.RightRole);
                case ScaledEqualityPredicate scaled:
                    return (scaled.ActualRole, scaled.ReferenceRole);
                case VarianceRatioPredicate varianceRatio:
                    // sourceScalars → expected → LowSampleRole (larger σ before refinement),
                    // followupScalars → actual → HighSampleRole (smaller σ after refinement).
                    return (varianceRatio.HighSampleRole, varianceRatio.LowSampleRole);
            }
        }

        return ("followup", "source");
    }

    /// <summary>
    /// When the spec carries a <see cref="VarianceRatioPredicate"/>, promote the
    /// statistical metric value from each side's scalar map into a per-role
    /// <see cref="StatisticalValue"/> so <see cref="VarianceRatioKernel"/> can read
    /// it via <c>context.GetStatistical(role, metric)</c>. The convention is that
    /// <c>scalars[metric]</c> carries the stderr (e.g. <c>k_eff_std</c>); the Mean
    /// component is set to <c>0.0</c> because the kernel does not consume it.
    /// Returns <c>(null, null)</c> for non-variance-ratio specs — additive.
    /// </summary>
    private static (
        IReadOnlyDictionary<string, StatisticalValue>? expectedStatistics,
        IReadOnlyDictionary<string, StatisticalValue>? actualStatistics)
        BuildStatisticsForSpec(
            MrSpec spec,
            IReadOnlyDictionary<string, double> sourceScalars,
            IReadOnlyDictionary<string, double> followupScalars)
    {
        if (spec.Predicates is not { Count: > 0 } predicates)
        {
            return (null, null);
        }

        if (predicates[0] is not VarianceRatioPredicate variance)
        {
            return (null, null);
        }

        var expected = TryBuildStatistics(sourceScalars, variance.StatisticalMetric);
        var actual = TryBuildStatistics(followupScalars, variance.StatisticalMetric);
        return (expected, actual);
    }

    private static IReadOnlyDictionary<string, StatisticalValue>? TryBuildStatistics(
        IReadOnlyDictionary<string, double> scalars,
        string metric)
    {
        if (!scalars.TryGetValue(metric, out var stderr))
        {
            return null;
        }

        return new Dictionary<string, StatisticalValue>(StringComparer.Ordinal)
        {
            [metric] = new StatisticalValue(Mean: 0.0, StdError: stderr),
        };
    }

    /// <summary>
    /// PR-Bol-2A: build a <see cref="VerificationContext"/> from a per-phase scalar map.
    /// One <see cref="RoleOutput"/> per <paramref name="phaseScalars"/> entry, keyed by the
    /// phase role name. No statistics promotion (the <c>ErrorMonotonicKernel</c> reads via
    /// <c>GetMetric</c>, not <c>GetStatistical</c>). Validates the spec up-front and throws
    /// <see cref="InvalidOperationException"/> on validation failure — same fail-closed
    /// contract as <see cref="FromScalarOutputs"/>. Independent of <see cref="FromScalarOutputs"/>
    /// to keep PR-VR's 2-side statistics-promotion path byte-identical.
    /// </summary>
    public static VerificationContext FromPhaseOutputs(
        MrSpec spec,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> phaseScalars,
        IReadOnlyDictionary<string, string> parameterValues)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (phaseScalars is null) throw new ArgumentNullException(nameof(phaseScalars));
        if (parameterValues is null) throw new ArgumentNullException(nameof(parameterValues));

        var validation = spec.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Typed semantic spec must validate before runtime execution: " +
                string.Join("; ", validation.Errors.Select(e => $"{e.Path}: {e.Message}")));
        }

        var roles = new Dictionary<string, RoleOutput>(StringComparer.Ordinal);
        foreach (var (role, scalars) in phaseScalars)
        {
            if (scalars is null)
                throw new ArgumentException(
                    $"phaseScalars[{role}] must not be null.", nameof(phaseScalars));
            roles[role] = new RoleOutput(
                role,
                new Dictionary<string, double>(scalars),
                Fields: null,
                Statistics: null);
        }

        var inputs = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (key, value) in parameterValues)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                inputs[key] = parsed;
            }
        }

        return new VerificationContext(spec, roles, inputs);
    }
}
