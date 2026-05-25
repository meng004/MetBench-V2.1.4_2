using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Serialization;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class OrderedSequenceShapePredicateTests
{
    [Fact]
    public void Ordered_sequence_shape_yaml_deserializes_and_validates()
    {
        const string yaml = """
kind: MrSpec
mr_id: dif-phy-09
name: Differential rod worth forms bell shape
description: Ordered run sequence should form a bell-shaped differential worth trajectory.
tags:
  equation_key: Diffusion
  program_type: Deterministic
roles:
  h0:
    kind: Baseline
  h1:
    kind: Followup
  h2:
    kind: Followup
  h3:
    kind: Followup
  h4:
    kind: Followup
projections:
  diff_worth:
    kind: ScalarProjection
    path: /results/diff_worth
predicates:
  - kind: OrderedSequenceShape
    predicate_id: bell-shape
    ordered_roles: [h0, h1, h2, h3, h4]
    metric: diff_worth
    shape:
      kind: BellShape
default_tolerance:
  kind: DeterministicTolerance
  atol: 0.0
  rtol: 0.0
""";

        var spec = TypedCatalogSerializer.DeserializeMrSpec(yaml);

        Assert.True(spec.Validate().IsValid);
        Assert.IsType<OrderedSequenceShapePredicate>(Assert.Single(spec.Predicates));
    }

    [Fact]
    public void Predicate_dispatcher_evaluates_ordered_sequence_shape_over_scalar_roles()
    {
        var spec = new MrSpec(
            "MrSpec",
            "dif-phy-09",
            "Differential rod worth forms bell shape",
            null,
            new Dictionary<string, string> { ["equation_key"] = "Diffusion", ["program_type"] = "Deterministic" },
            null,
            new Dictionary<string, RunRoleSpec>
            {
                ["h0"] = new("Baseline"),
                ["h1"] = new("Followup"),
                ["h2"] = new("Followup"),
                ["h3"] = new("Followup"),
                ["h4"] = new("Followup"),
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["diff_worth"] = new ScalarProjectionSpec("/results/diff_worth"),
            },
            new PredicateSpec[]
            {
                new OrderedSequenceShapePredicate(
                    "bell-shape",
                    new[] { "h0", "h1", "h2", "h3", "h4" },
                    "diff_worth",
                    new BellShapeSpec()),
            },
            new DeterministicToleranceSpec(0.0, 0.0));

        var context = new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["h0"] = ScalarRole("h0", "diff_worth", 1.0),
                ["h1"] = ScalarRole("h1", "diff_worth", 3.0),
                ["h2"] = ScalarRole("h2", "diff_worth", 5.0),
                ["h3"] = ScalarRole("h3", "diff_worth", 3.0),
                ["h4"] = ScalarRole("h4", "diff_worth", 1.0),
            });

        var result = new PredicateDispatcher().Dispatch(spec.Predicates[0], context);

        Assert.Equal(VerifyStatus.Passed, result.Status);
        Assert.True(result.Assertion?.Passed);
    }

    [Fact]
    public void Validator_rejects_non_scalar_projection_for_ordered_sequence_shape()
    {
        var spec = new MrSpec(
            "MrSpec",
            "dif-phy-09",
            "Differential rod worth forms bell shape",
            null,
            new Dictionary<string, string> { ["equation_key"] = "Diffusion", ["program_type"] = "Deterministic" },
            null,
            new Dictionary<string, RunRoleSpec>
            {
                ["h0"] = new("Baseline"),
                ["h1"] = new("Followup"),
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["diff_worth"] = new Field2DProjectionSpec("/results/diff_worth", 2, 2),
            },
            new PredicateSpec[]
            {
                new OrderedSequenceShapePredicate(
                    "bell-shape",
                    new[] { "h0", "h1" },
                    "diff_worth",
                    new BellShapeSpec()),
            },
            new DeterministicToleranceSpec(0.0, 0.0));

        var result = spec.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Path.Contains(".metric", System.StringComparison.Ordinal));
    }

    private static RoleOutput ScalarRole(string role, string metric, double value) =>
        new(role, new Dictionary<string, double> { [metric] = value });
}
