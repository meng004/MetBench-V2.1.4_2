using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.Coverage;
using MetBench_BLL.Reporting;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Reporting;

public sealed class SystemMtReportServiceTests : IDisposable
{
    private readonly string _tmpDir = Path.Combine(Path.GetTempPath(),
        $"metbench-report-tests-{Guid.NewGuid():N}");

    public SystemMtReportServiceTests() => Directory.CreateDirectory(_tmpDir);
    public void Dispose() => Directory.Delete(_tmpDir, recursive: true);

    [Fact]
    public void GenerateExecution_writes_markdown_and_persists_report()
    {
        var (svc, fakes) = MakeService();
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        var path = Path.Combine(_tmpDir, "exec.md");

        var result = svc.GenerateExecution(execId, path);

        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        Assert.Contains("Execution Report", content);
        Assert.Contains("alice", content);
        Assert.Single(fakes.Reports.Data);
        Assert.Equal("single-execution", fakes.Reports.Data[0].Scope);
    }

    [Fact]
    public void GenerateAnomaly_writes_markdown()
    {
        var (svc, fakes) = MakeService();
        var anomalyId = Guid.NewGuid();
        fakes.Anomalies.Add(new MetBench_Domain.Anomaly
        {
            IdAnomaly = anomalyId, Severity = "major", Category = "basin",
            Status = MetBench_Domain.AnomalyStatus.Investigating, DiscoveredAt = DateTime.UtcNow, Notes = "note",
        });
        var path = Path.Combine(_tmpDir, "anomaly.md");

        var result = svc.GenerateAnomaly(anomalyId, path);

        var content = File.ReadAllText(path);
        Assert.Contains("Anomaly Report", content);
        Assert.Contains("basin", content);
        Assert.Equal("anomaly", fakes.Reports.Data[0].Scope);
    }

