using MetBench_BLL.Discovery.Validators;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery;

/// <summary>
/// Validation orchestrator —— 给一个 CandidateMR 跑多个 IMRValidator，
/// 写 ValidationRun 行，必要时自动 promote。
/// </summary>
public sealed class ValidationService
{
    private readonly ICandidateMRRepository _candidates;
    private readonly IValidationRunRepository _validationRuns;
    private readonly IMetamorphicRelationRepository _mrRepo;
    private readonly IAuditLogRepository _auditLog;

    /// <summary>≥ 几个 validator 通过才算 promote 候选（默认 2）。</summary>
    public int PromotionThreshold { get; init; } = 2;

    public ValidationService(
        ICandidateMRRepository candidates,
        IValidationRunRepository validationRuns,
        IMetamorphicRelationRepository mrRepo,
        IAuditLogRepository auditLog)
    {
        _candidates = candidates;
        _validationRuns = validationRuns;
        _mrRepo = mrRepo;
        _auditLog = auditLog;
    }

    /// <summary>
    /// 在 <paramref name="candidateId"/> 上跑一批 validator → 写 ValidationRun + 必要时 promote。
    /// </summary>
    public async Task<ValidationSummary> ValidateAsync(
        Guid candidateId, IReadOnlyList<IMRValidator> validators,
        string actor, CancellationToken ct = default)
    {
        var candidate = _candidates.Get(candidateId)
            ?? throw new InvalidOperationException($"Candidate {candidateId} not found");

        var outcomes = new List<(string ValidatorName, ValidationOutcome Outcome)>();
        foreach (var v in validators)
        {
            ct.ThrowIfCancellationRequested();
            ValidationOutcome outcome;
            try
            {
                outcome = await v.ValidateAsync(candidate, ct);
            }
            catch (Exception ex)
            {
                outcome = new ValidationOutcome(false, $"{{\"reason\":\"validator-threw\",\"message\":\"{ex.Message.Replace("\"", "\\\"")}\"}}");
            }

            _validationRuns.Add(new ValidationRun
            {
                IdValidation = Guid.NewGuid(),
                CandidateMRId = candidateId,
                ValidatorName = v.Name,
                RunAt = DateTime.UtcNow,
                Passed = outcome.Passed,
                DetailsJson = outcome.DetailsJson,
            });
            outcomes.Add((v.Name, outcome));
        }

        var passedCount = _validationRuns.CountPassedValidators(candidateId);
        var promoted = false;
        int? promotedMRId = null;
        if (passedCount >= PromotionThreshold && candidate.Status != "promoted")
        {
            promoted = TryPromote(candidate, actor, out promotedMRId);
        }
        else if (candidate.Status == "pending" && outcomes.Any(o => o.Outcome.Passed))
        {
            candidate.Status = "validated";
            _candidates.Modify(candidate);
        }

        return new ValidationSummary(
            CandidateId: candidateId,
            PassedValidators: passedCount,
            TotalValidatorsRun: outcomes.Count,
            Promoted: promoted,
            PromotedMRId: promotedMRId,
            Outcomes: outcomes);
    }

    private bool TryPromote(CandidateMR candidate, string actor, out int? newMRId)
    {
        var mr = new MetamorphicRelation
        {
            Code = candidate.ProposedCode,
            Description = candidate.ProposedName,
            MetaPatternCode = "",
            AssertionTypeCode = candidate.SuggestedAssertionTypeCode ?? "",
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
        var ok = _mrRepo.Add(mr);
        if (!ok)
        {
            newMRId = null;
            return false;
        }
        candidate.Status = "promoted";
        candidate.PromotedToMRId = mr.IdMR;
        _candidates.Modify(candidate);
        newMRId = mr.IdMR;

        _auditLog.Add(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = "candidate.promote",
            TargetEntityType = "CandidateMR",
            TargetEntityId = candidate.IdCandidate.ToString(),
            DetailsJson = $"{{\"newMRId\":{mr.IdMR},\"code\":\"{candidate.ProposedCode}\"}}",
        });
        return true;
    }
}

/// <summary>ValidationService 的返回摘要。</summary>
public sealed record ValidationSummary(
    Guid CandidateId,
    int PassedValidators,
    int TotalValidatorsRun,
    bool Promoted,
    int? PromotedMRId,
    IReadOnlyList<(string ValidatorName, ValidationOutcome Outcome)> Outcomes);
