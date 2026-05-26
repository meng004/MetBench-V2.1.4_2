using System.Collections.Generic;
using System.Globalization;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Migration;

/// <summary>
/// Synthesizes a minimal <see cref="MrSpec"/> for the PR-C runtime convergence
/// path so the typed predicate dispatcher can run against pipeline outputs
/// that originated from a legacy string-code execution context. Used only as
/// migration input — production catalog entries should provide their own
/// validated <see cref="MrSpec"/>.
/// </summary>
public static class TypedSpecFactory
{
    public static MrSpec ForScalarBinaryComparison(
        string mrCode,
        string valueName,
        string actualRole,
        string expectedRole,
        string @operator,
        double toleranceAbs,
        double toleranceRel)
    {
        var predicate = new BinaryComparisonPredicate(
            PredicateId: $"{valueName}-{@operator.ToLowerInvariant()}",
            LeftRole: actualRole,
            RightRole: expectedRole,
            Metric: valueName,
            Operator: @operator);

        return Build(mrCode, valueName, predicate, parameters: null,
            toleranceAbs: toleranceAbs, toleranceRel: toleranceRel,
            actualRole: actualRole, expectedRole: expectedRole);
    }

    public static MrSpec ForLegacyScalar(
        string mrCode,
        string valueName,
        PredicateSpec predicate,
        double toleranceAbs,
        double toleranceRel)
    {
        var (actualRole, expectedRole) = RolesFor(predicate);
        return Build(mrCode, valueName, predicate, parameters: null,
            toleranceAbs: toleranceAbs, toleranceRel: toleranceRel,
            actualRole: actualRole, expectedRole: expectedRole);
    }

