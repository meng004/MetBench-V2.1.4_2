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

        var roles = new Dictionary<string, RoleOutput>(StringComparer.Ordinal)
        {
            [expectedRole] = new RoleOutput(expectedRole, new Dictionary<string, double>(sourceScalars)),
            [actualRole] = new RoleOutput(actualRole, new Dictionary<string, double>(followupScalars)),
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
            }
        }

        return ("followup", "source");
    }
}
