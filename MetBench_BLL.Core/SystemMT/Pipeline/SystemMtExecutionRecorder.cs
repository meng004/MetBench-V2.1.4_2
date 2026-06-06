using MetBench_BLL.SystemMT.Catalog.Typed.Property;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_Domain;
using MetBench_IDAL;
using System.IO;
using System.Text.Json;
using TypedPropertyResult = MetBench_BLL.SystemMT.Catalog.Typed.Property.PropertyResult;

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
///   • Legacy <c>SystemMtResults</c> 镜像：当构造时注入 <see cref="ISystemMtResultRepository"/>
///     时，会在 V2 <c>Result</c> 写入后镜像一份 <see cref="SystemMtResultRecord"/> 到 legacy
///     集合，<c>Id == executionId</c>（即 V2 <see cref="Execution.IdExecution"/>），
///     保证 <c>IExecutionHistoryEditor.DeleteAsync</c> 跨集合删除 join 工作。镜像走
///     同样的 <c>AssertionResult-非 null</c> 门，与 V2 <c>Result</c> 写入策略一致。
///     PR-4 (#224) 加的此路径由 <c>LegacyResultMirrorTests</c> (4 facts) 守护：
///     Id == ExecutionId / 无注入跳镜像 / 无 assertion 跳镜像 / 失败 reason 透传。
/// </remarks>
public sealed class SystemMtExecutionRecorder
{
    private readonly IExecutionRepository _executions;
    private readonly IResultRepository _results;
    private readonly IExecutionEvidenceRepository? _evidence;
    private readonly IMetamorphicRelationV3Repository? _v3;
    private readonly ISystemMtResultRepository? _legacyResults;

    public SystemMtExecutionRecorder(
        IExecutionRepository executions,
        IResultRepository results,
        IExecutionEvidenceRepository? evidence = null,
        IMetamorphicRelationV3Repository? v3 = null,
        ISystemMtResultRepository? legacyResults = null)
    {
        _executions = executions ?? throw new ArgumentNullException(nameof(executions));
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _evidence = evidence;
        _v3 = v3;
        _legacyResults = legacyResults;
    }

    /// <summary>
    /// 持久化一次执行。返回新建行的 id；未写 Result 时 <c>ResultId</c> 为 null。
    /// </summary>
    /// <param name="context">本次执行的运行时上下文（提供版本三元组 + 触发者）。</param>
    /// <param name="outcome">pipeline 跑完返回的结果摘要。</param>
    /// <param name="mrInstanceId">→ MRInstances.IdInstance；无对应 instance 时调用方传哨兵值。</param>
    /// <param name="batchId">所属 Batch（单跑则 null）。</param>
    public async Task<RecordedExecution> RecordAsync(
        PipelineContext context,
        PipelineOutcome outcome,
        int mrInstanceId,
        Guid? batchId = null,
        VerificationResult? typedVerification = null,
        TypedPropertyResult? typedProperty = null,
        MrSpec? typedSpec = null,
        PredicateSpec? typedPredicate = null,
        PropertySpec? typedPropertySpec = null,
        RuntimeEvidence? runtimeEvidence = null,
        CancellationToken cancellationToken = default)
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
            if (_evidence is not null && runtimeEvidence is not null)
            {
                await WriteEvidenceAsync(
                    executionId,
                    context,
                    outcome,
                    runtimeEvidence: runtimeEvidence,
                    cancellationToken: cancellationToken);
            }

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

        // Phase D (Task 6): write sample-level evidence alongside summary when the evidence
        // repo (and, for typed projection, the V3 MR repo) are injected. BuildSampleTraces
        // captures the declared target field plus every other input leaf the MR transformation
        // changed (diffed from the source vs follow-up input JSON).
        if (_evidence is not null)
        {
            await WriteEvidenceAsync(
                executionId,
                context,
                outcome,
                typedVerification,
                typedProperty,
                typedSpec,
                typedPredicate,
                typedPropertySpec,
                runtimeEvidence,
                cancellationToken);
        }

