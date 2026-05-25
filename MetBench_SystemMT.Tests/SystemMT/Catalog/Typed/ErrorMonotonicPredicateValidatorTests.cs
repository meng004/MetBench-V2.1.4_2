using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class ErrorMonotonicPredicateValidatorTests
{
    [Fact]
    public void Validate_rejects_duplicate_ordered_roles()
    {
        var result = new ErrorMonotonicPredicateValidator(new SharedReferenceResolver())
            .Validate(
                new ErrorMonotonicPredicate("p1", new[] { "coarse", "coarse" }, "reference", "k_eff", NormKind.Absolute),
                BuildSpec());

        Assert.False(result.IsValid);
    }

    private static MrSpec BuildSpec() =>
        new(
            "MrSpec",
            "mr-conv",
            "conv",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>
            {
                ["coarse"] = new("Baseline"),
                ["fine"] = new("Followup"),
                ["reference"] = new("Reference")
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["k_eff"] = new ScalarProjectionSpec("/results/k_eff")
            },
            new PredicateSpec[]
            {
                new ErrorMonotonicPredicate("p1", new[] { "coarse", "fine" }, "reference", "k_eff", NormKind.Absolute)
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));
}