    /// <summary>
    /// PR-VR: single migration entry point that turns a legacy pipeline-style
    /// (assertion-type-code, value-name, parameters, tolerance) tuple into a
    /// typed <see cref="MrSpec"/>. String dispatch on
    /// <see cref="AssertionTypeCodes"/> is intentionally confined to this
    /// helper (per the SemanticCatalogBoundaryTests architecture guard) so the
    /// pipeline can stay typed-only.
    ///
    /// Dispatch:
    /// <list type="bullet">
    ///   <item><c>"variance-ratio"</c> → <see cref="ForVarianceRatio"/> using
    ///   <c>parameters["factor"]</c> as the SampleRatio (must be finite, &gt; 1)
    ///   and <c>toleranceRel</c> as the rel slack
    ///   (<c>SigmaMultiplier = 1 + toleranceRel</c>).</item>
    ///   <item>all other codes → <see cref="LegacyAssertionPredicateMapper.MapScalar"/>
    ///   + <see cref="ForLegacyScalar"/> (the existing scalar binary-comparison
    ///   path).</item>
    /// </list>
    /// Throws <see cref="System.ArgumentException"/> on bad inputs (blank
    /// value-name, missing / non-numeric / ≤ 1 factor, non-positive tolerance,
    /// unknown legacy code from <see cref="LegacyAssertionPredicateMapper"/>);
    /// the pipeline catches and surfaces as <c>UnknownType</c>.
    /// </summary>
    public static MrSpec ForLegacyAssertion(
        string mrCode,
        string assertionTypeCode,
        string valueName,
        IReadOnlyDictionary<string, string> parameters,
        double toleranceAbs,
        double toleranceRel)
    {
        if (parameters is null) throw new System.ArgumentNullException(nameof(parameters));

        if (string.Equals(assertionTypeCode, AssertionTypeCodes.VarianceRatio, System.StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(valueName))
            {
                throw new System.ArgumentException(
                    "variance-ratio assertion requires a non-blank ValueName (the statistical metric).",
                    nameof(valueName));
            }

            if (!parameters.TryGetValue("factor", out var factorRaw))
            {
                throw new System.ArgumentException(
                    "variance-ratio assertion requires parameter 'factor' (per-run sample-count ratio).",
                    nameof(parameters));
            }

            if (!double.TryParse(factorRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            {
                throw new System.ArgumentException(
                    $"variance-ratio assertion parameter 'factor' must parse as a double; got '{factorRaw}'.",
                    nameof(parameters));
            }

            return ForVarianceRatio(
                mrCode: mrCode,
                statisticalMetric: valueName,
                factor: factor,
                toleranceRel: toleranceRel);
        }

        var legacyPredicate = LegacyAssertionPredicateMapper.MapScalar(
            assertionTypeCode,
            actualRole: "followup",
            expectedRole: "source",
            metric: valueName);
        return ForLegacyScalar(
            mrCode: mrCode,
            valueName: valueName,
            predicate: legacyPredicate,
            toleranceAbs: toleranceAbs,
            toleranceRel: toleranceRel);
    }

    public static MrSpec ForLegacyScaling(
        string mrCode,
        string valueName,
        ScaledEqualityPredicate predicate,
        string factorParameterName,
        IReadOnlyDictionary<string, string> parameterValues,
        double toleranceAbs,
        double toleranceRel)
    {
        if (!parameterValues.TryGetValue(factorParameterName, out var raw)
            || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var factorValue))
        {
            throw new System.ArgumentException(
                $"ScaledEqualityPredicate factor parameter '{factorParameterName}' must be present and parse as a double.",
                nameof(parameterValues));
        }

        var parameters = new Dictionary<string, ParameterExpression>
        {
            [factorParameterName] = new ConstantParameterExpression(factorValue),
        };
        return Build(mrCode, valueName, predicate, parameters,
            toleranceAbs: toleranceAbs, toleranceRel: toleranceRel,
            actualRole: predicate.ActualRole, expectedRole: predicate.ReferenceRole);
    }

    /// <summary>
    /// Build an <see cref="MrSpec"/> wrapping a <see cref="VarianceRatioPredicate"/>
    /// for the pipeline variance-ratio routing arm. Convention:
    /// the expectedRole (default <c>"source"</c>) is the low-sample side (larger σ),
    /// the actualRole (default <c>"followup"</c>) is the high-sample side (smaller σ
    /// after the refinement scaling); the <c>SigmaMultiplier</c> of the statistical
    /// tolerance is <c>1.0 + toleranceRel</c> so a 30 % rel blueprint tolerance
    /// allows the observed <c>high.StdError</c> to sit up to 30 % above
    /// <c>low.StdError / √sampleRatio</c>. Throws <see cref="System.ArgumentException"/>
    /// for blank inputs, non-finite or ≤ 1 factor, or non-positive toleranceRel — the
    /// pipeline translates the throw into an `UnknownType` assertion result.
    /// </summary>
    public static MrSpec ForVarianceRatio(
        string mrCode,
        string statisticalMetric,
        double factor,
        double toleranceRel,
        string actualRole = "followup",
        string expectedRole = "source")
    {
        if (string.IsNullOrWhiteSpace(mrCode))
            throw new System.ArgumentException("mrCode is required.", nameof(mrCode));
        if (string.IsNullOrWhiteSpace(statisticalMetric))
            throw new System.ArgumentException("statisticalMetric is required.", nameof(statisticalMetric));
        if (string.IsNullOrWhiteSpace(actualRole))
            throw new System.ArgumentException("actualRole is required.", nameof(actualRole));
        if (string.IsNullOrWhiteSpace(expectedRole))
            throw new System.ArgumentException("expectedRole is required.", nameof(expectedRole));
        if (!double.IsFinite(factor) || factor <= 1.0)
            throw new System.ArgumentException(
                $"variance-ratio sample factor must be finite and > 1; got {factor.ToString(CultureInfo.InvariantCulture)}.",
                nameof(factor));
        if (!double.IsFinite(toleranceRel) || toleranceRel <= 0.0)
            throw new System.ArgumentException(
                $"variance-ratio toleranceRel must be finite and > 0; got {toleranceRel.ToString(CultureInfo.InvariantCulture)}.",
                nameof(toleranceRel));

        var predicate = new VarianceRatioPredicate(
            PredicateId: $"{statisticalMetric}-variance-ratio",
            LowSampleRole: expectedRole,
            HighSampleRole: actualRole,
            StatisticalMetric: statisticalMetric,
            SampleRatio: new ConstantParameterExpression(factor),
            Tolerance: new StatisticalToleranceSpec(1.0 + toleranceRel));

        var roles = new Dictionary<string, RunRoleSpec>
        {
            [expectedRole] = new("Baseline"),
            [actualRole] = new("Followup"),
        };
        var projections = new Dictionary<string, ProjectionSpec>
        {
            [statisticalMetric] = new StatisticalProjectionSpec(
                MeanPath: $"/values/{statisticalMetric}_mean",
                StdErrorPath: $"/values/{statisticalMetric}"),
        };
        return new MrSpec(
            Kind: "MrSpec",
            MrId: mrCode,
            Name: mrCode,
            Description: null,
            Tags: null,
            Parameters: null,
            Roles: roles,
            Projections: projections,
            Predicates: new[] { (PredicateSpec)predicate },
            DefaultTolerance: new DeterministicToleranceSpec(0.0, toleranceRel));
    }

    private static (string actualRole, string expectedRole) RolesFor(PredicateSpec predicate) =>
        predicate switch
        {
            BinaryComparisonPredicate binary => (binary.LeftRole, binary.RightRole),
            ScaledEqualityPredicate scaled => (scaled.ActualRole, scaled.ReferenceRole),
            _ => ("followup", "source"),
        };

    private static MrSpec Build(
        string mrCode,
        string valueName,
        PredicateSpec predicate,
        IReadOnlyDictionary<string, ParameterExpression>? parameters,
        double toleranceAbs,
        double toleranceRel,
        string actualRole,
        string expectedRole)
    {
        var roles = new Dictionary<string, RunRoleSpec>
        {
            [expectedRole] = new("Baseline"),
            [actualRole] = new("Followup"),
        };
        var projections = new Dictionary<string, ProjectionSpec>
        {
            [valueName] = new ScalarProjectionSpec($"/values/{valueName}"),
        };
        return new MrSpec(
            Kind: "MrSpec",
            MrId: mrCode,
            Name: mrCode,
            Description: null,
            Tags: null,
            Parameters: parameters,
            Roles: roles,
            Projections: projections,
            Predicates: new[] { predicate },
            DefaultTolerance: new DeterministicToleranceSpec(toleranceAbs, toleranceRel));
    }
}
