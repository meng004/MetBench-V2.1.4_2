using MetBench_BLL.Coverage;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Coverage;

public sealed class CoverageServiceTests
{
    [Fact]
    public void ComputeMetaPattern_only_counts_known_codes()
    {
        var (svc, fakes) = MakeService();
        fakes.Mrs.Add(NewMR("MR-T", "m_mono"));
        fakes.Mrs.Add(NewMR("MR-S", "m_inv"));
        fakes.Mrs.Add(NewMR("MR-X", "")); // empty — ignored
        fakes.Mrs.Add(NewMR("MR-Y", "made-up-code")); // unknown — ignored

        var mp = svc.ComputeMetaPattern();

        Assert.Equal(CoverageService.AllMetaPatterns.Count, mp.TotalMetaPatterns);
        Assert.Equal(2, mp.CoveredMetaPatterns);
        Assert.Contains("m_mono", mp.CoveredCodes);
        Assert.Contains("m_inv", mp.CoveredCodes);
        Assert.DoesNotContain("made-up-code", mp.CoveredCodes);
        Assert.Equal(2.0 / mp.TotalMetaPatterns, mp.Ratio, 3);
    }

    [Fact]
    public void ComputeSutMr_lists_unbound_cells()
    {
        var (svc, fakes) = MakeService();
        fakes.Apps.Add(NewApp(1, "openmoc"));
        fakes.Apps.Add(NewApp(2, "openmc"));
        fakes.Mrs.Add(NewMR("MR-T", "m_mono"));
        fakes.Mrs.Add(NewMR("MR-S", "m_inv"));
        // bind (openmoc, MR-T) only
        fakes.Bindings.Add(new MRBinding { IdMRBinding = 1, MRId = 1, ApplicationId = 1 });

        var sm = svc.ComputeSutMr();

        Assert.Equal(2, sm.TotalSuts);
        Assert.Equal(2, sm.TotalMRs);
        Assert.Equal(1, sm.BoundCells);
        Assert.Equal(4, sm.PossibleCells);
        Assert.Equal(3, sm.UnboundCells.Count);
        Assert.Equal(0.25, sm.Density, 3);
    }

    [Fact]
    public void ComputeBug_anomaly_linked_marks_reproduced()
    {
        var (svc, fakes) = MakeService();
        fakes.Bugs.Data.Add(new KnownBug { IdBug = 1, Code = "R-Case-1" });
        fakes.Bugs.Data.Add(new KnownBug { IdBug = 2, Code = "R-Case-2" });
        fakes.Bugs.Data.Add(new KnownBug { IdBug = 3, Code = "R-Case-3" });
        fakes.Anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), LinkedKnownBugId = 1 });
        fakes.Anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), LinkedKnownBugId = 1 }); // dup ok
        fakes.Anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), LinkedKnownBugId = 2 });

        var bc = svc.ComputeBug();

        Assert.Equal(3, bc.TotalKnownBugs);
        Assert.Equal(2, bc.ReproducedBugs);
        Assert.Contains("R-Case-1", bc.ReproducedCodes);
        Assert.Contains("R-Case-3", bc.NotReproducedCodes);
        Assert.Equal(2.0 / 3.0, bc.Ratio, 3);
    }

    [Fact]
    public void ComputeMutation_aggregates_by_mutant()
    {
        var (svc, fakes) = MakeService();
        var c1 = Guid.NewGuid();
        // mutant 1: detected in c1, missed in c2 → still counts as detected
        fakes.MutResults.Add(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = c1, MutantId = 1, MRBindingId = 1, Outcome = "detected" });
        fakes.MutResults.Add(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = c1, MutantId = 1, MRBindingId = 2, Outcome = "missed" });
        // mutant 2: only missed → counts as missed
        fakes.MutResults.Add(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = c1, MutantId = 2, MRBindingId = 1, Outcome = "missed" });
        // mutant 3: only not-affected → counts as neither
        fakes.MutResults.Add(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = c1, MutantId = 3, MRBindingId = 1, Outcome = "not-affected" });

        var mc = svc.ComputeMutation();

        Assert.Equal(1, mc.CampaignsSampled);
        Assert.Equal(3, mc.TotalMutants);
        Assert.Equal(1, mc.DetectedMutants);
        Assert.Equal(1, mc.MissedMutants);
        Assert.Equal(0.5, mc.DetectionRate, 3);
    }

    [Fact]
    public void Compute_returns_full_report()
    {
        var (svc, _) = MakeService();
        var report = svc.Compute();
        Assert.NotNull(report.MetaPattern);
        Assert.NotNull(report.SutMr);
        Assert.NotNull(report.Bug);
        Assert.NotNull(report.Mutation);
    }

    // ===== fixtures =====

    private static (CoverageService, FakeBundle) MakeService()
    {
        var fakes = new FakeBundle();
        var svc = new CoverageService(fakes.Mrs, fakes.Bindings, fakes.Apps,
            fakes.Bugs, fakes.Anomalies, fakes.MutResults);
        return (svc, fakes);
    }

    private static MetamorphicRelation NewMR(string code, string metaPattern) => new()
    {
        Code = code,
        MetaPatternCode = metaPattern,
        Description = code,
        Context = "",
        Constraint = "",
        OrderOfMR = "",
        InputPattern = "",
        OutputPattern = "",
        InputPatterntosympy = "",
        OutputPatterntosympy = "",
        DimensionOfInputPattern = "",
        DimensionOfOutputPattern = "",
        Granularity = "",
        Hierarchy = "",
        Operator = "",
        Expression = "",
        InputPatternImageData = Array.Empty<byte>(),
        OutputPatternImageData = Array.Empty<byte>(),
    };

    private static Application NewApp(int id, string name) => new()
    {
        IdApplication = id,
        Name = name,
        CodeName = name,
        SourceTestCaseName = "",
        DomainName = "",
    };
}

internal sealed class FakeBundle
{
    public FakeMRRepository Mrs { get; } = new();
    public FakeMRBindingRepository Bindings { get; } = new();
    public FakeApplicationRepository Apps { get; } = new();
    public FakeKnownBugRepository Bugs { get; } = new();
    public FakeAnomalyRepo Anomalies { get; } = new();
    public FakeMutationResultRepo MutResults { get; } = new();
}
