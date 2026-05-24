using MetBench_BLL.Equations;
using MetBench_BLL.Equations.Bateman;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Transformations;

namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Production implementation of <see cref="ISystemMtLauncher"/>.
/// Owns the MR registry; routes each MR through <see cref="ISystemMtPipeline"/>
/// (v2 single-engine path) and persists outcomes via <see cref="SystemMtExecutionRecorder"/>
/// as <c>Execution</c> + <c>Result</c>(+ <c>Anomaly</c>) into the unified
/// <c>MR.Litedb</c> schema.
/// </summary>
/// <remarks>
/// 计划见 docs/superpowers/plans/2026-05-22-systemmt-engine-unification-plan.md。
/// 17 MR 的硬编码目录（S8-P1..P4 扩展前为 9）(<see cref="LegacyCatalogFactory.Build"/>)是 v2 数据驱动 MR 目录全面
/// 落地前的过渡形态;每个 blueprint 已含 v2 pipeline 规格(InputParser /
/// OutputParser / TransformSteps / AssertionTypeCode)。多步 MR(decay-chain /
/// damped-oscillator)在构造时把对应 <see cref="CompositeTransform"/> 注册到
/// <see cref="TransformationRegistry"/>。
/// </remarks>
public sealed class SystemMtLauncher : ISystemMtLauncher
{
    private readonly LauncherOptions _options;
    private readonly ISystemMtPipeline _pipeline;
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly IAnomalyService _anomalyService;
    private readonly AnomalySeverityThresholds _severityThresholds;
    private readonly IReadOnlyDictionary<string, MrBlueprint> _mrCatalog;
    private readonly EquationFunctionRegistry _equationFunctions;

    public SystemMtLauncher(
        LauncherOptions options,
        ISystemMtPipeline pipeline,
        SystemMtExecutionRecorder recorder,
        IAnomalyService anomalyService,
        IMrCatalogProvider? catalogProvider = null,
        AnomalySeverityThresholds? severityThresholds = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _anomalyService = anomalyService ?? throw new ArgumentNullException(nameof(anomalyService));
        _severityThresholds = severityThresholds ?? AnomalySeverityThresholds.Default;
        // Phase C: provider-backed catalog. When no provider is supplied (legacy DI sites that
        // haven't yet registered IMrCatalogProvider — e.g. WPF App.xaml.cs pre-Task-3-VM), fall
        // back to the transitional HardcodedMrCatalogProvider. Task 4 marks Hardcoded obsolete;
        // Task 7 removes both the fallback and Hardcoded entirely once VM-side DI is registered.
        var provider = catalogProvider ?? new HardcodedMrCatalogProvider(options);
        _mrCatalog = provider.Load()
            .Select(entry => entry.ToBlueprint())
            .ToDictionary(b => b.Mr.Id, StringComparer.Ordinal);
        _equationFunctions = BuildEquationFunctionRegistry();

        // Register CompositeTransform for multi-step MRs that do NOT use a Recipe
        // (EquationKey != "" means the recipe in _equationFunctions handles the composition).
        foreach (var bp in _mrCatalog.Values
            .Where(b => b.TransformSteps.Count > 1 && string.IsNullOrEmpty(b.EquationKey)))
        {
            var compositeName = CompositeNameFor(bp.Mr.Id);
            var blueprintRef = bp;
            TransformationRegistry.RegisterIfMissing(compositeName, () =>
            {
                var steps = blueprintRef.TransformSteps
                    .Select(s => new CompositeTransform.Step(
                        TransformationRegistry.Get(s.TransformationName),
                        s.TargetFieldPath,
                        new Dictionary<string, string>()))
                    .ToList();
                return new CompositeTransform(compositeName, steps);
            });
        }
    }

    private static EquationFunctionRegistry BuildEquationFunctionRegistry()
    {
        var reg = new EquationFunctionRegistry();

        // P4: bateman.ScaleInitial — L1 Recipe:按 factor 等比缩放三个初始量
        const string batemanScaleInitialRecipe = """
            {"compose":[
              {"op":"ScaleField","path":"/initial/N_A","params":{"factor":"{factor}"}},
              {"op":"ScaleField","path":"/initial/N_B","params":{"factor":"{factor}"}},
              {"op":"ScaleField","path":"/initial/N_C","params":{"factor":"{factor}"}}
            ]}
            """;
        reg.Register(new RecipeBasedEquationFunction(
            "bateman", "ScaleInitial", "transformation", batemanScaleInitialRecipe));

        // P4: bateman.AnalyticSolution — L2 解析解
        reg.Register(new BatemanAnalyticSolution());

        return reg;
    }

