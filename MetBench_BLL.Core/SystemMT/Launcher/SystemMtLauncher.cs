using MetBench_BLL.Equations;
using MetBench_BLL.Equations.Bateman;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Runtime;
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
/// 当前 launcher 只接受显式注入的 <see cref="IMrCatalogProvider"/>，不再保留
/// 生产路径的硬编码 fallback；每个 blueprint 已含 v2 pipeline 规格
/// (InputParser / OutputParser / TransformSteps / AssertionTypeCode)。
/// 多步 MR(decay-chain / damped-oscillator)在构造时把对应
/// <see cref="CompositeTransform"/> 注册到 <see cref="TransformationRegistry"/>。
/// </remarks>
public sealed class SystemMtLauncher : ISystemMtLauncher, ISystemMtCatalogReader
{
    private readonly LauncherOptions _options;
    private readonly ISystemMtPipeline _pipeline;
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly IAnomalyService _anomalyService;
    private readonly AnomalySeverityThresholds _severityThresholds;
    private readonly IRuntimeProfileProvider _runtimeProfileProvider;
    private readonly IRuntimePreflightService _runtimePreflightService;
    private readonly IReadOnlyDictionary<string, MrBlueprint> _mrCatalog;
    private readonly EquationFunctionRegistry _equationFunctions;

    public SystemMtLauncher(
        LauncherOptions options,
        ISystemMtPipeline pipeline,
        SystemMtExecutionRecorder recorder,
        IAnomalyService anomalyService,
        IMrCatalogProvider catalogProvider,
        AnomalySeverityThresholds? severityThresholds = null)
        : this(
            options,
            pipeline,
            recorder,
            anomalyService,
            catalogProvider,
            severityThresholds,
            runtimeProfileProvider: null,
            runtimePreflightService: null)
    {
    }

    public SystemMtLauncher(
        LauncherOptions options,
        ISystemMtPipeline pipeline,
        SystemMtExecutionRecorder recorder,
        IAnomalyService anomalyService,
        IMrCatalogProvider catalogProvider,
        AnomalySeverityThresholds? severityThresholds,
        IRuntimeProfileProvider? runtimeProfileProvider,
        IRuntimePreflightService? runtimePreflightService)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _anomalyService = anomalyService ?? throw new ArgumentNullException(nameof(anomalyService));
        ArgumentNullException.ThrowIfNull(catalogProvider);
        _severityThresholds = severityThresholds ?? AnomalySeverityThresholds.Default;
        _runtimeProfileProvider = runtimeProfileProvider
            ?? new LauncherOptionsRuntimeProfileProvider(_options);
        _runtimePreflightService = runtimePreflightService
            ?? new RuntimePreflightService(new DefaultProcessExecutor());
        _mrCatalog = catalogProvider.Load()
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
    public IReadOnlyList<MrCatalogEntry> GetCatalogEntries() =>
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
        RuntimeProfile? resolvedRuntimeProfile = null;
        var runtimeProfileResolutionError = string.Empty;
        var pythonExecutable = blueprint.PythonExecutable;
        var parserPythonExecutable = blueprint.PythonExecutable;
        var runnerExecutable = blueprint.PythonExecutable;
        IReadOnlyList<string> runnerBaseArguments = new[] { blueprint.RunnerScriptPath };
        try
        {
            resolvedRuntimeProfile = CreateRuntimeProfile(blueprint);
            pythonExecutable = resolvedRuntimeProfile.DockerMcp?.PythonExecutable
                ?? resolvedRuntimeProfile.ExecutablePath
                ?? blueprint.PythonExecutable;
            parserPythonExecutable = resolvedRuntimeProfile.DockerMcp?.LocalPythonExecutable
                ?? pythonExecutable;
            if (resolvedRuntimeProfile.DockerMcp is { } dockerMcp)
            {
                runnerExecutable = dockerMcp.LocalExecutable;
                runnerBaseArguments = Array.Empty<string>();
            }
            else
            {
                runnerExecutable = pythonExecutable;
            }
        }
        catch (RuntimeEnvironmentResolutionException ex)
        {
            runtimeProfileResolutionError = ex.Message;
            runnerExecutable = pythonExecutable;
        }

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
            InputParserInvocation: new ProcessInvocation(
                parserPythonExecutable,
                new[] { blueprint.InputParserScriptPath }),
            OutputParserInvocation: new ProcessInvocation(
                parserPythonExecutable,
                new[] { blueprint.OutputParserScriptPath }),
            RunnerInvocation: new ProcessInvocation(
                runnerExecutable,
                runnerBaseArguments),
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

