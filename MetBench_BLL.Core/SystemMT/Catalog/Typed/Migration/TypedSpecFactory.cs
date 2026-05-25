using System.Collections.Generic;
using System.Globalization;
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