    [Fact]
    public void GenerateMutationCampaign_renders_detection_rate()
    {
        var (svc, fakes) = MakeService();
        var campaignId = Guid.NewGuid();
        fakes.Campaigns.Add(new MutationCampaign
        {
            IdCampaign = campaignId, Name = "smoke", Status = "ok",
            StartedAt = DateTime.UtcNow,
        });
        for (int i = 0; i < 3; i++)
            fakes.Cells.Add(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = campaignId, MutantId = i + 1, MRBindingId = 1, Outcome = "detected" });
        fakes.Cells.Add(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = campaignId, MutantId = 99, MRBindingId = 1, Outcome = "missed" });
        var path = Path.Combine(_tmpDir, "campaign.md");

        var result = svc.GenerateMutationCampaign(campaignId, path);

        var content = File.ReadAllText(path);
        Assert.Contains("Mutation Campaign", content);
        Assert.Contains("Detection rate: 75", content);
    }

    // GenerateWeekly 测试已删除（next-stage P0：Trend 子系统下线，5 scope → 4 scope）。

    [Fact]
    public void GenerateCoverage_renders_each_dimension()
    {
        var (svc, _) = MakeService();
        var report = new CoverageReport(
            DateTime.UtcNow,
            new MetaPatternCoverage(8, 4, new[] { "m_inv", "m_mono", "m_conv", "m_cmp" }, new[] { "m_adj", "m_rev", "m_dyn", "m_rel" }),
            new SutMrCoverage(2, 5, 7, Array.Empty<UnboundCell>()),
            new BugCoverage(6, 4, new[] { "R-Case-1", "R-Case-2", "R-Case-3", "R-Case-4" }, new[] { "R-Case-5", "R-Case-6" }),
            new MutationCoverage(2, 10, 7, 2));
        var path = Path.Combine(_tmpDir, "coverage.md");

        var result = svc.GenerateCoverage(report, path);

        var content = File.ReadAllText(path);
        Assert.Contains("MetaPattern coverage", content);
        Assert.Contains("4/8", content);
        Assert.Contains("SUT × MR coverage", content);
        Assert.Contains("Known-bug reproduction", content);
        Assert.Contains("Mutation detection", content);
    }

    [Fact]
    public void GenerateExecution_missing_id_throws()
    {
        var (svc, _) = MakeService();
        var path = Path.Combine(_tmpDir, "ex.md");
        Assert.Throws<InvalidOperationException>(() =>
            svc.GenerateExecution(Guid.NewGuid(), path));
    }

    // ---- PR-128: evidence-aware execution markdown ----

    [Fact]
    public void GenerateExecution_without_evidence_repo_does_not_include_typed_section()
    {
        var (svc, fakes) = MakeService();
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        var path = Path.Combine(_tmpDir, "exec.md");

        svc.GenerateExecution(execId, path);
        var content = File.ReadAllText(path);

        Assert.DoesNotContain("Typed verification", content);
        Assert.DoesNotContain("Spec kind", content);
    }

    [Fact]
    public void GenerateExecution_with_evidence_repo_but_no_evidence_row_does_not_include_typed_section()
    {
        var (svc, fakes) = MakeServiceWithEvidence(out var evRepo);
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        var path = Path.Combine(_tmpDir, "exec-noev.md");

        svc.GenerateExecution(execId, path);
        var content = File.ReadAllText(path);

        Assert.DoesNotContain("Typed verification", content);
    }

    [Fact]
    public async Task GenerateExecutionAsync_appends_typed_mr_verification_section_with_diagnostics_when_evidence_present()
    {
        var (svc, fakes) = MakeServiceWithEvidence(out var evRepo);
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        evRepo.Data[execId] = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = execId,
            TypedVerification = new TypedVerificationEvidence
            {
                SpecId = "heat-equation-amplitude",
                SpecKind = "MrSpec",
                PredicateId = "amplitude-greater",
                PredicateKind = "BinaryComparison",
                Status = "Passed",
                Passed = true,
                Diagnostic = new TypedDiagnosticEvidence
                {
                    Expected = 1.13,
                    Actual = 1.51,
                    Residual = 0.38,
                    Tolerance = 1e-6,
                },
            },
        };

        var path = Path.Combine(_tmpDir, "exec-with-ev.md");
        await svc.GenerateExecutionAsync(execId, path);
        var content = File.ReadAllText(path);

        Assert.Contains("## Typed verification", content);
        Assert.Contains("Spec kind: MrSpec", content);
        Assert.Contains("Spec ID: heat-equation-amplitude", content);
        Assert.Contains("Predicate: amplitude-greater (BinaryComparison)", content);
        Assert.Contains("Status: Passed", content);
        Assert.Contains("Expected: 1.13", content);
        Assert.Contains("Actual: 1.51", content);
        Assert.Contains("Residual: 0.38", content);
        Assert.Contains("Tolerance: 1E-06", content);
    }

    [Fact]
    public async Task GenerateExecutionAsync_appends_typed_mr_verification_section_when_evidence_present()
    {
        var (svc, fakes) = MakeServiceWithEvidence(out var evRepo);
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId,
            MRInstanceId = 5,
            Status = "ok",
            TriggeredBy = "alice",
            QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2",
            MetbenchVersion = "v2.1",
        });
        evRepo.Data[execId] = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = execId,
            TypedVerification = new TypedVerificationEvidence
            {
                SpecId = "heat-equation-amplitude",
                SpecKind = "MrSpec",
                PredicateId = "amplitude-greater",
                PredicateKind = "BinaryComparison",
                Status = "Passed",
                Passed = true,
            },
        };

        var path = Path.Combine(_tmpDir, "exec-with-ev-async.md");
        await svc.GenerateExecutionAsync(execId, path);
        var content = File.ReadAllText(path);

        Assert.Contains("## Typed verification", content);
        Assert.Contains("Spec ID: heat-equation-amplitude", content);
        Assert.Contains("Predicate: amplitude-greater (BinaryComparison)", content);
        Assert.Equal("single-execution", fakes.Reports.Data.Single().Scope);
    }

    [Fact]
    public async Task GenerateExecutionAsync_typed_skipped_evidence_shows_reason_and_omits_diagnostic()
    {
        var (svc, fakes) = MakeServiceWithEvidence(out var evRepo);
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        evRepo.Data[execId] = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = execId,
            TypedVerification = new TypedVerificationEvidence
            {
                SpecId = "heat-equation-amplitude",
                SpecKind = "MrSpec",
                Status = "SkippedMissingObservable",
                Passed = null,
                Diagnostic = null,
                SkipOrInvalidReason = "Required observable is missing.",
            },
        };

        var path = Path.Combine(_tmpDir, "exec-skipped.md");
        await svc.GenerateExecutionAsync(execId, path);
        var content = File.ReadAllText(path);

        Assert.Contains("Status: SkippedMissingObservable", content);
        Assert.Contains("Skip reason: Required observable is missing.", content);
        Assert.DoesNotContain("Expected:", content);
        Assert.DoesNotContain("Tolerance:", content);
    }


    [Fact]
    public async Task GenerateExecutionAsync_appends_pair_quality_section_when_evidence_present()
    {
        var (svc, fakes) = MakeServiceWithEvidence(out var evRepo);
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        evRepo.Data[execId] = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = execId,
            PairQuality = new PairQualitySummary
            {
                PlannedPairs = 4,
                ExecutedPairs = 3,
                ValidPairs = 2,
                PassedPairs = 1,
                FailedPairs = 1,
                SkippedPairs = 1,
                InvalidSpecPairs = 1,
                PassRateValid = 0.5,
                PassRateAll = 0.25,
                SkipReasons =
                {
                    new PairQualityReasonCount
                    {
                        Status = "SkippedMissingObservable",
                        Reason = "k_eff missing from followup role",
                        Count = 1,
                    },
                },
                InvalidSpecReasons =
                {
                    new PairQualityReasonCount
                    {
                        Status = "InvalidSpec",
                        Reason = "predicate role was not declared",
                        Count = 1,
                    },
                },
            },
        };

        var path = Path.Combine(_tmpDir, "exec-pair-quality.md");
        await svc.GenerateExecutionAsync(execId, path);
        var content = File.ReadAllText(path);

        Assert.Contains("## Pair quality", content);
        Assert.Contains("planned_pairs: 4", content);
        Assert.Contains("executed_pairs: 3", content);
        Assert.Contains("valid_pairs: 2", content);
        Assert.Contains("passed_pairs: 1", content);
        Assert.Contains("failed_pairs: 1", content);
        Assert.Contains("skipped_pairs: 1", content);
        Assert.Contains("invalid_spec_pairs: 1", content);
        Assert.Contains("pass_rate_valid: 50.0 %", content);
        Assert.Contains("pass_rate_all: 25.0 %", content);
        Assert.Contains("skip reasons", content);
        Assert.Contains("SkippedMissingObservable: k_eff missing from followup role (count: 1)", content);
        Assert.Contains("invalid spec reasons", content);
        Assert.Contains("InvalidSpec: predicate role was not declared (count: 1)", content);
    }

    [Fact]
    public async Task GenerateExecutionAsync_default_empty_pair_quality_keeps_legacy_no_pair_section_behavior()
    {
        var (svc, fakes) = MakeServiceWithEvidence(out var evRepo);
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        evRepo.Data[execId] = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = execId,
            PairQuality = new PairQualitySummary(),
        };

        var path = Path.Combine(_tmpDir, "exec-empty-pair-quality.md");
        await svc.GenerateExecutionAsync(execId, path);
        var content = File.ReadAllText(path);

        Assert.DoesNotContain("Pair quality", content);
        Assert.DoesNotContain("planned_pairs", content);
    }

    [Fact]
    public async Task GenerateExecutionAsync_typed_property_evidence_lists_predicates_in_order()
    {
        var (svc, fakes) = MakeServiceWithEvidence(out var evRepo);
        var execId = Guid.NewGuid();
        fakes.Executions.Add(new Execution
        {
            IdExecution = execId, MRInstanceId = 5, Status = "ok",
            TriggeredBy = "alice", QueuedAt = DateTime.UtcNow,
            SutVersionSnapshot = "v1.2", MetbenchVersion = "v2.1",
        });
        evRepo.Data[execId] = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = execId,
            TypedVerification = new TypedVerificationEvidence
            {
                SpecId = "neutron-flux-positivity",
                SpecKind = "PropertySpec",
                Status = "Violated",
                Passed = false,
                PropertyPredicates = new List<TypedPropertyPredicateEvidence>
                {
                    new() { PredicateId = "phi-nonneg",        PredicateKind = "Bound", Status = "Held"     },
                    new() { PredicateId = "phi-shape-monotone", PredicateKind = "Shape", Status = "Violated", Reason = "Decrease at index 4" },
                },
            },
        };

        var path = Path.Combine(_tmpDir, "exec-prop.md");
        await svc.GenerateExecutionAsync(execId, path);
        var content = File.ReadAllText(path);

        Assert.Contains("Spec kind: PropertySpec", content);
        Assert.Contains("Status: Violated", content);
        var nonnegIdx = content.IndexOf("phi-nonneg", StringComparison.Ordinal);
        var shapeIdx = content.IndexOf("phi-shape-monotone", StringComparison.Ordinal);
        Assert.True(nonnegIdx > 0 && shapeIdx > nonnegIdx,
            $"Property predicates must render in source order; got nonnegIdx={nonnegIdx}, shapeIdx={shapeIdx}.");
        Assert.Contains("Decrease at index 4", content);
    }

    private static (SystemMtReportService Svc, FakeBundle Fakes) MakeServiceWithEvidence(out FakeEvidenceRepo evRepo)
    {
        var fakes = new FakeBundle();
        evRepo = new FakeEvidenceRepo();
        var svc = new SystemMtReportService(
            fakes.Reports, fakes.Executions,
            fakes.Anomalies, fakes.Campaigns, fakes.Cells,
            evRepo);
        return (svc, fakes);
    }

    private static (SystemMtReportService, FakeBundle) MakeService()
    {
        var fakes = new FakeBundle();
        return (new SystemMtReportService(fakes.Reports, fakes.Executions,
            fakes.Anomalies, fakes.Campaigns, fakes.Cells), fakes);
    }
}