    private static string CompositeNameFor(string mrId) => $"Composite-{mrId}";

    /// <summary>
    /// internal 暴露 launcher 内部 MR 目录（17 entries as of S8-P4）的快照,供 <see cref="LauncherCatalogV2Importer"/>
    /// 把数据"导入"到 v2 实体表(Application + MetamorphicRelation + MRBinding)。
    /// </summary>
    internal IReadOnlyList<MrCatalogEntry> GetCatalogEntries() =>
        _mrCatalog.Values.Select(MrCatalogEntry.FromBlueprint).ToList();

    public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<MrSummary> mrs = _mrCatalog.Values
            .Select(s => s.Mr)
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(mrs);
    }

    public async Task<MrRunResult> RunAsync(
        string mrId,
        IReadOnlyDictionary<string, string>? parameterOverrides = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrId))
        {
            throw new ArgumentException("MR id is required", nameof(mrId));
        }
        if (!_mrCatalog.TryGetValue(mrId, out var blueprint))
        {
            throw new ArgumentException($"Unknown MR id: '{mrId}'", nameof(mrId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new Dictionary<string, string>(blueprint.Mr.DefaultParameters, StringComparer.Ordinal);
        if (parameterOverrides is not null)
        {
            foreach (var (key, value) in parameterOverrides)
            {
                parameters[key] = value;
            }
        }

        // Set up workdir + copy sample to source.in.json
        var workRoot = Path.Combine(Path.GetTempPath(), blueprint.WorkRootName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workRoot);
        var sourceInputPath = Path.Combine(workRoot, "source.in.json");

        var sampleSource = Path.Combine(_options.SutRoot, blueprint.SampleCaseRelativePath);
        if (!File.Exists(sampleSource))
        {
            throw new FileNotFoundException(
                $"MR '{mrId}' sample case not found at {sampleSource}", sampleSource);
        }
        var sampleContent = await File.ReadAllTextAsync(sampleSource, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(sourceInputPath, sampleContent, cancellationToken).ConfigureAwait(false);

        // Resolve transformation (single step direct;multi-step uses pre-registered composite)
        var (transformName, targetFieldPath) = ResolveTransformation(blueprint);

        var context = new PipelineContext(
            MrCode: blueprint.Mr.Id,
            TransformationName: transformName,
            AssertionTypeCode: blueprint.AssertionTypeCode,
            ValueName: blueprint.Mr.ValueName,
            TargetFieldPath: targetFieldPath,
            PathSyntax: "json-pointer",
            Parameters: parameters,
            Tolerance: blueprint.Tolerance ?? new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: blueprint.Mr.SutName,
            SourceCasePath: sourceInputPath,
            WorkingDirectory: workRoot,
            InputParserCommand: $"\"{blueprint.PythonExecutable}\" \"{blueprint.InputParserScriptPath}\"",
            OutputParserCommand: $"\"{blueprint.PythonExecutable}\" \"{blueprint.OutputParserScriptPath}\"",
            RunnerCommand: $"\"{blueprint.PythonExecutable}\" \"{blueprint.RunnerScriptPath}\"",
            TimeoutSeconds: (int)blueprint.Timeout.TotalSeconds,
            CatalogVersionSha: string.Empty,
            SutVersionSnapshot: string.Empty,
            MetbenchVersion: string.Empty,
            TriggeredBy: "launcher")
        {
            // P4: 传入方程业务键 + 注册表，启用决策 B Recipe 查找
            EquationKey = blueprint.EquationKey,
            EquationFunctionRegistry = string.IsNullOrEmpty(blueprint.EquationKey)
                ? null : _equationFunctions,
        };

        var outcome = await _pipeline.ExecuteAsync(context, progress: null, cancellationToken)
            .ConfigureAwait(false);

        var recorded = _recorder.Record(context, outcome, mrInstanceId: -1);

        await RecordAnomalyIfFailedAsync(blueprint.Mr.DisplayName, recorded.ResultId, outcome, cancellationToken)
            .ConfigureAwait(false);

        return new MrRunResult(
            RecordId: recorded.ExecutionId.ToString(),
            MrId: mrId,
            Passed: outcome.FinalStatus == PipelineStatus.Ok,
            FailureReason: outcome.ErrorMessage ?? outcome.AssertionResult?.FailureReason ?? string.Empty,
            ValueName: blueprint.Mr.ValueName,
            SourceValue: outcome.AssertionResult?.SourceValue ?? 0.0,
            FollowUpValue: outcome.AssertionResult?.FollowupValue ?? 0.0,
            SourceElapsed: outcome.SourceElapsed,
            FollowUpElapsed: outcome.FollowupElapsed);
    }

    private static (string TransformationName, string TargetFieldPath) ResolveTransformation(MrBlueprint blueprint)
    {
        if (blueprint.TransformSteps.Count == 1)
        {
            var s = blueprint.TransformSteps[0];
            return (s.TransformationName, s.TargetFieldPath);
        }
        // 多步:由 ctor 已注册的 Composite 处理,TargetFieldPath 由 CompositeTransform 内部各 step 提供
        return (CompositeNameFor(blueprint.Mr.Id), string.Empty);
    }

    /// <summary>
    /// 失败 run 自动建一条 Anomaly,链 Result.IdResult。
    /// internal 暴露给测试做 wiring 验证(InternalsVisibleTo MetBench_SystemMT.Tests)。
    /// </summary>
    internal async Task RecordAnomalyIfFailedAsync(
        string mrName,
        Guid? resultId,
        PipelineOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (outcome.FinalStatus == PipelineStatus.Ok) return;
        if (resultId is null) return;  // error/timeout/cancelled 无 Result(recorder 未写)
        var severity = AnomalyClassifier.ClassifySeverity(outcome, _severityThresholds);
        var category = AnomalyClassifier.ClassifyCategory(outcome);
        await _anomalyService.RecordAnomalyAsync(
            mrName, resultId.Value.ToString(), severity, category, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
        IReadOnlyList<BatchMrRunRequest> requests,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (requests is null)
        {
            throw new ArgumentNullException(nameof(requests));
        }
        if (requests.Count == 0)
        {
            return Array.Empty<MrRunResult>();
        }

        // Pre-validate every request id before running anything. A typo in
        // request[5] should not waste four successful runs.
        for (var i = 0; i < requests.Count; i++)
        {
            var req = requests[i];
            if (req is null || string.IsNullOrWhiteSpace(req.MrId))
            {
                throw new ArgumentException(
                    $"Batch request at index {i} has a blank MR id", nameof(requests));
            }
            if (!_mrCatalog.ContainsKey(req.MrId))
            {
                throw new ArgumentException(
                    $"Batch request at index {i} has an unknown MR id: '{req.MrId}'",
                    nameof(requests));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<MrRunResult>(requests.Count);
        var total = requests.Count;

        for (var i = 0; i < requests.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var req = requests[i];

            progress?.Report(new BatchProgress(
                Completed: i, Total: total,
                CurrentMrId: req.MrId, LastResult: null));

            MrRunResult result;
            try
            {
                result = await RunAsync(req.MrId, req.ParameterOverrides, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Infrastructure failure inside one MR (Python missing,
                // SUT file gone, persistence I/O error). Synthesize a failed
                // result so the remaining MRs still execute.
                result = new MrRunResult(
                    RecordId: string.Empty,
                    MrId: req.MrId,
                    Passed: false,
                    FailureReason: $"Run threw {ex.GetType().Name}: {ex.Message}",
                    ValueName: string.Empty,
                    SourceValue: 0,
                    FollowUpValue: 0,
                    SourceElapsed: TimeSpan.Zero,
                    FollowUpElapsed: TimeSpan.Zero);
            }

            results.Add(result);
            progress?.Report(new BatchProgress(
                Completed: i + 1, Total: total,
                CurrentMrId: req.MrId, LastResult: result));
        }

        return results;
    }
    // (BuildMrCatalog extracted to MetBench_BLL.SystemMT.Launcher.LegacyCatalogFactory in Task 2a)
}

/// <summary>
/// Runtime blueprint of a single MR×SUT binding consumed by <see cref="SystemMtLauncher"/>.
/// Lifted from <c>private</c> to <c>internal</c> in Task 2a so
/// <see cref="LegacyCatalogFactory"/> (a sibling type) can build instances of it.
/// </summary>
internal sealed record MrBlueprint(
    MrSummary Mr,
    string SampleCaseRelativePath,
    string RunnerScriptPath,
    string InputAdapterScriptPath,
    string OutputAdapterScriptPath,
    string PythonExecutable,
    string WorkRootName,
    TimeSpan Timeout,
    // ↓ v2 pipeline data（P3.2 入位；P3.3 launcher.RunAsync 重写后消费）
    string InputParserScriptPath,
    string OutputParserScriptPath,
    IReadOnlyList<MrTransformStep> TransformSteps,
    string AssertionTypeCode,
    // P4: 方程业务键（空 = 无关联；非空 = 走 EquationFunctionRegistry Recipe 查找）
    string EquationKey = "",
    // S8-P5 review fix: approx/scaled-equality MR 必须显式设容差；默认 0/0 会让
    // BeApproximately(src, 0) 退化为 bit-exact equality，永远 fail 在数值噪声上
    AssertionTolerance? Tolerance = null);

/// <summary>v2 pipeline 的单步变换规格。多步在 launcher.RunAsync 内包 <c>CompositeTransform</c>。</summary>
internal sealed record MrTransformStep(
    string TransformationName,
    string TargetFieldPath);

/// <summary>
/// launcher 内部 MR 目录的对外快照,导入 v2 实体表时用。
/// 字段是 <see cref="LauncherCatalogV2Importer"/> 真正用得到的子集
/// (不含 PythonExecutable / WorkRootName / Timeout 等纯运行时配置)。
/// </summary>
/// <remarks>
/// Public visibility so <see cref="MetBench_BLL.SystemMT.Catalog.IMrCatalogProvider"/>
/// (also public) can expose this record across the launcher↔provider boundary.
/// Task 2 will add a <c>FromBlueprint</c> factory method here.
/// </remarks>
public sealed record MrCatalogEntry(
    MrSummary Mr,
    string SampleCaseRelativePath,
    string RunnerScriptPath,
    string InputAdapterScriptPath,
    string OutputAdapterScriptPath,
    string PythonExecutable,
    string WorkRootName,
    TimeSpan Timeout,
    string InputParserScriptPath,
    string OutputParserScriptPath,
    IReadOnlyList<MrCatalogTransformStep> TransformSteps,
    string AssertionTypeCode,
    string EquationKey,
    AssertionTolerance? Tolerance)
{
    /// <summary>UI convenience: first step's transformation name (engine name, not display).</summary>
    public string PrimaryTransformationName =>
        TransformSteps.Count > 0 ? TransformSteps[0].TransformationName : string.Empty;

    /// <summary>
    /// Project an internal <see cref="MrBlueprint"/> to the public catalog-entry snapshot.
    /// </summary>
    internal static MrCatalogEntry FromBlueprint(MrBlueprint bp) =>
        new(bp.Mr,
            bp.SampleCaseRelativePath,
            bp.RunnerScriptPath,
            bp.InputAdapterScriptPath,
            bp.OutputAdapterScriptPath,
            bp.PythonExecutable,
            bp.WorkRootName,
            bp.Timeout,
            bp.InputParserScriptPath,
            bp.OutputParserScriptPath,
            bp.TransformSteps.Select(s => new MrCatalogTransformStep(s.TransformationName, s.TargetFieldPath)).ToList(),
            bp.AssertionTypeCode,
            bp.EquationKey,
            bp.Tolerance);

    /// <summary>
    /// Inverse of <see cref="FromBlueprint"/>; reconstructs the runtime blueprint for launcher consumption.
    /// </summary>
    internal MrBlueprint ToBlueprint() =>
        new(Mr,
            SampleCaseRelativePath,
            RunnerScriptPath,
            InputAdapterScriptPath,
            OutputAdapterScriptPath,
            PythonExecutable,
            WorkRootName,
            Timeout,
            InputParserScriptPath,
            OutputParserScriptPath,
            TransformSteps.Select(s => new MrTransformStep(s.TransformationName, s.TargetFieldPath)).ToList(),
            AssertionTypeCode,
            EquationKey,
            Tolerance);
}

/// <summary>Public mirror of internal MrTransformStep for IMrCatalogProvider boundary.</summary>
public sealed record MrCatalogTransformStep(string TransformationName, string TargetFieldPath);
