using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Migration;

/// <summary>
/// Migration-input helper for the verification-semantics convergence (PR-C):
/// translate legacy <see cref="MetBench_BLL.SystemMT.Assertions.AssertionTypeCodes"/>
/// string codes into Typed Semantic Catalog predicate records so the runtime
/// can drive the predicate dispatcher uniformly.
/// </summary>
/// <remarks>
/// This is the only typed entry point production code may reach from a
/// legacy string code. Scope is the deliberately narrow PR-C subset:
/// <c>less</c>, <c>greater</c>, <c>approx</c>, and the <c>flw = k * src</c>
/// scaling relation. All other legacy codes are rejected fail-closed.
/// Broader migration (noise-aware / variance-ratio / flux / cross-method)
/// must add its own typed predicate before being mapped here.
/// </remarks>
public static class LegacyAssertionPredicateMapper
{
    public static PredicateSpec MapScalar(
        string assertionTypeCode,
        string actualRole,
        string expectedRole,
        string metric)
    {
        if (string.IsNullOrWhiteSpace(actualRole))
            throw new ArgumentException("actualRole is required.", nameof(actualRole));
        if (string.IsNullOrWhiteSpace(expectedRole))
            throw new ArgumentException("expectedRole is required.", nameof(expectedRole));
        if (string.IsNullOrWhiteSpace(metric))
            throw new ArgumentException("metric is required.", nameof(metric));

        switch (assertionTypeCode)
        {
            case "less-noise-aware":
            case "greater-noise-aware":
                throw new ArgumentException(
                    $"Legacy assertion code '{assertionTypeCode}' has noise-aware semantics " +
                    $"(SourceStd / FollowupStd / NoiseMultiplier) that are not yet representable " +
                    $"in the Typed Semantic Catalog scalar predicates. Add a noise-aware typed " +
                    $"predicate before mapping this code, or use a typed alternative for the " +
                    $"specific MR binding.",
                    nameof(assertionTypeCode));
        }

        var op = assertionTypeCode switch
        {
            "less" => "Less",
            "greater" => "Greater",
            "approx" => "Equal",
            // approx-invariant scalar form: |followup - source| <= tolerance. Deterministic
            // tolerance equality matches the legacy semantics when no noise floor is involved.
            // Field-shape variants of approx-invariant should use a DerivedInvariantPredicate
            // through a dedicated mapper call instead.
            "approx-invariant" => "Equal",
            _ => throw new ArgumentException(
                $"Unsupported legacy assertion code '{assertionTypeCode}'. " +
                "Use Typed Semantic Catalog predicates or extend the mapper in a dedicated PR.",
                nameof(assertionTypeCode)),
        };

        return new BinaryComparisonPredicate(
            PredicateId: $"{metric}-{op.ToLowerInvariant()}",
            LeftRole: actualRole,
            RightRole: expectedRole,
            Metric: metric,
            Operator: op);
    }

    public static PredicateSpec MapScaling(
        string actualRole,
        string expectedRole,
        string metric,
        ParameterExpression factor,
        double exponent)
    {
        if (string.IsNullOrWhiteSpace(actualRole))
            throw new ArgumentException("actualRole is required.", nameof(actualRole));
        if (string.IsNullOrWhiteSpace(expectedRole))
            throw new ArgumentException("expectedRole is required.", nameof(expectedRole));
        if (string.IsNullOrWhiteSpace(metric))
            throw new ArgumentException("metric is required.", nameof(metric));
        if (factor is null)
            throw new ArgumentNullException(nameof(factor));

        return new ScaledEqualityPredicate(
            PredicateId: $"{metric}-scaled-equality",
            ActualRole: actualRole,
            ReferenceRole: expectedRole,
            Metric: metric,
            Factor: factor,
            Exponent: exponent);
    }