        // Legacy SystemMtResults mirror — populates the collection
        // IExecutionHistoryEditor.ListPagedAsync reads so the WPF Execution
        // History page sees live runs. Id is set to executionId (v2
        // Execution.IdExecution) so DeleteAsync(executionId) joins cleanly
        // across Result + Evidence + legacy collections.
        if (_legacyResults is not null)
        {
            var legacyRecord = new SystemMtResultRecord
            {
                Id = executionId,
                MrName = context.MrCode,
                RunAt = outcome.FinishedAt,
                AssertionName = assertion.AssertionTypeCode,
                ValueName = context.ValueName,
                SourceValue = assertion.SourceValue ?? 0,
                FollowUpValue = assertion.FollowupValue ?? 0,
                Passed = assertion.Passed,
                FailureReason = assertion.FailureReason ?? string.Empty,
                SourceCaseName = Path.GetFileName(context.SourceCasePath ?? string.Empty),
                FollowUpCaseName = Path.GetFileName(outcome.FollowupInputPath ?? string.Empty),
                SourceElapsed = outcome.SourceElapsed,
                FollowUpElapsed = outcome.FollowupElapsed,
                SourceExitCode = outcome.SourceExitCode,
                FollowUpExitCode = outcome.FollowupExitCode,
                SourceMetrics = ToMutable(outcome.SourceMetrics),
                FollowUpMetrics = ToMutable(outcome.FollowupMetrics),
                TransformationName = context.TransformationName,
                TransformationParameters = new Dictionary<string, string>(context.Parameters),
            };
            await _legacyResults.SaveAsync(legacyRecord, cancellationToken);
        }