internal sealed class FakeEvidenceRepo : IExecutionEvidenceRepository
{
    public Dictionary<Guid, ExecutionEvidence> Data { get; } = new();

    public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        cancellationToken.ThrowIfCancellationRequested();
        Data[evidence.ExecutionId] = evidence;
        return Task.CompletedTask;
    }

    public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Data.TryGetValue(executionId, out var ev);
        return Task.FromResult<ExecutionEvidence?>(ev);
    }

    public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Data.Remove(executionId));
    }
}

internal sealed class FakeBundle
{
    public FakeReportRepo Reports { get; } = new();
    public FakeExecRepo Executions { get; } = new();
    public FakeAnomalyRepo Anomalies { get; } = new();
    public FakeCampaignRepo Campaigns { get; } = new();
    public FakeMutationCellRepo Cells { get; } = new();
}

internal sealed class FakeReportRepo : IReportRepository
{
    public List<Report> Data { get; } = new();
    public ObservableCollection<Report> GetAll() => new(Data);
    public Report? Get(Guid id) => Data.FirstOrDefault(r => r.IdReport == id);
    public ObservableCollection<Report> Get(Report e) => new(Data);
    public bool Add(Report e) { Data.Add(e); return true; }
    public bool Modify(Report e) => true;
    public bool Remove(Report e) => Data.Remove(e);
    public ObservableCollection<Report> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<Report> GetByScope(string s) => new(Data.Where(r => r.Scope == s).ToList());
    public ObservableCollection<Report> GetByDateRange(DateTime f, DateTime t)
        => new(Data.Where(r => r.GeneratedAt >= f && r.GeneratedAt < t).ToList());
}

