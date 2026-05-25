using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Property;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Persistence;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

/// <summary>
/// Pure-mapper contract for ExecutionEvidence v2's typed verification projection.
/// Locks the mapping table from §4 of
/// docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md
/// independently of LiteDB round-trip.
/// </summary>
public sealed class TypedVerificationEvidenceMapperTests
{
    private static MrSpec BuildMrSpec(PredicateSpec predicate)
        => TypedSpecFactory.ForLegacyScalar(
            mrCode: "MR-test",
            valueName: "k_eff",
            predicate: predicate,
            toleranceAbs: 1e-9,
            toleranceRel: 0.0);

    [Fact]
    public void Maps_passing_VerificationResult_into_TypedVerification()
    {
        var predicate = (BinaryComparisonPredicate)LegacyAssertionPredicateMapper.MapScalar(
            "less", "followup", "source", "k_eff");
        var spec = BuildMrSpec(predicate);
        var assertion = new SystemMtAssertionResultV2(
            AssertionTypeCode: "Less",
            Passed: true,
            SourceValue: 1.13,
            FollowupValue: 0.51,
            ObservedDelta: 0.62,
            ExpectedThreshold: 1e-9,
            Expression: "followup.k_eff Less source.k_eff",
            FailureReason: null);
        var result = VerificationResult.FromAssertion(
            assertion,
            new VerificationDiagnostic(1.13, 0.51, 0.62, 1e-9));

        var evidence = TypedVerificationEvidenceMapper.FromVerificationResult(spec, predicate, result);

        Assert.Equal("MrSpec", evidence.SpecKind);
        Assert.Equal("MR-test", evidence.SpecId);
        Assert.Equal("BinaryComparison", evidence.PredicateKind);
        Assert.Equal("Passed", evidence.Status);
        Assert.Equal(true, evidence.Passed);
        Assert.NotNull(evidence.Diagnostic);
        Assert.Equal(1.13, evidence.Diagnostic!.Expected);
        Assert.Equal(0.51, evidence.Diagnostic.Actual);
        Assert.Equal(0.62, evidence.Diagnostic.Residual);
        Assert.Equal(1e-9, evidence.Diagnostic.Tolerance);
        Assert.Null(evidence.SkipOrInvalidReason);
        Assert.Empty(evidence.PropertyPredicates);
        Assert.Equal(predicate.PredicateId, evidence.PredicateId);
    }

    [Fact]
    public void Maps_skipped_missing_observable_with_reason_and_null_diagnostic()
    {
        var predicate = (BinaryComparisonPredicate)LegacyAssertionPredicateMapper.MapScalar(
            "greater", "followup", "source", "k_eff");
        var spec = BuildMrSpec(predicate);
        var result = VerificationResult.SkippedMissingObservable("missing source field");

        var evidence = TypedVerificationEvidenceMapper.FromVerificationResult(spec, predicate, result);

        Assert.Equal("SkippedMissingObservable", evidence.Status);
        Assert.Null(evidence.Passed);
        Assert.Null(evidence.Diagnostic);
        Assert.Equal("missing source field", evidence.SkipOrInvalidReason);
        Assert.Empty(evidence.PropertyPredicates);
    }

    [Fact]
    public void Maps_skipped_not_applicable_with_reason_and_null_diagnostic()
    {
        var predicate = (BinaryComparisonPredicate)LegacyAssertionPredicateMapper.MapScalar(
            "less", "followup", "source", "k_eff");
        var spec = BuildMrSpec(predicate);
        var result = VerificationResult.SkippedNotApplicable("applicability gate closed");

        var evidence = TypedVerificationEvidenceMapper.FromVerificationResult(spec, predicate, result);

        Assert.Equal("SkippedNotApplicable", evidence.Status);
        Assert.Null(evidence.Passed);
        Assert.Null(evidence.Diagnostic);
        Assert.Equal("applicability gate closed", evidence.SkipOrInvalidReason);
    }

    [Fact]
    public void Maps_invalid_spec_with_reason_and_null_passed()
    {
        var predicate = (BinaryComparisonPredicate)LegacyAssertionPredicateMapper.MapScalar(
            "less", "followup", "source", "k_eff");
        var spec = BuildMrSpec(predicate);
        var result = VerificationResult.InvalidSpec("Spec validation failed before runtime dispatch.");

        var evidence = TypedVerificationEvidenceMapper.FromVerificationResult(spec, predicate, result);

        Assert.Equal("InvalidSpec", evidence.Status);
        Assert.Null(evidence.Passed);
        Assert.Equal("Spec validation failed before runtime dispatch.", evidence.SkipOrInvalidReason);
    }