        // PR-Bol-2A: 多相 error-monotonic 分支 — launcher 预构建 TypedSpec + TypedPredicate
        // via TypedSpecFactory.ForErrorMonotonic (string-code 分派仍封闭在 Migration/ 内),
        // 然后调 ExecuteMultiPhaseAsync. 30+ 现存 2-side MR 走 else 分支保持字节一致.
        var runtimePreflight = await CreateRuntimePreflightResultAsync(
                blueprint,
                resolvedRuntimeProfile,
                runtimeProfileResolutionError,
                cancellationToken)
            .ConfigureAwait(false);
        var runtimeEvidence = RuntimeEvidence.FromPreflightResult(runtimePreflight);
        context = context with { RuntimeProfile = runtimePreflight.Profile };
        if (!runtimePreflight.Passed)
        {
            var blocked = await _recorder.RecordBlockedPreflightAsync(
                context, runtimePreflight, mrInstanceId: -1, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return new MrRunResult(
                RecordId: blocked.ExecutionId.ToString(),
                MrId: mrId,
                Passed: false,
                FailureReason: $"Runtime preflight failed: {runtimePreflight.Detail}",
                ValueName: blueprint.Mr.ValueName,
                SourceValue: 0.0,
                FollowUpValue: 0.0,
                SourceElapsed: TimeSpan.Zero,
                FollowUpElapsed: TimeSpan.Zero);
        }

        PipelineOutcome outcome;
        if (blueprint.RefinementPhases is { Count: > 0 } phases)
        {
            var orderedRoles = phases.Take(phases.Count - 1).Select(p => p.Role).ToArray();
            var referenceRole = phases[phases.Count - 1].Role;
            var typedSpec = MetBench_BLL.SystemMT.Catalog.Typed.Migration.TypedSpecFactory.ForErrorMonotonic(
                mrCode: blueprint.Mr.Id,
                metric: blueprint.Mr.ValueName,
                orderedRoles: orderedRoles,
                referenceRole: referenceRole,
                normKind: MetBench_BLL.SystemMT.Catalog.Typed.Specs.NormKind.Relative,
                toleranceRel: context.Tolerance.ToleranceRel);
            var typedPredicate = (MetBench_BLL.SystemMT.Catalog.Typed.Specs.ErrorMonotonicPredicate)typedSpec.Predicates![0];
            var contextWithSpec = context with
            {
                TypedSpec = typedSpec,
                TypedPredicate = typedPredicate,
            };
            outcome = await _pipeline.ExecuteMultiPhaseAsync(
                new MultiPhaseExecutionContext(contextWithSpec, phases),
                progress: null,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            outcome = await _pipeline.ExecuteAsync(context, progress: null, cancellationToken)
                .ConfigureAwait(false);
        }

        var recorded = await _recorder.RecordAsync(
            context, outcome, mrInstanceId: -1, runtimeEvidence: runtimeEvidence, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

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

    private RuntimeProfile CreateRuntimeProfile(MrBlueprint blueprint)
    {
        var resolved = _runtimeProfileProvider.GetProfile(blueprint.RuntimeKey);
        return new RuntimeProfile(
            resolved.RuntimeKey,
            resolved.DisplayName,
            resolved.Kind,
            resolved.ExecutablePath,
            resolved.DependencyChecks,
            resolved.VersionChecks,
            resolved.RequiredEnvironmentVariables,
            timeout: blueprint.Timeout,
            dockerMcp: resolved.DockerMcp);
    }

    private async Task<RuntimePreflightResult> CreateRuntimePreflightResultAsync(
        MrBlueprint blueprint,
        RuntimeProfile? resolvedProfile,
        string profileResolutionError,
        CancellationToken cancellationToken)
    {
        if (resolvedProfile is not null)
        {
            return await _runtimePreflightService
                .CheckAsync(resolvedProfile, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            return await _runtimePreflightService
                .CheckAsync(CreateRuntimeProfile(blueprint), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RuntimeEnvironmentResolutionException ex)
        {
            return MissingRuntimeProfilePreflight(blueprint, ex.Message);
        }
        catch (InvalidOperationException) when (!string.IsNullOrWhiteSpace(profileResolutionError))
        {
            return MissingRuntimeProfilePreflight(blueprint, profileResolutionError);
        }
    }

    private static RuntimePreflightResult MissingRuntimeProfilePreflight(
        MrBlueprint blueprint,
        string detail)
    {
        var runtimeKey = RuntimeProfile.NormalizeRuntimeKey(blueprint.RuntimeKey);
        var profile = RuntimeProfile.Placeholder(
            runtimeKey,
            $"{runtimeKey} runtime",
            RuntimeKind.RemotePlaceholder);
        var diagnostic = new RuntimePreflightDiagnostic(
            "profile",
            runtimeKey,
            false,
            RuntimeFailureKind.RuntimeProfileMissing,
            detail);

        return RuntimePreflightResult.Blocked(
            profile,
            RuntimeFailureKind.RuntimeProfileMissing,
            detail,
            new[] { diagnostic });
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

        // PR-132: if the typed verifier reported Skipped*/InvalidSpec, the legacy
        // assertion result still flagged Passed=false (pipeline fallback for any
        // non-Passed VerifyStatus), which made the pipeline call this an Anomaly.
        // None of those typed statuses represent a real MR violation, so the
        // launcher must not create an anomaly row for them.
        if (outcome.TypedVerification is { } typed
            && typed.Status is MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerifyStatus.SkippedMissingObservable
                            or MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerifyStatus.SkippedNotApplicable
                            or MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerifyStatus.InvalidSpec)
        {
            return;
        }

        var severity = AnomalyClassifier.ClassifySeverity(outcome, _severityThresholds);
        var category = AnomalyClassifier.ClassifyCategory(outcome);
        var typedSummary = BuildTypedVerificationSummary(outcome);
        await _anomalyService.RecordAnomalyAsync(
            mrName, resultId.Value.ToString(), severity, category,
            typedVerificationSummary: typedSummary,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Projects <see cref="PipelineOutcome.TypedVerification"/> to a one-line
    /// summary string for inclusion in the anomaly's Notes and audit detailsJson.
    /// Returns null when the outcome did not carry a typed verification result.
    /// </summary>
    internal static string? BuildTypedVerificationSummary(PipelineOutcome outcome)
    {
        var verification = outcome.TypedVerification;
        var predicate = outcome.TypedPredicate;
        if (verification is null)
        {
            return null;
        }

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var parts = new System.Collections.Generic.List<string>
        {
            $"typed={verification.Status}",
        };
        if (predicate is not null)
        {
            var kind = predicate switch
            {
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.BinaryComparisonPredicate => "BinaryComparison",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.ScaledEqualityPredicate => "ScaledEquality",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.CrossMethodComparisonPredicate => "CrossMethodComparison",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.VarianceRatioPredicate => "VarianceRatio",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.FieldEqualityPredicate => "FieldEquality",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.FieldProportionalityPredicate => "FieldProportionality",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.DerivedInvariantPredicate => "DerivedInvariant",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.OrderedSequenceShapePredicate => "OrderedSequenceShape",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.ErrorMonotonicPredicate => "ErrorMonotonic",
                MetBench_BLL.SystemMT.Catalog.Typed.Specs.SubadditivePredicate => "Subadditive",
                _ => predicate.GetType().Name,
            };
            parts.Add($"predicate={predicate.PredicateId} ({kind})");
        }
        if (verification.Diagnostic is { } d)
        {
            parts.Add($"residual={d.Residual.ToString("G", inv)}");
            parts.Add($"tolerance={d.Tolerance.ToString("G", inv)}");
        }
        if (verification.Context?.Reason is { } reason && !string.IsNullOrEmpty(reason))
        {
            parts.Add($"reason: {reason}");
        }

        // Include the metric name when it's available on the predicate; this
        // makes the summary self-describing for cross-MR comparison.
        var metric = predicate switch
        {
            MetBench_BLL.SystemMT.Catalog.Typed.Specs.BinaryComparisonPredicate b => b.Metric,
            MetBench_BLL.SystemMT.Catalog.Typed.Specs.ScaledEqualityPredicate s => s.Metric,
            MetBench_BLL.SystemMT.Catalog.Typed.Specs.CrossMethodComparisonPredicate c => c.Metric,
            MetBench_BLL.SystemMT.Catalog.Typed.Specs.VarianceRatioPredicate v => v.StatisticalMetric,
            _ => null,
        };
        if (!string.IsNullOrEmpty(metric))
        {
            parts.Insert(1, $"metric={metric}");
        }

        return string.Join(" ", parts);
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
    AssertionTolerance? Tolerance = null,
    // PR-Bol-2A: 多相 error-monotonic MR 用 (null/empty = 走传统 2-side ExecuteAsync)
    IReadOnlyList<MetBench_BLL.SystemMT.Pipeline.RefinementPhase>? RefinementPhases = null)
{
    public string RuntimeKey { get; init; } = PythonExecutableKinds.System;
}

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
    AssertionTolerance? Tolerance,
    // PR-Bol-2A: 多相 error-monotonic MR 用 (null/empty = 走传统 2-side path)
    IReadOnlyList<MetBench_BLL.SystemMT.Pipeline.RefinementPhase>? RefinementPhases = null)
{
    /// <summary>
    /// Meta-pattern slug declared by the manifest binding (<c>Mono</c> / <c>Inv</c> /
    /// <c>Conv</c>, per <c>CLAUDE.md §2.2 T4</c>). Empty string when the catalog source
    /// does not project meta-pattern (e.g. test fakes that pre-date PR-T3-7); the meta-
    /// pattern auditor treats empty as "Unclassified" so unmigrated rows are visible
    /// rather than silently buried.
    /// </summary>
    public string MetaPattern { get; init; } = string.Empty;

    /// <summary>
    /// Manifest runtime key (system/openmoc/openmc/scipy/custom). Kept separate
    /// from the resolved executable so runtime evidence is not guessed from paths.
    /// </summary>
    public string RuntimeKey { get; init; } = PythonExecutableKinds.System;

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
            bp.Tolerance,
            bp.RefinementPhases)
        {
            // Derive the meta-pattern from the MrFamily convention so the entry
            // projected from the legacy hardcoded path carries the same metadata
            // as the manifest-backed projection. See MrMetaPatternConventions.
            MetaPattern = MrMetaPatternConventions.FromMrFamily(bp.Mr.MrFamily),
            RuntimeKey = bp.RuntimeKey,
        };

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
            Tolerance,
            RefinementPhases)
        {
            RuntimeKey = RuntimeKey,
        };
}

/// <summary>Public mirror of internal MrTransformStep for IMrCatalogProvider boundary.</summary>
public sealed record MrCatalogTransformStep(string TransformationName, string TargetFieldPath);
