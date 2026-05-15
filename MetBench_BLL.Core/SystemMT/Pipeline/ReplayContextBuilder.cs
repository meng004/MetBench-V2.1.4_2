using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// 把一条 v2 Anomaly 重新建模成可供 <see cref="ISystemMtPipeline"/> 重跑的 <see cref="PipelineContext"/>。
/// </summary>
/// <remarks>
/// Replay 需要的 18 个 PipelineContext 字段散落在多个 v2 collection：
///   Anomaly → Result → Execution → MRInstance → MRBinding → MetamorphicRelation + Application + ParameterMappings
/// 本服务把 join 路径集中起来，避免 UI ViewModel 直接知道 5 个 Repository。
///
/// Replay 跟新跑的区别：把 Execution 的版本快照 (CatalogVersionSha / SutVersionSnapshot /
/// MetbenchVersion) 复制到 replay context，让 replay 能用同一份 catalog + 同一版 SUT 跑 ——
/// 这是排查 fixed-upstream / regression-on-replay 分类的关键。
/// </remarks>
public sealed class ReplayContextBuilder
{
    private readonly IAnomalyRepository _anomalies;
    private readonly IResultRepository _results;
    private readonly IExecutionRepository _executions;
    private readonly IMRInstanceRepository _mrInstances;
    private readonly IMRBindingRepository _mrBindings;
    private readonly IMetamorphicRelationRepository _mrs;
    private readonly IApplicationRepository _apps;

    public ReplayContextBuilder(
        IAnomalyRepository anomalies,
        IResultRepository results,
        IExecutionRepository executions,
        IMRInstanceRepository mrInstances,
        IMRBindingRepository mrBindings,
        IMetamorphicRelationRepository mrs,
        IApplicationRepository apps)
    {
        _anomalies = anomalies;
        _results = results;
        _executions = executions;
        _mrInstances = mrInstances;
        _mrBindings = mrBindings;
        _mrs = mrs;
        _apps = apps;
    }

    /// <summary>
    /// 把 anomalyId 解析成 PipelineContext + 原 Execution 的 outcome（用于 ReplayService 对比）。
    /// </summary>
    /// <exception cref="InvalidOperationException">任意 join 中断（被引用的实体不存在）。</exception>
    public ReplayContextBuildResult Build(Guid anomalyId, string workingDirectory)
    {
        var anomaly = _anomalies.Get(anomalyId)
            ?? throw new InvalidOperationException($"Anomaly {anomalyId} not found.");

        var result = _results.Get(anomaly.ResultId)
            ?? throw new InvalidOperationException($"Result {anomaly.ResultId} not found for anomaly {anomalyId}.");

        var execution = _executions.Get(result.ExecutionId)
            ?? throw new InvalidOperationException($"Execution {result.ExecutionId} not found for result {result.IdResult}.");

        var mrInstance = _mrInstances.Get(execution.MRInstanceId)
            ?? throw new InvalidOperationException($"MRInstance {execution.MRInstanceId} not found.");

        var binding = _mrBindings.Get(mrInstance.MRBindingId)
            ?? throw new InvalidOperationException($"MRBinding {mrInstance.MRBindingId} not found.");

        var mr = _mrs.Get(binding.MRId)
            ?? throw new InvalidOperationException($"MetamorphicRelation id={binding.MRId} not found.");

        var app = _apps.Get(binding.ApplicationId)
            ?? throw new InvalidOperationException($"Application id={binding.ApplicationId} not found.");

        // ParameterOverrides 优先于 binding 默认；缺则用 binding ParameterMappings
        var parameters = mrInstance.ParameterOverrides ?? new Dictionary<string, string>();

        // 取第一个 ParameterMapping 当 TargetFieldPath（多 mapping 走 ComposingTransform，本 service first-ship 不展开）
        var firstMapping = binding.ParameterMappings.FirstOrDefault();
        var targetFieldPath = firstMapping?.ConcreteFieldPath ?? "";
        var pathSyntax = firstMapping?.PathSyntax ?? "json-pointer";

        var tolerance = binding.DefaultTolerance is { } dt
            ? new AssertionTolerance(
                NoiseAware: dt.NoiseAware,
                ToleranceRel: dt.ToleranceRel,
                ToleranceAbs: dt.ToleranceAbs,
                NoiseMultiplier: dt.NoiseMultiplier)
            : new AssertionTolerance();

        var samplePath = mrInstance.SampleCaseOverridePath ?? binding.DefaultSampleCasePath;

        var context = new PipelineContext(
            MrCode: mr.Code,
            TransformationName: mr.MetaPatternCode,  // 简化：用 MetaPatternCode 当 transformation hint
            AssertionTypeCode: mr.AssertionTypeCode,
            ValueName: ExtractValueName(mr),
            TargetFieldPath: targetFieldPath,
            PathSyntax: pathSyntax,
            Parameters: parameters,
            Tolerance: tolerance,
            ExtraAssertionValues: null,

            SutName: app.Name ?? "",
            SourceCasePath: samplePath,
            WorkingDirectory: workingDirectory,
            InputParserCommand: app.CodeName ?? "",   // 调用方需进一步解析 — 简化为 codename
            OutputParserCommand: app.CodeName ?? "",
            RunnerCommand: app.CodeName ?? "",
            TimeoutSeconds: 300,

            CatalogVersionSha: execution.CatalogVersionSha,
            SutVersionSnapshot: execution.SutVersionSnapshot,
            MetbenchVersion: execution.MetbenchVersion,
            TriggeredBy: $"replay-of-{anomalyId}");

        return new ReplayContextBuildResult(
            Context: context,
            OriginalAnomaly: anomaly,
            OriginalResult: result,
            OriginalExecution: execution,
            MrCode: mr.Code,
            SutName: app.Name ?? "",
            TriggerParameters: parameters);
    }

    /// <summary>从 MR 的 Description / Operator 字段推断 ValueName，缺省 "k_eff"。</summary>
    internal static string ExtractValueName(MetamorphicRelation mr)
    {
        // MR.Operator 在 v1 偶尔填了主值字段名；否则默认 k_eff（pin-cell SUT 主断言量）
        return string.IsNullOrEmpty(mr.Operator) ? "k_eff" : mr.Operator;
    }
}

/// <summary>ReplayContextBuilder 的返回 —— 含 context + 原 anomaly/result/execution 引用便于 UI 对比展示。</summary>
public sealed record ReplayContextBuildResult(
    PipelineContext Context,
    MetBench_Domain.Anomaly OriginalAnomaly,
    Result OriginalResult,
    Execution OriginalExecution,
    string MrCode,
    string SutName,
    IReadOnlyDictionary<string, string> TriggerParameters);
