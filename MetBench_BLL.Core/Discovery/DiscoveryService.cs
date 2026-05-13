using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery;

/// <summary>
/// Discovery orchestrator —— 调 <see cref="IMRDiscoverer"/>，把结果落库（DiscoveryRun + CandidateMR）。
/// </summary>
public sealed class DiscoveryService
{
    private readonly IDiscoveryMethodRepository _methods;
    private readonly IDiscoveryRunRepository _runs;
    private readonly ICandidateMRRepository _candidates;
    private readonly IAuditLogRepository _auditLog;

    public DiscoveryService(
        IDiscoveryMethodRepository methods,
        IDiscoveryRunRepository runs,
        ICandidateMRRepository candidates,
        IAuditLogRepository auditLog)
    {
        _methods = methods;
        _runs = runs;
        _candidates = candidates;
        _auditLog = auditLog;
    }

    /// <summary>
    /// 跑一次 discovery，返回新建的 DiscoveryRun + 写入的候选 Guid 列表。
    /// </summary>
    public async Task<DiscoveryExecutionResult> RunAsync(
        IMRDiscoverer discoverer, int methodId, int? targetApplicationId,
        string actor, CancellationToken ct = default)
    {
        var run = new DiscoveryRun
        {
            IdRun = Guid.NewGuid(),
            MethodId = methodId,
            TargetApplicationId = targetApplicationId,
            StartedAt = DateTime.UtcNow,
            Status = "running",
        };
        _runs.Add(run);

        DiscoveryRunOutcome outcome;
        try
        {
            outcome = await discoverer.DiscoverAsync(targetApplicationId, ct);
        }
        catch (Exception ex)
        {
            outcome = new DiscoveryRunOutcome("error", null,
                Array.Empty<CandidateMrProposal>(), ex.Message);
        }

        var candidateIds = new List<Guid>();
        foreach (var p in outcome.Proposals)
        {
            var candidate = new CandidateMR
            {
                IdCandidate = Guid.NewGuid(),
                DiscoveryRunId = run.IdRun,
                ProposedCode = p.ProposedCode,
                ProposedName = p.ProposedName,
                SuggestedTransformationName = p.SuggestedTransformationName,
                SuggestedAssertionTypeCode = p.SuggestedAssertionTypeCode,
                ProposedValueName = p.ProposedValueName,
                Rationale = p.Rationale,
                Confidence = p.Confidence,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
            };
            _candidates.Add(candidate);
            candidateIds.Add(candidate.IdCandidate);
        }

        run.FinishedAt = DateTime.UtcNow;
        run.Status = outcome.Status;
        run.InvocationDetails = outcome.InvocationDetails;
        run.CandidatesProduced = outcome.Proposals.Count;
        _runs.Modify(run);

        _auditLog.Add(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = "discovery.run",
            TargetEntityType = "DiscoveryRun",
            TargetEntityId = run.IdRun.ToString(),
            DetailsJson = $"{{\"method\":\"{discoverer.Name}\",\"candidates\":{outcome.Proposals.Count}}}",
        });

        return new DiscoveryExecutionResult(run.IdRun, candidateIds, outcome);
    }
}

/// <summary>DiscoveryService.RunAsync 的返回值。</summary>
public sealed record DiscoveryExecutionResult(
    Guid DiscoveryRunId,
    IReadOnlyList<Guid> CreatedCandidateIds,
    DiscoveryRunOutcome RawOutcome);
