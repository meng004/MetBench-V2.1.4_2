using MetBench_BLL.SystemMT.Persistence;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// 把一次 pipeline 执行的 <see cref="PipelineOutcome"/> 投影并持久化为
/// <see cref="Execution"/> + <see cref="Result"/>。
/// </summary>
/// <remarks>
/// v2 结果 schema 的统一写入口 —— launcher / replay / mutation / r-case 复现
/// 都应经此落库，消除各调用方各写一套的重复。计划见
/// docs/superpowers/plans/2026-05-22-systemmt-engine-unification-plan.md。
///
/// 落库规则：
///   • <see cref="Execution"/> 总写一行，<c>Status</c> = <c>outcome.FinalStatus</c>。
///   • <see cref="Result"/> 仅当 pipeline 跑到断言阶段（<c>AssertionResult</c> 非 null）
///     才写；error / timeout / cancelled 只有 Execution、无 Result。
///   • Anomaly 不在此创建 —— 异常调查工作流由 AnomalyService 负责（见计划 P4）。
/// </remarks>
public sealed class SystemMtExecutionRecorder
{
    private readonly IExecutionRepository _executions;
    private readonly IResultRepository _results;
    private readonly IExecutionEvidenceRepository? _evidence;
    private readonly IMetamorphicRelationV3Repository? _v3;

    public SystemMtExecutionRecorder(
        IExecutionRepository executions,
        IResultRepository results,
        IExecutionEvidenceRepository? evidence = null,
        IMetamorphicRelationV3Repository? v3 = null)
    {
        _executions = executions ?? throw new ArgumentNullException(nameof(executions));
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _evidence = evidence;
        _v3 = v3;
    }

    /// <summary>
    /// 持久化一次执行。返回新建行的 id；未写 Result 时 <c>ResultId</c> 为 null。
    /// </summary>
    /// <param name="context">本次执行的运行时上下文（提供版本三元组 + 触发者）。</param>
    /// <param name="outcome">pipeline 跑完返回的结果摘要。</param>
    /// <param name="mrInstanceId">→ MRInstances.IdInstance；无对应 instance 时调用方传哨兵值。</param>
    /// <param name="batchId">所属 Batch（单跑则 null）。</param>
    public RecordedExecution Record(
        PipelineContext context,
        PipelineOutcome outcome,
        int mrInstanceId,
        Guid? batchId = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(outcome);

        var executionId = Guid.NewGuid();
        _executions.Add(new Execution
        {
            IdExecution = executionId,
            MRInstanceId = mrInstanceId,
            BatchId = batchId,
            TriggeredBy = context.TriggeredBy,
            QueuedAt = outcome.StartedAt,
            StartedAt = outcome.StartedAt,
            FinishedAt = outcome.FinishedAt,
            Status = outcome.FinalStatus,
            CatalogVersionSha = context.CatalogVersionSha,
            SutVersionSnapshot = context.SutVersionSnapshot,
            MetbenchVersion = context.MetbenchVersion,
            ArtifactsDirectory = outcome.ArtifactsDirectory,
            ErrorMessage = outcome.ErrorMessage,
        });

        // Result 只在 pipeline 跑到断言阶段时存在；error/timeout/cancelled 无 Result。
        if (outcome.AssertionResult is not { } assertion)
        {
            return new RecordedExecution(executionId, null);
        }

        var resultId = Guid.NewGuid();
        _results.Add(new Result
        {
            IdResult = resultId,
            ExecutionId = executionId,
            SourceValue = assertion.SourceValue,
            FollowupValue = assertion.FollowupValue,
            SourceMetrics = ToMutable(outcome.SourceMetrics),
            FollowupMetrics = ToMutable(outcome.FollowupMetrics),
            AssertionPassed = assertion.Passed,
            AssertionExpression = assertion.Expression,
            ObservedDelta = assertion.ObservedDelta,
            ExpectedThreshold = assertion.ExpectedThreshold,
            FailureReason = assertion.FailureReason,
            SourceElapsed = outcome.SourceElapsed,
            FollowupElapsed = outcome.FollowupElapsed,
            SourceExitCode = outcome.SourceExitCode,
            FollowupExitCode = outcome.FollowupExitCode,
        });

        // Phase D (Task 6 step 2): write sample-level evidence alongside summary when both
        // the evidence repo and the V3 MR repo are injected. Sample traces are left empty
        // until Task 6 step 3 wires per-variable capture into SystemMtPipeline.
        if (_evidence is not null)
        {
            WriteEvidence(executionId, context, outcome);
        }

        return new RecordedExecution(executionId, resultId);
    }

    private void WriteEvidence(Guid executionId, PipelineContext context, PipelineOutcome outcome)
    {
        var v3 = _v3?.GetByCode(context.MrCode);
        var snapshot = new ExecutionMetadataSnapshot
        {
            MrId = context.MrCode,
            V3MrIdRef = v3?.IdV3 ?? Guid.Empty,
            SutName = context.SutName,
            Equation = v3?.Equation.ToString() ?? string.Empty,
            ProgramType = v3?.ProgramType.ToString() ?? string.Empty,
            MetaPattern = v3?.MetaPattern.ToString() ?? string.Empty,
            SourceLevel = v3?.SourceLevel.ToString() ?? string.Empty,
            FailureCorrelation = v3?.FailureCorrelation.ToString() ?? string.Empty,
            MetbenchVersion = context.MetbenchVersion,
        };

        var evidence = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = executionId,
            Metadata = snapshot,
            SampleTraces = new(),               // Task 6 step 3 lands per-variable triples
            TransformationParameters = new Dictionary<string, string>(context.Parameters),
            RecordedAtUtc = outcome.FinishedAt.ToUniversalTime(),
        };
        // Recorder is sync; evidence write also runs sync (LiteDB).
        _evidence!.SaveAsync(evidence).GetAwaiter().GetResult();
    }

    private static Dictionary<string, double> ToMutable(
        IReadOnlyDictionary<string, double>? metrics)
        => metrics is null
            ? new Dictionary<string, double>()
            : new Dictionary<string, double>(metrics);
}

/// <summary>一次 <see cref="SystemMtExecutionRecorder.Record"/> 写入的行 id。</summary>
public sealed record RecordedExecution(Guid ExecutionId, Guid? ResultId);
