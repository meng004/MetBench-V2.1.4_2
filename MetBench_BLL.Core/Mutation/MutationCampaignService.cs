using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Mutation;

/// <summary>
/// MutationCampaign 编排器 —— 跑 (mutants × MRBindings × sample cases) 三维矩阵。
/// </summary>
/// <remarks>
/// <para>
/// <b>T6 maturity status (2026-06-06): Prototype.</b> The orchestration in this class is
/// real and unit-tested, but the production <see cref="MutationCellRunner"/> wired in WPF
/// (<c>MutationCampaignViewModel.StubCellRunner</c>) is a hash-based simulator that never
/// touches a real SUT — meaning live kill-rate / detection-rate statistics produced by
/// the running app are simulated, not measured. The minimum viable path to a Functional
/// T6 is documented in
/// <c>docs/superpowers/plans/2026-06-06-metbench-maturity-remediation-plan.md §5</c>:
/// implement <see cref="IMutantApplicator"/> (in this PR), then a launcher-backed cellRunner
/// that materializes a patched SUT via the applicator and calls
/// <c>ISystemMtLauncher.RunAsync</c> against it (deferred follow-up — requires a per-run
/// SUT-root override on <c>LauncherOptions</c>).
/// </para>
///
/// 落库：
/// <list type="bullet">
///   <item><c>MutationCampaign</c> 行（status: running → ok / failed）</item>
///   <item>每个 cell 一条 <c>MutationResult</c>（outcome: detected / missed / error / not-affected）</item>
/// </list>
/// 实际 "跑一个 cell" 由 <see cref="MutationCellRunner"/> 委托执行 —— 让 cloud / VM 注入
/// 不同强度的实现（cloud 端 fake / VM 端真实 ISystemMtPipeline）。
/// </remarks>
public sealed class MutationCampaignService
{
    private readonly IMutationCampaignRepository _campaigns;
    private readonly IMutationResultRepository _results;
    private readonly IAuditLogRepository _auditLog;

    public MutationCampaignService(
        IMutationCampaignRepository campaigns,
        IMutationResultRepository results,
        IAuditLogRepository auditLog)
    {
        _campaigns = campaigns;
        _results = results;
        _auditLog = auditLog;
    }

    /// <summary>
    /// 启动一次 Campaign，串行跑全部 cell。
    /// </summary>
    public async Task<MutationCampaignSummary> RunAsync(
        MutationCampaignSpec spec,
        MutationCellRunner cellRunner,
        string actor,
        IProgress<MutationCellProgress>? progress = null,
        CancellationToken ct = default)
    {
        var campaign = new MutationCampaign
        {
            IdCampaign = Guid.NewGuid(),
            Name = spec.Name,
            ScopeSpecJson = SerializeScope(spec),
            CatalogVersionSha = spec.CatalogVersionSha,
            StartedAt = DateTime.UtcNow,
            Status = "running",
            CreatedBy = spec.CreatedBy,
        };
        _campaigns.Add(campaign);

        int detected = 0, missed = 0, errors = 0, notAffected = 0, total = 0;
        try
        {
            var samples = spec.SampleCasePaths.Count == 0
                ? new[] { string.Empty }
                : spec.SampleCasePaths.ToArray();

            foreach (var mutantId in spec.MutantIds)
            foreach (var bindingId in spec.MRBindingIds)
            foreach (var samplePath in samples)
            {
                ct.ThrowIfCancellationRequested();
                total++;
                MutationCellOutcome outcome;
                try
                {
                    outcome = await cellRunner(new MutationCellInput(
                        CampaignId: campaign.IdCampaign,
                        MutantId: mutantId,
                        MRBindingId: bindingId,
                        SampleCasePath: samplePath), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // cell-level cancel == campaign cancel; let outer handler mark "cancelled"
                    throw;
                }
                catch (Exception ex)
                {
                    outcome = new MutationCellOutcome(Guid.Empty, "error",
                        ObservedDelta: null, ExpectedDelta: null, Notes: ex.Message);
                }

                _results.Add(new MutationResult
                {
                    IdMutationResult = Guid.NewGuid(),
                    CampaignId = campaign.IdCampaign,
                    MutantId = mutantId,
                    MRBindingId = bindingId,
                    ExecutionId = outcome.ExecutionId,
                    Outcome = outcome.Outcome,
                    ObservedDelta = outcome.ObservedDelta,
                    ExpectedDelta = outcome.ExpectedDelta,
                    Notes = outcome.Notes,
                });

                switch (outcome.Outcome)
                {
                    case "detected": detected++; break;
                    case "missed": missed++; break;
                    case "error": errors++; break;
                    case "not-affected": notAffected++; break;
                }

                progress?.Report(new MutationCellProgress(
                    Total: total, Detected: detected, Missed: missed,
                    Errors: errors, NotAffected: notAffected,
                    LastMutantId: mutantId, LastBindingId: bindingId));
            }

            campaign.Status = "ok";
        }
        catch (OperationCanceledException)
        {
            campaign.Status = "cancelled";
        }
        catch
        {
            campaign.Status = "failed";
            throw;
        }
        finally
        {
            campaign.FinishedAt = DateTime.UtcNow;
            _campaigns.Modify(campaign);

            _auditLog.Add(new AuditLog
            {
                IdLog = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Actor = actor,
                Action = "mutation-campaign.run",
                TargetEntityType = "MutationCampaign",
                TargetEntityId = campaign.IdCampaign.ToString(),
                DetailsJson = $"{{\"name\":\"{spec.Name}\",\"total\":{total},\"status\":\"{campaign.Status}\"}}",
            });
        }

        return new MutationCampaignSummary(
            CampaignId: campaign.IdCampaign,
            TotalCells: total,
            Detected: detected,
            Missed: missed,
            Errors: errors,
            NotAffected: notAffected);
    }

    private static string SerializeScope(MutationCampaignSpec spec)
    {
        var mutants = string.Join(",", spec.MutantIds);
        var bindings = string.Join(",", spec.MRBindingIds);
        var samples = string.Join(",", spec.SampleCasePaths.Select(s => $"\"{s.Replace("\\", "/")}\""));
        return $"{{\"mutants\":[{mutants}],\"mrBindings\":[{bindings}],\"sampleCases\":[{samples}]}}";
    }
}

/// <summary>"跑一个 cell" 的委托 —— campaign service 注入。</summary>
public delegate Task<MutationCellOutcome> MutationCellRunner(MutationCellInput input, CancellationToken ct);

/// <summary>"跑一个 cell" 的入参。</summary>
public sealed record MutationCellInput(
    Guid CampaignId,
    int MutantId,
    int MRBindingId,
    string SampleCasePath);

/// <summary>"跑一个 cell" 的出参。</summary>
public sealed record MutationCellOutcome(
    Guid ExecutionId,
    string Outcome,
    double? ObservedDelta,
    double? ExpectedDelta,
    string? Notes);

/// <summary>进度上报：累计计数 + 最近一格的 (mutant, binding)。</summary>
public sealed record MutationCellProgress(
    int Total, int Detected, int Missed, int Errors, int NotAffected,
    int LastMutantId, int LastBindingId);
