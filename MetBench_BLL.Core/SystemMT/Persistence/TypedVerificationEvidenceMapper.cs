using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MetBench_BLL.SystemMT.Catalog.Typed.Property;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Stateless mapper that projects typed verifier outputs (the
/// <see cref="VerificationResult"/> / <see cref="PropertyResult"/> records
/// produced by the Typed Semantic Catalog runtime) into the persistence-shaped
/// <see cref="TypedVerificationEvidence"/> block on
/// <see cref="ExecutionEvidence"/>. Numeric / object Expected and Actual
/// fields are JSON-serialised with <see cref="JsonSerializerDefaults.General"/>
/// so the stored representation is culture-independent.
/// </summary>
public static class TypedVerificationEvidenceMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

    public static TypedVerificationEvidence FromVerificationResult(
        MrSpec spec,
        PredicateSpec predicate,
        VerificationResult result)
    {
        var evidence = new TypedVerificationEvidence
        {
            SpecId = spec.MrId,
            SpecKind = "MrSpec",
            PredicateId = predicate.PredicateId,
            PredicateKind = PredicateKindOf(predicate),
            Status = result.Status.ToString(),
        };

        switch (result.Status)
        {
            case VerifyStatus.Passed:
            case VerifyStatus.Failed:
                evidence.Passed = result.Assertion?.Passed ?? (result.Status == VerifyStatus.Passed);
                if (result.Diagnostic is { } diagnostic)
                {
                    evidence.Diagnostic = new TypedDiagnosticEvidence
                    {
                        Expected = diagnostic.Expected,
                        Actual = diagnostic.Actual,
                        Residual = diagnostic.Residual,
                        Tolerance = diagnostic.Tolerance,
                    };
                }
                break;
            case VerifyStatus.SkippedNotApplicable:
            case VerifyStatus.SkippedMissingObservable:
            case VerifyStatus.InvalidSpec:
                evidence.Passed = null;
                evidence.Diagnostic = null;
                evidence.SkipOrInvalidReason = result.Context?.Reason;
                break;
        }

        return evidence;
    }

    public static TypedVerificationEvidence FromPropertyResult(
        PropertySpec spec,
        PropertyResult result)
    {
        var evidence = new TypedVerificationEvidence
        {
            SpecId = spec.PropertyId,
            SpecKind = "PropertySpec",
            PredicateId = string.Empty,
            PredicateKind = string.Empty,
            Status = result.Status.ToString(),
        };

        switch (result.Status)
        {
            case PropertyStatus.Held:
                evidence.Passed = true;
                break;
            case PropertyStatus.Violated:
                evidence.Passed = false;
                break;
            case PropertyStatus.SkippedMissingObservable:
            case PropertyStatus.InvalidSpec:
                evidence.Passed = null;
                break;
        }

        if (!string.IsNullOrEmpty(result.Reason))
        {
            evidence.SkipOrInvalidReason = result.Reason;
        }

        evidence.PropertyPredicates = result.PredicateResults
            .Select(pr => new TypedPropertyPredicateEvidence
            {
                PredicateId = pr.PredicateId,
                PredicateKind = pr.PredicateKind,
                Status = pr.Status.ToString(),
                Residual = pr.Residual,
                Tolerance = pr.Tolerance,
                Reason = pr.Reason,
                ExpectedJson = SerializeOrNull(pr.Expected),
                ActualJson = SerializeOrNull(pr.Actual),
            })
            .ToList();

        return evidence;
    }

    private static string PredicateKindOf(PredicateSpec predicate) =>
        predicate switch
        {
            BinaryComparisonPredicate => "BinaryComparison",
            ScaledEqualityPredicate => "ScaledEquality",
            CrossMethodComparisonPredicate => "CrossMethodComparison",
            ErrorMonotonicPredicate => "ErrorMonotonic",
            OrderedSequenceShapePredicate => "OrderedSequenceShape",
            VarianceRatioPredicate => "VarianceRatio",
            SubadditivePredicate => "Subadditive",
            FieldEqualityPredicate => "FieldEquality",
            FieldProportionalityPredicate => "FieldProportionality",
            DerivedInvariantPredicate => "DerivedInvariant",
            _ => predicate.GetType().Name,
        };

    private static string? SerializeOrNull(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, value.GetType(), JsonOptions);
    }
}
