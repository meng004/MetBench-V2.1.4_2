using System;
using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Additive execution-evidence projection that summarizes pair-level quality for
/// one System-MT run. Missing legacy rows deserialize to the default-empty shape
/// (all counts/rates zero and empty reason distributions).
/// </summary>
public sealed class PairQualitySummary
{
    public int PlannedPairs { get; set; }
    public int ExecutedPairs { get; set; }
    public int ValidPairs { get; set; }
    public int PassedPairs { get; set; }
    public int FailedPairs { get; set; }
    public int SkippedPairs { get; set; }
    public int InvalidSpecPairs { get; set; }
    public double PassRateValid { get; set; }
    public double PassRateAll { get; set; }
    private List<PairQualityReasonCount>? _skipReasons = new();
    private List<PairQualityReasonCount>? _invalidSpecReasons = new();

    public List<PairQualityReasonCount> SkipReasons
    {
        get => _skipReasons ??= new();
        set => _skipReasons = value ?? new();
    }

    public List<PairQualityReasonCount> InvalidSpecReasons
    {
        get => _invalidSpecReasons ??= new();
        set => _invalidSpecReasons = value ?? new();
    }

    public static PairQualitySummary Empty() => new();

    public static PairQualitySummary FromVerificationResult(
        PredicateSpec? predicate,
        VerificationResult? verification,
        bool roleOutputsProduced)
    {
        if (verification is null)
        {
            return Empty();
        }

        var plannedPairs = CountPlannedPairs(predicate);
        var executedPairs = roleOutputsProduced ? plannedPairs : 0;
        var summary = new PairQualitySummary
        {
            PlannedPairs = plannedPairs,
            ExecutedPairs = executedPairs,
        };

        switch (verification.Status)
        {
            case VerifyStatus.Passed:
                summary.ValidPairs = plannedPairs;
                summary.PassedPairs = plannedPairs;
                break;
            case VerifyStatus.Failed:
                summary.ValidPairs = Math.Min(1, plannedPairs);
                summary.FailedPairs = Math.Min(1, plannedPairs);
                break;
            case VerifyStatus.SkippedMissingObservable:
            case VerifyStatus.SkippedNotApplicable:
                summary.SkippedPairs = plannedPairs;
                summary.SkipReasons.Add(PairQualityReasonCount.From(
                    verification.Status.ToString(), verification.Context?.Reason, plannedPairs));
                break;
            case VerifyStatus.InvalidSpec:
                summary.InvalidSpecPairs = plannedPairs;
                summary.InvalidSpecReasons.Add(PairQualityReasonCount.From(
                    verification.Status.ToString(), verification.Context?.Reason, plannedPairs));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(verification), verification.Status, "Unsupported verification status.");
        }

        summary.RecalculateRates();
        return summary;
    }

    public void RecalculateRates()
    {
        PassRateValid = ValidPairs == 0 ? 0.0 : (double)PassedPairs / ValidPairs;
        PassRateAll = PlannedPairs == 0 ? 0.0 : (double)PassedPairs / PlannedPairs;
    }

    private static int CountPlannedPairs(PredicateSpec? predicate)
    {
        return predicate switch
        {
            ErrorMonotonicPredicate monotonic => Math.Max(0, monotonic.OrderedRoles.Count - 1),
            OrderedSequenceShapePredicate ordered => Math.Max(0, ordered.OrderedRoles.Count - 1),
            null => 1,
            _ => 1,
        };
    }
}

public sealed class PairQualityReasonCount
{
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }

    public static PairQualityReasonCount From(string status, string? reason, int count) => new()
    {
        Status = status,
        Reason = reason ?? string.Empty,
        Count = count,
    };
}