    [Fact]
    public void Maps_PropertyResult_held_with_two_predicate_results_in_order()
    {
        var spec = new PropertySpec(
            Kind: "PropertySpec",
            PropertyId: "neutron-flux-positivity",
            Name: "Neutron flux positivity",
            Description: null,
            Tags: null,
            Case: null,
            Projections: null,
            Assertions: System.Array.Empty<PropertyPredicateSpec>(),
            DefaultTolerance: new DeterministicToleranceSpec(0.0, 0.0));

        var result = new PropertyResult(
            PropertyId: "neutron-flux-positivity",
            Status: PropertyStatus.Held,
            PredicateResults: new[]
            {
                new PropertyPredicateResult(
                    PredicateId: "phi-nonneg",
                    PredicateKind: "Bound",
                    Status: PropertyStatus.Held,
                    Actual: 0.5,
                    Expected: new { lower = 0.0 },
                    Residual: 0.0,
                    Tolerance: 1e-12,
                    Reason: null),
                new PropertyPredicateResult(
                    PredicateId: "phi-shape",
                    PredicateKind: "Shape",
                    Status: PropertyStatus.Held,
                    Actual: new[] { 1.0, 0.9, 0.5 },
                    Expected: "NonIncreasing",
                    Residual: 0.0,
                    Tolerance: 0.0,
                    Reason: null),
            },
            Reason: null);

        var evidence = TypedVerificationEvidenceMapper.FromPropertyResult(spec, result);

        Assert.Equal("PropertySpec", evidence.SpecKind);
        Assert.Equal("neutron-flux-positivity", evidence.SpecId);
        Assert.Equal("Held", evidence.Status);
        Assert.True(evidence.Passed);
        Assert.Null(evidence.Diagnostic);
        Assert.Equal(2, evidence.PropertyPredicates.Count);

        var first = evidence.PropertyPredicates[0];
        Assert.Equal("phi-nonneg", first.PredicateId);
        Assert.Equal("Bound", first.PredicateKind);
        Assert.Equal("Held", first.Status);
        Assert.Equal(0.0, first.Residual);
        Assert.Equal(1e-12, first.Tolerance);
        Assert.False(string.IsNullOrEmpty(first.ExpectedJson));
        Assert.Contains("0", first.ExpectedJson!);
        Assert.Equal("0.5", first.ActualJson);

        var second = evidence.PropertyPredicates[1];
        Assert.Equal("phi-shape", second.PredicateId);
        Assert.Equal("Shape", second.PredicateKind);
        Assert.Equal("\"NonIncreasing\"", second.ExpectedJson);
        Assert.NotNull(second.ActualJson);
        Assert.StartsWith("[", second.ActualJson);
    }

    [Fact]
    public void Maps_PropertyResult_violated_with_reason()
    {
        var spec = new PropertySpec(
            Kind: "PropertySpec",
            PropertyId: "phi-bound",
            Name: "Phi bound",
            Description: null,
            Tags: null,
            Case: null,
            Projections: null,
            Assertions: System.Array.Empty<PropertyPredicateSpec>(),
            DefaultTolerance: new DeterministicToleranceSpec(0.0, 0.0));

        var result = new PropertyResult(
            PropertyId: "phi-bound",
            Status: PropertyStatus.Violated,
            PredicateResults: new[]
            {
                new PropertyPredicateResult(
                    PredicateId: "phi-upper",
                    PredicateKind: "Bound",
                    Status: PropertyStatus.Violated,
                    Actual: 1.5,
                    Expected: new { upper = 1.0 },
                    Residual: 0.5,
                    Tolerance: 1e-9,
                    Reason: "phi=1.5 exceeded upper=1.0"),
            },
            Reason: "upper bound exceeded");

        var evidence = TypedVerificationEvidenceMapper.FromPropertyResult(spec, result);

        Assert.Equal("Violated", evidence.Status);
        Assert.False(evidence.Passed);
        Assert.Equal("upper bound exceeded", evidence.SkipOrInvalidReason);
        var only = Assert.Single(evidence.PropertyPredicates);
        Assert.Equal("Violated", only.Status);
        Assert.Equal(0.5, only.Residual);
        Assert.Equal("phi=1.5 exceeded upper=1.0", only.Reason);
    }
}
