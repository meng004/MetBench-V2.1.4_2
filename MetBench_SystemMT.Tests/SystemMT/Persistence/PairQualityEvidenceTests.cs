using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LiteDB;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

public sealed class PairQualityEvidenceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly string _connectionString;

    public PairQualityEvidenceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"metbench-pair-quality-{Guid.NewGuid():N}.litedb");
        _connectionString = $"Filename={_tempFile}";
    }

    public void Dispose()
    {
        try { File.Delete(_tempFile); } catch { /* cleanup best effort */ }
    }

    [Fact]
    public async Task ExecutionEvidence_saves_and_round_trips_pair_quality_summary()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeEvidence(executionId);
        evidence.PairQuality = new PairQualitySummary
        {
            PlannedPairs = 3,
            ExecutedPairs = 3,
            ValidPairs = 2,
            PassedPairs = 1,
            FailedPairs = 1,
            SkippedPairs = 1,
            InvalidSpecPairs = 0,
            PassRateValid = 0.5,
            PassRateAll = 1.0 / 3.0,
            SkipReasons =
            {
                new PairQualityReasonCount
                {
                    Status = "SkippedMissingObservable",
                    Reason = "missing metric",
                    Count = 1,
                },
            },
        };

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.PairQuality);
        Assert.Equal(3, loaded.PairQuality.PlannedPairs);
        Assert.Equal(3, loaded.PairQuality.ExecutedPairs);
        Assert.Equal(2, loaded.PairQuality.ValidPairs);
        Assert.Equal(1, loaded.PairQuality.PassedPairs);
        Assert.Equal(1, loaded.PairQuality.FailedPairs);
        Assert.Equal(1, loaded.PairQuality.SkippedPairs);
        Assert.Equal(0, loaded.PairQuality.InvalidSpecPairs);
        Assert.Equal(0.5, loaded.PairQuality.PassRateValid);
        Assert.Equal(1.0 / 3.0, loaded.PairQuality.PassRateAll, precision: 12);
        var reason = Assert.Single(loaded.PairQuality.SkipReasons);
        Assert.Equal("SkippedMissingObservable", reason.Status);
        Assert.Equal("missing metric", reason.Reason);
        Assert.Equal(1, reason.Count);
    }

    [Theory]
    [InlineData("Passed", 1, 1, 0, 0, 0, 0, 1.0, 1.0)]
    [InlineData("Failed", 1, 0, 1, 0, 0, 0, 0.0, 0.0)]
    [InlineData("SkippedMissingObservable", 0, 0, 0, 1, 0, 0, 0.0, 0.0)]
    [InlineData("SkippedNotApplicable", 0, 0, 0, 1, 0, 0, 0.0, 0.0)]
    [InlineData("InvalidSpec", 0, 0, 0, 0, 1, 1, 0.0, 0.0)]
    public void Two_role_pair_summary_counts_typed_status_without_folding_non_failed_statuses_into_passed(
        string status,
        int valid,
        int passed,
        int failed,
        int skipped,
        int invalid,
        int expectedInvalidReasons,
        double passRateValid,
        double passRateAll)
    {
        var summary = PairQualitySummary.FromVerificationResult(
            BinaryPredicate(),
            Verification(status),
            roleOutputsProduced: true);

        Assert.Equal(1, summary.PlannedPairs);
        Assert.Equal(1, summary.ExecutedPairs);
        Assert.Equal(valid, summary.ValidPairs);
        Assert.Equal(passed, summary.PassedPairs);
        Assert.Equal(failed, summary.FailedPairs);
        Assert.Equal(skipped, summary.SkippedPairs);
        Assert.Equal(invalid, summary.InvalidSpecPairs);
        Assert.Equal(passRateValid, summary.PassRateValid);
        Assert.Equal(passRateAll, summary.PassRateAll);
        Assert.Equal(expectedInvalidReasons, summary.InvalidSpecReasons.Count);
        Assert.True(summary.SkippedPairs == 0 || summary.SkipReasons.Count == 1);
    }

    [Fact]
    public void Multi_phase_passed_summary_uses_declared_ordered_phase_comparison_count()
    {
        var predicate = new ErrorMonotonicPredicate(
            PredicateId: "k-error-monotonic",
            OrderedRoles: new[] { "coarse", "medium", "fine" },
            ReferenceRole: "reference",
            Metric: "k_eff",
            NormKind: NormKind.Relative);

        var summary = PairQualitySummary.FromVerificationResult(
            predicate,
            Verification("Passed"),
            roleOutputsProduced: true);

        Assert.Equal(2, summary.PlannedPairs);
        Assert.Equal(2, summary.ExecutedPairs);
        Assert.Equal(2, summary.ValidPairs);
        Assert.Equal(2, summary.PassedPairs);
        Assert.Equal(0, summary.FailedPairs);
        Assert.Equal(1.0, summary.PassRateValid);
        Assert.Equal(1.0, summary.PassRateAll);
    }

    [Fact]
    public void Multi_phase_failed_summary_counts_only_the_failed_comparison_as_valid()
    {
        var predicate = new ErrorMonotonicPredicate(
            PredicateId: "k-error-monotonic",
            OrderedRoles: new[] { "coarse", "medium", "fine" },
            ReferenceRole: "reference",
            Metric: "k_eff",
            NormKind: NormKind.Relative);

        var summary = PairQualitySummary.FromVerificationResult(
            predicate,
            Verification("Failed"),
            roleOutputsProduced: true);

        Assert.Equal(2, summary.PlannedPairs);
        Assert.Equal(2, summary.ExecutedPairs);
        Assert.Equal(1, summary.ValidPairs);
        Assert.Equal(0, summary.PassedPairs);
        Assert.Equal(1, summary.FailedPairs);
        Assert.Equal(0.0, summary.PassRateValid);
        Assert.Equal(0.0, summary.PassRateAll);
    }

    [Fact]
    public async Task Legacy_evidence_row_missing_pair_quality_loads_default_empty_summary()
    {
        var executionId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        using (var db = new LiteDatabase(_connectionString, new BsonMapper()))
        {
            db.Pragma("UTC_DATE", true);
            var col = db.GetCollection("ExecutionEvidence");
            col.Insert(new BsonDocument
            {
                ["_id"] = evidenceId,
                ["ExecutionId"] = executionId,
                ["Metadata"] = new BsonDocument { ["MrId"] = "legacy-mr" },
                ["SampleTraces"] = new BsonArray(),
                ["TransformationParameters"] = new BsonDocument(),
                ["RecordedAtUtc"] = DateTime.UtcNow,
            });
            col.EnsureIndex("ExecutionId", unique: true);
        }

        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.PairQuality);
        Assert.Equal(0, loaded.PairQuality.PlannedPairs);
        Assert.Equal(0, loaded.PairQuality.ExecutedPairs);
        Assert.Equal(0, loaded.PairQuality.ValidPairs);
        Assert.Equal(0.0, loaded.PairQuality.PassRateValid);
        Assert.Empty(loaded.PairQuality.SkipReasons);
        Assert.Empty(loaded.PairQuality.InvalidSpecReasons);
    }

    private static ExecutionEvidence MakeEvidence(Guid executionId) => new()
    {
        IdEvidence = Guid.NewGuid(),
        ExecutionId = executionId,
        Metadata = new ExecutionMetadataSnapshot
        {
            MrId = "heat-equation-amplitude",
            SutName = "heat-equation",
            Equation = "Fourier",
            ProgramType = "Num",
            MetaPattern = "Mono",
            SourceLevel = "Manual",
            FailureCorrelation = "None",
            MetbenchVersion = "v2.2-dev",
        },
        TransformationParameters = new Dictionary<string, string> { ["factor"] = "2" },
    };

    private static BinaryComparisonPredicate BinaryPredicate() => new(
        PredicateId: "max_u-greater",
        LeftRole: "source",
        RightRole: "followup",
        Metric: "max_u",
        Operator: "Greater");

    private static VerificationResult Verification(string status) => status switch
    {
        "Passed" => VerificationResult.FromAssertion(Assertion(passed: true), new VerificationDiagnostic(1.0, 2.0, 1.0, 0.0)),
        "Failed" => VerificationResult.FromAssertion(Assertion(passed: false), new VerificationDiagnostic(2.0, 1.0, 1.0, 0.0)),
        "SkippedMissingObservable" => VerificationResult.SkippedMissingObservable("missing metric"),
        "SkippedNotApplicable" => VerificationResult.SkippedNotApplicable("outside applicability"),
        "InvalidSpec" => VerificationResult.InvalidSpec("bad spec"),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unexpected status."),
    };

    private static SystemMtAssertionResultV2 Assertion(bool passed) => new(
        AssertionTypeCode: "greater",
        Passed: passed,
        SourceValue: 1.0,
        FollowupValue: passed ? 2.0 : 0.5,
        ObservedDelta: 1.0,
        ExpectedThreshold: 0.0,
        Expression: "follow > source",
        FailureReason: passed ? null : "not greater");
}
