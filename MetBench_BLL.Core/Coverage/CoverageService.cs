using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Coverage;

/// <summary>
/// 计算 4 维 coverage —— MetaPattern / SUT×MR / Bug / Mutation。
/// </summary>
/// <remarks>
/// 服务无状态、纯查询。落地不写库（dashboard 读、不写）。
/// 调用方拿 <see cref="CoverageReport"/> 后可序列化到 Reports 表当 JSON 快照。
/// </remarks>
public sealed class CoverageService
{
    /// <summary>NOETHER v1 的 8 个 MetaPattern 代码。</summary>
    public static readonly IReadOnlyList<string> AllMetaPatterns = new[]
    {
        "m_inv", "m_mono", "m_conv", "m_cmp",
        "m_adj", "m_rev", "m_dyn", "m_rel",
    };

    private readonly IMetamorphicRelationRepository _mrs;
    private readonly IMRBindingRepository _bindings;
    private readonly IApplicationRepository _apps;
    private readonly IKnownBugRepository _bugs;
    private readonly IAnomalyRepository _anomalies;
    private readonly IMutationResultRepository _mutationResults;

    public CoverageService(
        IMetamorphicRelationRepository mrs,
        IMRBindingRepository bindings,
        IApplicationRepository apps,
        IKnownBugRepository bugs,
        IAnomalyRepository anomalies,
        IMutationResultRepository mutationResults)
    {
        _mrs = mrs;
        _bindings = bindings;
        _apps = apps;
        _bugs = bugs;
        _anomalies = anomalies;
        _mutationResults = mutationResults;
    }

    public CoverageReport Compute()
    {
        return new CoverageReport(
            GeneratedAt: DateTime.UtcNow,
            MetaPattern: ComputeMetaPattern(),
            SutMr: ComputeSutMr(),
            Bug: ComputeBug(),
            Mutation: ComputeMutation());
    }

    internal MetaPatternCoverage ComputeMetaPattern()
    {
        var all = _mrs.GetAll();
        var coveredCodes = all
            .Select(m => m.MetaPatternCode)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();
        var uncoveredCodes = AllMetaPatterns
            .Where(p => !coveredCodes.Contains(p))
            .ToList();
        return new MetaPatternCoverage(
            TotalMetaPatterns: AllMetaPatterns.Count,
            CoveredMetaPatterns: coveredCodes.Count(c => AllMetaPatterns.Contains(c)),
            CoveredCodes: coveredCodes.Where(c => AllMetaPatterns.Contains(c)).OrderBy(c => c).ToList(),
            UncoveredCodes: uncoveredCodes);
    }

    internal SutMrCoverage ComputeSutMr()
    {
        var allApps = _apps.GetAll().ToList();
        var allMrs = _mrs.GetAll().ToList();
        var bindings = _bindings.GetAll().ToList();

        var bound = bindings
            .Select(b => (b.ApplicationId, b.MRId))
            .ToHashSet();

        var unbound = new List<UnboundCell>();
        foreach (var app in allApps)
        {
            foreach (var mr in allMrs)
            {
                if (!bound.Contains((app.IdApplication, mr.IdMR)))
                {
                    unbound.Add(new UnboundCell(app.IdApplication, app.Name ?? string.Empty,
                        mr.IdMR, mr.Code ?? string.Empty));
                }
            }
        }

        return new SutMrCoverage(
            TotalSuts: allApps.Count,
            TotalMRs: allMrs.Count,
            BoundCells: bound.Count,
            UnboundCells: unbound);
    }

    internal BugCoverage ComputeBug()
    {
        var allBugs = _bugs.GetAll().ToList();
        var anomalies = _anomalies.GetAll();
        var reproducedIds = anomalies
            .Where(a => a.LinkedKnownBugId.HasValue)
            .Select(a => a.LinkedKnownBugId!.Value)
            .Distinct()
            .ToHashSet();

        var reproducedCodes = allBugs
            .Where(b => reproducedIds.Contains(b.IdBug))
            .Select(b => b.Code)
            .OrderBy(c => c)
            .ToList();
        var notReproducedCodes = allBugs
            .Where(b => !reproducedIds.Contains(b.IdBug))
            .Select(b => b.Code)
            .OrderBy(c => c)
            .ToList();

        return new BugCoverage(
            TotalKnownBugs: allBugs.Count,
            ReproducedBugs: reproducedCodes.Count,
            ReproducedCodes: reproducedCodes,
            NotReproducedCodes: notReproducedCodes);
    }

    internal MutationCoverage ComputeMutation()
    {
        var results = _mutationResults.GetAll().ToList();
        if (results.Count == 0)
            return new MutationCoverage(0, 0, 0, 0);

        var byMutant = results
            .GroupBy(r => r.MutantId)
            .Select(g =>
            {
                var hasDetected = g.Any(x => x.Outcome == "detected");
                var hasMissed = g.Any(x => x.Outcome == "missed");
                return (MutantId: g.Key, Detected: hasDetected, Missed: hasMissed);
            })
            .ToList();

        var campaigns = results.Select(r => r.CampaignId).Distinct().Count();
        var totalMutants = byMutant.Count;
        var detected = byMutant.Count(x => x.Detected);
        var missed = byMutant.Count(x => !x.Detected && x.Missed);

        return new MutationCoverage(
            CampaignsSampled: campaigns,
            TotalMutants: totalMutants,
            DetectedMutants: detected,
            MissedMutants: missed);
    }
}
