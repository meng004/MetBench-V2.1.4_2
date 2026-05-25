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

        var op = assertionTypeCode switch
        {
            "less" => "Less",
            "greater" => "Greater",
            "approx" => "Equal",
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
}