        return new RecordedExecution(executionId, resultId);
    }

    public async Task<RecordedExecution> RecordBlockedPreflightAsync(
        PipelineContext context,
        RuntimePreflightResult preflight,
        int mrInstanceId,
        Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(preflight);
        if (preflight.Passed)
            throw new ArgumentException("Blocked preflight recorder path requires a failed preflight result.", nameof(preflight));

        var executionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var errorMessage = $"Runtime preflight failed: {preflight.Detail}";

        _executions.Add(new Execution
        {
            IdExecution = executionId,
            MRInstanceId = mrInstanceId,
            BatchId = batchId,
            TriggeredBy = context.TriggeredBy,
            QueuedAt = now,
            StartedAt = now,
            FinishedAt = now,
            Status = PipelineStatus.Error,
            CatalogVersionSha = context.CatalogVersionSha,
            SutVersionSnapshot = context.SutVersionSnapshot,
            MetbenchVersion = context.MetbenchVersion,
            ArtifactsDirectory = context.WorkingDirectory,
            ErrorMessage = errorMessage,
        });

        if (_evidence is not null)
        {
            var outcome = new PipelineOutcome(
                FinalStatus: PipelineStatus.Error,
                ErrorMessage: errorMessage,
                StartedAt: now,
                FinishedAt: now,
                ArtifactsDirectory: context.WorkingDirectory,
                SourceInputPath: context.SourceCasePath,
                FollowupInputPath: string.Empty,
                SourceOutputPath: string.Empty,
                FollowupOutputPath: string.Empty,
                SourceMetrics: null,
                FollowupMetrics: null,
                AssertionResult: null,
                SourceElapsed: TimeSpan.Zero,
                FollowupElapsed: TimeSpan.Zero,
                SourceExitCode: 0,
                FollowupExitCode: 0);

            await WriteEvidenceAsync(
                executionId,
                context,
                outcome,
                runtimeEvidence: RuntimeEvidence.FromPreflightResult(preflight),
                cancellationToken: cancellationToken);
        }

        return new RecordedExecution(executionId, null);
    }

    private async Task WriteEvidenceAsync(
        Guid executionId,
        PipelineContext context,
        PipelineOutcome outcome,
        VerificationResult? typedVerification = null,
        TypedPropertyResult? typedProperty = null,
        MrSpec? typedSpec = null,
        PredicateSpec? typedPredicate = null,
        PropertySpec? typedPropertySpec = null,
        RuntimeEvidence? runtimeEvidence = null,
        CancellationToken cancellationToken = default)
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
            SampleTraces = BuildSampleTraces(context, outcome),
            TransformationParameters = new Dictionary<string, string>(context.Parameters),
            RecordedAtUtc = outcome.FinishedAt.ToUniversalTime(),
            RuntimeEvidence = runtimeEvidence,
        };

        // ExecutionEvidence v2: project typed verifier output. Precedence is
        // explicit Record(...) parameters first (used by unit tests that want
        // to inject a hand-crafted triple), then fall back to the typed
        // triple the live SystemMtPipeline attached to PipelineOutcome (PR-123).
        // Property results have no PipelineOutcome carrier today and stay on
        // the explicit-parameter path; absence means TypedVerification null.
        var effectiveVerification = typedVerification ?? outcome.TypedVerification;
        var effectiveSpec = typedSpec ?? outcome.TypedSpec;
        var effectivePredicate = typedPredicate ?? outcome.TypedPredicate;

        if (effectiveVerification is not null && effectiveSpec is not null && effectivePredicate is not null)
        {
            evidence.TypedVerification = TypedVerificationEvidenceMapper
                .FromVerificationResult(effectiveSpec, effectivePredicate, effectiveVerification);
            evidence.PairQuality = PairQualitySummary.FromVerificationResult(
                effectivePredicate,
                effectiveVerification,
                RoleOutputsProduced(outcome));
        }
        else if (typedProperty is not null && typedPropertySpec is not null)
        {
            evidence.TypedVerification = TypedVerificationEvidenceMapper
                .FromPropertyResult(typedPropertySpec, typedProperty);
        }

        await _evidence!.SaveAsync(evidence, cancellationToken);
    }

    private static bool RoleOutputsProduced(PipelineOutcome outcome)
    {
        if (outcome.PhaseMetrics is not null)
        {
            return outcome.PhaseMetrics.Count > 0;
        }

        return outcome.SourceMetrics is not null && outcome.FollowupMetrics is not null;
    }

    private static List<ExecutionSampleTrace> BuildSampleTraces(PipelineContext context, PipelineOutcome outcome)
    {
        var traces = new List<ExecutionSampleTrace>();

        if (string.IsNullOrWhiteSpace(context.TargetFieldPath)
            || string.IsNullOrWhiteSpace(context.SourceCasePath)
            || string.IsNullOrWhiteSpace(outcome.FollowupInputPath)
            || !File.Exists(context.SourceCasePath)
            || !File.Exists(outcome.FollowupInputPath))
        {
            return traces;
        }

        // Parse each input file exactly once; the declared target field and the
        // changed-field diff below both read from the same parsed documents.
        using var sourceDoc = JsonDocument.Parse(File.ReadAllText(context.SourceCasePath));
        using var followupDoc = JsonDocument.Parse(File.ReadAllText(outcome.FollowupInputPath));

        if (!TryResolveJsonPointer(sourceDoc.RootElement, context.TargetFieldPath, out var sourceTarget)
            || !TryResolveJsonPointer(followupDoc.RootElement, context.TargetFieldPath, out var followupTarget))
        {
            return traces;
        }

        traces.Add(new ExecutionSampleTrace
        {
            VariableName = context.ValueName,
            Path = context.TargetFieldPath,
            SourceValueJson = sourceTarget.GetRawText(),
            TransformedValueJson = followupTarget.GetRawText(),
            OutputValueJson = MetricJson(outcome, context.ValueName),
        });

        // Task 6 granularity: beyond the single declared target field, capture an honest
        // (source, transformed, output) triple for every other input leaf that the MR
        // transformation actually changed — diffed from the same parsed documents.
        AppendChangedFieldTraces(traces, context, outcome, sourceDoc.RootElement, followupDoc.RootElement);
        return traces;
    }

    private static string MetricJson(PipelineOutcome outcome, string variableName) =>
        outcome.FollowupMetrics is not null && outcome.FollowupMetrics.TryGetValue(variableName, out var metric)
            ? JsonSerializer.Serialize(metric)
            : string.Empty;

    private static void AppendChangedFieldTraces(
        List<ExecutionSampleTrace> traces,
        PipelineContext context,
        PipelineOutcome outcome,
        JsonElement sourceRoot,
        JsonElement followupRoot)
    {
        var sourceLeaves = EnumerateJsonLeaves(sourceRoot);
        var followupLeaves = EnumerateJsonLeaves(followupRoot);

        var targetPointer = context.TargetFieldPath ?? string.Empty;
        var pointers = new SortedSet<string>(StringComparer.Ordinal);
        pointers.UnionWith(sourceLeaves.Keys);
        pointers.UnionWith(followupLeaves.Keys);

        foreach (var pointer in pointers)
        {
            if (string.Equals(pointer, targetPointer, StringComparison.Ordinal))
                continue;

            sourceLeaves.TryGetValue(pointer, out var src);
            followupLeaves.TryGetValue(pointer, out var followup);
            if (string.Equals(src, followup, StringComparison.Ordinal))
                continue;

            var variableName = JsonPointerLastSegment(pointer);
            traces.Add(new ExecutionSampleTrace
            {
                VariableName = variableName,
                Path = pointer,
                SourceValueJson = src ?? string.Empty,
                TransformedValueJson = followup ?? string.Empty,
                OutputValueJson = MetricJson(outcome, variableName),
            });
        }
    }

    private static Dictionary<string, string> EnumerateJsonLeaves(JsonElement root)
    {
        var sink = new Dictionary<string, string>(StringComparer.Ordinal);
        EnumerateJsonLeaves(root, string.Empty, sink);
        return sink;
    }

    private static void EnumerateJsonLeaves(JsonElement element, string prefix, IDictionary<string, string> sink)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var hasProperties = false;
                foreach (var property in element.EnumerateObject())
                {
                    hasProperties = true;
                    EnumerateJsonLeaves(
                        property.Value,
                        $"{prefix}/{EncodePointerSegment(property.Name)}",
                        sink);
                }
                if (!hasProperties && prefix.Length > 0)
                    sink[prefix] = element.GetRawText();
                break;
            case JsonValueKind.Array:
                var index = 0;
                var hasItems = false;
                foreach (var item in element.EnumerateArray())
                {
                    hasItems = true;
                    EnumerateJsonLeaves(item, $"{prefix}/{index}", sink);
                    index++;
                }
                if (!hasItems && prefix.Length > 0)
                    sink[prefix] = element.GetRawText();
                break;
            default:
                if (prefix.Length > 0)
                    sink[prefix] = element.GetRawText();
                break;
        }
    }

    private static string JsonPointerLastSegment(string pointer)
    {
        var index = pointer.LastIndexOf('/');
        var segment = index >= 0 ? pointer[(index + 1)..] : pointer;
        return DecodePointerSegment(segment);
    }

    // RFC 6901 JSON-pointer segment escaping: '~' -> '~0', '/' -> '~1' (encode) and back (decode).
    private static string EncodePointerSegment(string raw) => raw.Replace("~", "~0").Replace("/", "~1");

    private static string DecodePointerSegment(string raw) => raw.Replace("~1", "/").Replace("~0", "~");

    private static bool TryResolveJsonPointer(JsonElement root, string jsonPointer, out JsonElement value)
    {
        value = root;
        if (string.IsNullOrEmpty(jsonPointer) || jsonPointer == "/")
        {
            return true;
        }

        if (!jsonPointer.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var rawSegment in jsonPointer.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = DecodePointerSegment(rawSegment);
            if (value.ValueKind == JsonValueKind.Object)
            {
                if (!value.TryGetProperty(segment, out value))
                {
                    return false;
                }

                continue;
            }

            if (value.ValueKind == JsonValueKind.Array
                && int.TryParse(segment, out var index)
                && index >= 0
                && index < value.GetArrayLength())
            {
                value = value[index];
                continue;
            }

            return false;
        }

        return true;
    }

    private static Dictionary<string, double> ToMutable(
        IReadOnlyDictionary<string, double>? metrics)
        => metrics is null
            ? new Dictionary<string, double>()
            : new Dictionary<string, double>(metrics);
}

/// <summary>一次 <see cref="SystemMtExecutionRecorder.RecordAsync"/> 写入的行 id。</summary>
public sealed record RecordedExecution(Guid ExecutionId, Guid? ResultId);