internal sealed class FakeExecRepo : IExecutionRepository
{
    public List<Execution> Data { get; } = new();
    public ObservableCollection<Execution> GetAll() => new(Data);
    public Execution? Get(Guid id) => Data.FirstOrDefault(e => e.IdExecution == id);
    public ObservableCollection<Execution> Get(Execution e) => new(Data);
    public bool Add(Execution e) { Data.Add(e); return true; }
    public bool Modify(Execution e) => true;
    public bool Remove(Execution e) => Data.Remove(e);
    public ObservableCollection<Execution> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<Execution> GetByMRInstance(int id) => new();
    public ObservableCollection<Execution> GetByBatch(Guid id) => new();
    public ObservableCollection<Execution> GetByStatus(string s, int p, int sz) => new();
    public ObservableCollection<Execution> GetByDateRange(DateTime f, DateTime t) => new();
}

internal sealed class FakeAnomalyRepo : IAnomalyRepository
{
    public List<MetBench_Domain.Anomaly> Data { get; } = new();
    public ObservableCollection<MetBench_Domain.Anomaly> GetAll() => new(Data);
    public MetBench_Domain.Anomaly? Get(Guid id) => Data.FirstOrDefault(a => a.IdAnomaly == id);
    public ObservableCollection<MetBench_Domain.Anomaly> Get(MetBench_Domain.Anomaly e) => new(Data);
    public bool Add(MetBench_Domain.Anomaly e) { Data.Add(e); return true; }
    public bool Modify(MetBench_Domain.Anomaly e) => true;
    public bool Remove(MetBench_Domain.Anomaly e) => Data.Remove(e);
    public ObservableCollection<MetBench_Domain.Anomaly> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public MetBench_Domain.Anomaly? GetByResult(Guid id) => null;
    public ObservableCollection<MetBench_Domain.Anomaly> GetByStatus(MetBench_Domain.AnomalyStatus s) => new();
    public ObservableCollection<MetBench_Domain.Anomaly> GetByLinkedBug(int id) => new();
}

