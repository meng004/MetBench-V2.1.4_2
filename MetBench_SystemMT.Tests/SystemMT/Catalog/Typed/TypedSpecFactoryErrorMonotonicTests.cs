using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// PR-Bol-2A pin: <see cref="TypedSpecFactory.ForErrorMonotonic"/> builds a typed
/// <see cref="MrSpec"/> wrapping an <see cref="ErrorMonotonicPredicate"/> with the
/// correct role layout (one Baseline role per OrderedRoles entry + one Reference
/// role) and fails closed on bad inputs.
/// </summary>
public sealed class TypedSpecFactoryErrorMonotonicTests
{
    [Fact]
    public void ForErrorMonotonic_emits_predicate_with_expected_ordered_and_reference_roles()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            mrCode: "mr-em",
            metric: "k_eff",
            orderedRoles: new[] { "coarse", "medium" },
            referenceRole: "reference");

        var predicate = Assert.IsType<ErrorMonotonicPredicate>(spec.Predicates![0]);
        Assert.Equal("k_eff", predicate.Metric);
        Assert.Equal("reference", predicate.ReferenceRole);
        Assert.Equal(new[] { "coarse", "medium" }, predicate.OrderedRoles);
        Assert.Equal("k_eff-error-monotonic", predicate.PredicateId);
    }

    [Fact]
    public void ForErrorMonotonic_default_norm_kind_is_relative()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            mrCode: "mr-em", metric: "k_eff",
            orderedRoles: new[] { "coarse", "medium" }, referenceRole: "reference");

        var predicate = (ErrorMonotonicPredicate)spec.Predicates![0];
        Assert.Equal(NormKind.Relative, predicate.NormKind);
    }

    [Fact]
    public void ForErrorMonotonic_honors_explicit_norm_kind()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            mrCode: "mr-em", metric: "k_eff",
            orderedRoles: new[] { "coarse", "medium" }, referenceRole: "reference",
            normKind: NormKind.Absolute);

        var predicate = (ErrorMonotonicPredicate)spec.Predicates![0];
        Assert.Equal(NormKind.Absolute, predicate.NormKind);
    }

    [Fact]
    public void ForErrorMonotonic_emits_baseline_role_per_ordered_entry_plus_reference_role()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            mrCode: "mr-em", metric: "k_eff",
            orderedRoles: new[] { "coarse", "medium" }, referenceRole: "reference");

        Assert.NotNull(spec.Roles);
        Assert.Equal(3, spec.Roles!.Count);
        Assert.Equal("Baseline", spec.Roles["coarse"].Kind);
        Assert.Equal("Baseline", spec.Roles["medium"].Kind);
        Assert.Equal("Reference", spec.Roles["reference"].Kind);
    }

    [Fact]
    public void ForErrorMonotonic_emits_scalar_projection_for_the_metric()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            mrCode: "mr-em", metric: "k_eff",
            orderedRoles: new[] { "coarse", "medium" }, referenceRole: "reference");

        Assert.NotNull(spec.Projections);
        var projection = Assert.IsType<ScalarProjectionSpec>(spec.Projections!["k_eff"]);
        Assert.Equal("/values/k_eff", projection.Path);
    }

    [Fact]
    public void ForErrorMonotonic_result_validates_via_typed_validator()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            mrCode: "mr-em", metric: "k_eff",
            orderedRoles: new[] { "coarse", "medium" }, referenceRole: "reference");

        var validation = spec.Validate();
        Assert.True(validation.IsValid,
            "ForErrorMonotonic spec must pass typed validation. Errors: "
            + string.Join("; ", validation.Errors));
    }

    [Fact]
    public void ForErrorMonotonic_rejects_fewer_than_two_ordered_roles()
    {
        Assert.Throws<ArgumentException>(() =>
            TypedSpecFactory.ForErrorMonotonic(
                mrCode: "mr-em", metric: "k_eff",
                orderedRoles: new[] { "single" }, referenceRole: "reference"));
    }

    [Fact]
    public void ForErrorMonotonic_rejects_duplicate_ordered_roles()
    {
        Assert.Throws<ArgumentException>(() =>
            TypedSpecFactory.ForErrorMonotonic(
                mrCode: "mr-em", metric: "k_eff",
                orderedRoles: new[] { "coarse", "coarse" }, referenceRole: "reference"));
    }

    [Fact]
    public void ForErrorMonotonic_rejects_ordered_role_matching_reference_role()
    {
        Assert.Throws<ArgumentException>(() =>
            TypedSpecFactory.ForErrorMonotonic(
                mrCode: "mr-em", metric: "k_eff",
                orderedRoles: new[] { "coarse", "reference" }, referenceRole: "reference"));
    }
}