    /// <summary>
    /// Legacy <c>variance-ratio</c> → <see cref="VarianceRatioPredicate"/>.
    /// Caller supplies the two sample roles, the statistical metric, the sample-size
    /// ratio between them (often N_refined / N_coarse), and the sigma multiplier
    /// the legacy assertion compared against.
    /// </summary>
    public static PredicateSpec MapVarianceRatio(
        string lowSampleRole,
        string highSampleRole,
        string statisticalMetric,
        ParameterExpression sampleRatio,
        double sigmaMultiplier)
    {
        if (string.IsNullOrWhiteSpace(lowSampleRole))
            throw new ArgumentException("lowSampleRole is required.", nameof(lowSampleRole));
        if (string.IsNullOrWhiteSpace(highSampleRole))
            throw new ArgumentException("highSampleRole is required.", nameof(highSampleRole));
        if (string.IsNullOrWhiteSpace(statisticalMetric))
            throw new ArgumentException("statisticalMetric is required.", nameof(statisticalMetric));
        if (sampleRatio is null)
            throw new ArgumentNullException(nameof(sampleRatio));

        return new VarianceRatioPredicate(
            PredicateId: $"{statisticalMetric}-variance-ratio",
            LowSampleRole: lowSampleRole,
            HighSampleRole: highSampleRole,
            StatisticalMetric: statisticalMetric,
            SampleRatio: sampleRatio,
            Tolerance: new StatisticalToleranceSpec(sigmaMultiplier));
    }

    /// <summary>
    /// Legacy <c>flux-pointwise-approx</c> → <see cref="FieldEqualityPredicate"/>
    /// with identity field pairing. Field metrics on both sides default to the
    /// same name (the typical legacy case); pass distinct <paramref name="leftMetric"/>
    /// / <paramref name="rightMetric"/> only when the catalog binding explicitly
    /// pairs different field columns.
    /// </summary>
    public static PredicateSpec MapFluxPointwise(
        string leftRole,
        string rightRole,
        string leftMetric,
        string rightMetric,
        string norm,
        double atol,
        double rtol)
    {
        if (string.IsNullOrWhiteSpace(leftRole))
            throw new ArgumentException("leftRole is required.", nameof(leftRole));
        if (string.IsNullOrWhiteSpace(rightRole))
            throw new ArgumentException("rightRole is required.", nameof(rightRole));
        if (string.IsNullOrWhiteSpace(leftMetric))
            throw new ArgumentException("leftMetric is required.", nameof(leftMetric));
        if (string.IsNullOrWhiteSpace(rightMetric))
            throw new ArgumentException("rightMetric is required.", nameof(rightMetric));
        if (string.IsNullOrWhiteSpace(norm))
            throw new ArgumentException("norm is required.", nameof(norm));

        return new FieldEqualityPredicate(
            PredicateId: $"{leftMetric}-field-equality",
            LeftRole: leftRole,
            RightRole: rightRole,
            LeftMetric: leftMetric,
            RightMetric: rightMetric,
            Pairing: new IdentityFieldPairing(),
            Tolerance: new FieldNormToleranceSpec(norm, atol, rtol));
    }

    /// <summary>
    /// Legacy <c>cross-program-agree</c> → <see cref="CrossMethodComparisonPredicate"/>
    /// with <c>Operator="Equal"</c> and deterministic tolerance. Use this when comparing
    /// the same scalar metric across two SUT implementations (e.g. OpenMOC vs OpenMC).
    /// </summary>
    public static PredicateSpec MapCrossProgramAgree(
        string leftRole,
        string rightRole,
        string metric,
        double atol,
        double rtol)
    {
        if (string.IsNullOrWhiteSpace(leftRole))
            throw new ArgumentException("leftRole is required.", nameof(leftRole));
        if (string.IsNullOrWhiteSpace(rightRole))
            throw new ArgumentException("rightRole is required.", nameof(rightRole));
        if (string.IsNullOrWhiteSpace(metric))
            throw new ArgumentException("metric is required.", nameof(metric));

        return new CrossMethodComparisonPredicate(
            PredicateId: $"{metric}-cross-method-equal",
            LeftRole: leftRole,
            RightRole: rightRole,
            Metric: metric,
            Operator: "Equal",
            Tolerance: new DeterministicToleranceSpec(atol, rtol));
    }
}