internal sealed class FakeCampaignRepo : IMutationCampaignRepository
{
    public List<MutationCampaign> Data { get; } = new();
    public ObservableCollection<MutationCampaign> GetAll() => new(Data);
    public MutationCampaign? Get(Guid id) => Data.FirstOrDefault(c => c.IdCampaign == id);
    public ObservableCollection<MutationCampaign> Get(MutationCampaign e) => new(Data);
    public bool Add(MutationCampaign e) { Data.Add(e); return true; }
    public bool Modify(MutationCampaign e) => true;
    public bool Remove(MutationCampaign e) => Data.Remove(e);
    public ObservableCollection<MutationCampaign> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<MutationCampaign> GetByStatus(string s) => new();
    public ObservableCollection<MutationCampaign> GetRecent(int n) => new();
}

internal sealed class FakeMutationCellRepo : IMutationResultRepository
{
    public List<MutationResult> Data { get; } = new();
    public ObservableCollection<MutationResult> GetAll() => new(Data);
    public MutationResult? Get(Guid id) => Data.FirstOrDefault(r => r.IdMutationResult == id);
    public ObservableCollection<MutationResult> Get(MutationResult e) => new(Data);
    public bool Add(MutationResult e) { Data.Add(e); return true; }
    public bool Modify(MutationResult e) => true;
    public bool Remove(MutationResult e) => Data.Remove(e);
    public ObservableCollection<MutationResult> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<MutationResult> GetByCampaign(Guid id) => new(Data.Where(r => r.CampaignId == id).ToList());
    public MutationResult? GetByMutantBinding(int m, int b) => null;
    public ObservableCollection<MutationResult> GetByOutcome(Guid id, string o) => new();
}
