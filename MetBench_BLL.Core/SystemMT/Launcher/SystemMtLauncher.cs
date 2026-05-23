using MetBench_BLL.Equations;
using MetBench_BLL.Equations.Bateman;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Assertions;
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
/// 17 MR 的硬编码目录（S8-P1..P4 扩展前为 9）(<see cref="BuildMrCatalog"/>)是 v2 数据驱动 MR 目录全面
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
        AnomalySeverityThresholds? severityThresholds = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _anomalyService = anomalyService ?? throw new ArgumentNullException(nameof(anomalyService));
        _severityThresholds = severityThresholds ?? AnomalySeverityThresholds.Default;
        _mrCatalog = BuildMrCatalog(options).ToDictionary(s => s.Mr.Id, StringComparer.Ordinal);
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
        _mrCatalog.Values
            .Select(bp => new MrCatalogEntry(
                bp.Mr,
                bp.SampleCaseRelativePath,
                bp.RunnerScriptPath,
                bp.InputParserScriptPath,
                bp.OutputParserScriptPath,
                bp.AssertionTypeCode,
                bp.TransformSteps[0].TransformationName,
                bp.EquationKey))
            .ToList();

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

    private static IEnumerable<MrBlueprint> BuildMrCatalog(LauncherOptions options)
    {
        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmoc-pincell-nu-sigma-f",
                DisplayName: "OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)",
                SutName: "openmoc",
                TransformationName: "ScaleNuSigmaF",
                AssertionName: "GreaterThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Scaling the fuel material's nu-sigma-f cross section by factor > 1 must " +
                    "monotonically increase the dominant eigenvalue k_eff of the OpenMOC " +
                    "neutron-transport solution.",
                MrFamily: "NeutronTransport.Scaling.NuSigmaF"),
            SampleCaseRelativePath: Path.Combine("openmoc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_adapter.py"),
            PythonExecutable: options.OpenMocPython,
            WorkRootName: "MetBenchOpenMocNuSigmaF",
            Timeout: TimeSpan.FromMinutes(2),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/materials/fuel/nu_sigma_f") },
            AssertionTypeCode: "greater");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmoc-pincell-sigma-a",
                DisplayName: "OpenMOC pin-cell — ScaleFuelSigmaA (k_eff decreases)",
                SutName: "openmoc",
                TransformationName: "ScaleFuelSigmaA",
                AssertionName: "LessThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Scaling the fuel material's absorption cross section by factor > 1 must " +
                    "monotonically decrease the dominant eigenvalue k_eff.",
                MrFamily: "NeutronTransport.Scaling.SigmaA"),
            SampleCaseRelativePath: Path.Combine("openmoc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_adapter_sigma_a.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_adapter.py"),
            PythonExecutable: options.OpenMocPython,
            WorkRootName: "MetBenchOpenMocSigmaA",
            Timeout: TimeSpan.FromMinutes(2),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleFuelAbsorption", "/materials/fuel") },
            AssertionTypeCode: "less");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmc-pincell-nu-sigma-f",
                DisplayName: "OpenMC pin-cell — ScaleNuSigmaF (k_eff increases)",
                SutName: "openmc",
                TransformationName: "ScaleNuSigmaF",
                AssertionName: "GreaterThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Monte Carlo counterpart of the OpenMOC ScaleNuSigmaF MR. Scaling the " +
                    "fuel material's nu-sigma-f cross section by factor > 1 must monotonically " +
                    "increase k_eff in OpenMC's multi-group eigenvalue solve, just as it does " +
                    "in OpenMOC's deterministic transport solve. Same MR, different solver.",
                MrFamily: "NeutronTransport.Scaling.NuSigmaF"),
            SampleCaseRelativePath: Path.Combine("openmc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_adapter.py"),
            PythonExecutable: options.EffectiveOpenMcPython,
            WorkRootName: "MetBenchOpenMcNuSigmaF",
            Timeout: TimeSpan.FromMinutes(5),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/materials/fuel/nu_sigma_f") },
            AssertionTypeCode: "greater");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmc-pincell-sigma-a",
                DisplayName: "OpenMC pin-cell — ScaleFuelSigmaA (k_eff decreases)",
                SutName: "openmc",
                TransformationName: "ScaleFuelSigmaA",
                AssertionName: "LessThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Monte Carlo counterpart of the OpenMOC ScaleFuelSigmaA MR. Scaling fuel " +
                    "absorption by factor > 1 must monotonically decrease k_eff in OpenMC's " +
                    "multi-group eigenvalue solve.",
                MrFamily: "NeutronTransport.Scaling.SigmaA"),
            SampleCaseRelativePath: Path.Combine("openmc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_adapter_sigma_a.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_adapter.py"),
            PythonExecutable: options.EffectiveOpenMcPython,
            WorkRootName: "MetBenchOpenMcSigmaA",
            Timeout: TimeSpan.FromMinutes(5),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleFuelAbsorption", "/materials/fuel") },
            AssertionTypeCode: "less");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "heat-equation-amplitude",
                DisplayName: "1D heat equation — ScaleAmplitude (linearity)",
                SutName: "heat-equation",
                TransformationName: "ScaleAmplitude",
                AssertionName: "GreaterThan",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "1D heat equation with homogeneous Dirichlet BCs is linear in the initial " +
                    "profile. Scaling the initial amplitude by factor > 1 must scale max_u at " +
                    "t_final by the same factor.",
                MrFamily: "Diffusion.Scaling.Amplitude"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchHeatEq",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/initial/amplitude") },
            AssertionTypeCode: "greater");

        // S8-P2: Fourier MR 库扩展（复用 heat-equation SUT）
        yield return new MrBlueprint(
            new MrSummary(
                Id: "fourier-timestep-convergence",
                DisplayName: "1D heat equation — TimestepConvergence (forward-Euler refinement)",
                SutName: "heat-equation",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Time-step convergence MP_conv: doubling num_steps (halving the forward-Euler " +
                    "time-step dt) must leave max_u(t_final) within Euler truncation tolerance — " +
                    "the integrator is already at the fine-grid plateau given the chosen alpha.",
                MrFamily: "Fourier.Convergence.Timestep"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchHeatEq",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/num_steps") },
            AssertionTypeCode: "approx",
            EquationKey: "heat-equation-1d",
            // forward-Euler 1 阶 O(dt)；步长翻倍后 max_u 差 ~1e-3 量级；ToleranceRel=1e-2 → 充裕
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-2, ToleranceAbs: 1e-6));

        yield return new MrBlueprint(
            new MrSummary(
                Id: "fourier-alpha-monotonic",
                DisplayName: "1D heat equation — ScaleAlpha (diffusion monotonicity)",
                SutName: "heat-equation",
                TransformationName: "ScaleField",
                AssertionName: "LessThan",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Diffusion coefficient monotonicity MP_mono: at fixed t_final, larger alpha " +
                    "causes faster diffusive smoothing of the initial profile, so scaling alpha " +
                    "by factor > 1 must strictly decrease max_u(t_final).",
                MrFamily: "Fourier.Scaling.Alpha"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchHeatEq",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/alpha") },
            AssertionTypeCode: "less",
            EquationKey: "heat-equation-1d");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "decay-chain-scale-initial",
                DisplayName: "Decay chain — ScaleInitial (linearity)",
                SutName: "decay-chain",
                TransformationName: "ScaleInitial",
                AssertionName: "GreaterThan",
                ValueName: "N_C_final",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "The 3-nuclide Bateman decay chain A→B→C is linear in the initial " +
                    "quantities. Scaling every initial N by factor > 1 must scale the " +
                    "accumulated end-nuclide N_C_final by the same factor.",
                MrFamily: "Bateman.Scaling.Initial"),
            SampleCaseRelativePath: Path.Combine("decay_chain", "sample", "three_nuclide.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDecayChain",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_parser.py"),
            // P4: 使用 L1 Recipe（EquationFunctionRegistry["bateman","ScaleInitial"]）
            // 单步声明；路径由 Recipe 内部各 ScaleField step 管理
            TransformSteps: new[] { new MrTransformStep("ScaleInitial", "") },
            AssertionTypeCode: "greater",
            EquationKey: "bateman");

        // S8-P1: Bateman MR 库扩展（复用 decay-chain SUT）
        yield return new MrBlueprint(
            new MrSummary(
                Id: "bateman-mass-conservation",
                DisplayName: "Bateman — MassConservation (lambda invariance)",
                SutName: "decay-chain",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "total",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mass conservation MP_inv: the Bateman chain A→B→C conserves total " +
                    "nuclide count (no production/absorption). Scaling lambda_A must not " +
                    "change the conserved total = N_A+N_B+N_C at t_final.",
                MrFamily: "Bateman.Invariance.MassConservation"),
            SampleCaseRelativePath: Path.Combine("decay_chain", "sample", "three_nuclide.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDecayChain",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/lambda_A") },
            AssertionTypeCode: "approx",
            EquationKey: "bateman",
            // 守恒 total ≈ N_A0+N_B0+N_C0=1000；RK4 累积截断误差 ~1e-6 量级；ToleranceRel=1e-6 → bound ≈ 1e-3
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-6, ToleranceAbs: 1e-9));

        yield return new MrBlueprint(
            new MrSummary(
                Id: "bateman-timestep-cauchy",
                DisplayName: "Bateman — TimestepCauchy (RK4 convergence)",
                SutName: "decay-chain",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "N_C_final",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Time-step Cauchy convergence MP_conv: doubling num_steps (halving the " +
                    "RK4 step size) must leave N_C_final within RK4 truncation tolerance — " +
                    "the integrator is already at the fine-grid plateau.",
                MrFamily: "Bateman.Convergence.Timestep"),
            SampleCaseRelativePath: Path.Combine("decay_chain", "sample", "three_nuclide.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDecayChain",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/num_steps") },
            AssertionTypeCode: "approx",
            EquationKey: "bateman",
            // RK4 4 阶截断 O(dt^4)；步长翻倍后 max_u 差 ~1e-4 量级；ToleranceRel=1e-3 → 充裕
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        yield return new MrBlueprint(
            new MrSummary(
                Id: "damped-oscillator-scale-state",
                DisplayName: "Damped oscillator — ScaleInitialState (linearity)",
                SutName: "damped-oscillator",
                TransformationName: "ScaleInitialState",
                AssertionName: "GreaterThan",
                ValueName: "max_abs_displacement",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "The damped harmonic oscillator is linear and homogeneous in the " +
                    "initial state (x0, v0). Scaling the initial state by factor > 1 must " +
                    "scale the peak absolute displacement by the same factor.",
                MrFamily: "Oscillator.Scaling.InitialState"),
            SampleCaseRelativePath: Path.Combine("damped_oscillator", "sample", "underdamped.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDampedOsc",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_output_parser.py"),
            TransformSteps: new[]
            {
                new MrTransformStep("ScaleField", "/initial/x0"),
                new MrTransformStep("ScaleField", "/initial/v0"),
            },
            AssertionTypeCode: "greater");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "lotka-volterra-scale-gamma",
                DisplayName: "Lotka-Volterra — ScaleGamma (mean-prey identity)",
                SutName: "lotka-volterra",
                TransformationName: "ScaleGamma",
                AssertionName: "GreaterThan",
                ValueName: "mean_prey",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "By the Lotka-Volterra time-average identity ⟨prey⟩ = gamma / delta, " +
                    "scaling the predator death rate gamma by factor > 1 must increase " +
                    "the time-averaged prey population mean_prey.",
                MrFamily: "LotkaVolterra.Scaling.Gamma"),
            SampleCaseRelativePath: Path.Combine("lotka_volterra", "sample", "classic.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchLotkaVolterra",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/gamma") },
            AssertionTypeCode: "greater");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "projectile-scale-v0",
                DisplayName: "Projectile range — ScaleV0 (R ∝ v0² monotonic)",
                SutName: "projectile",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "range",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "By the projectile-motion identity R = v0²·sin(2θ)/g, scaling the " +
                    "initial speed v0 by factor > 1 must monotonically increase the " +
                    "horizontal range (in fact, by factor²).",
                MrFamily: "Projectile.Scaling.V0"),
            SampleCaseRelativePath: Path.Combine("projectile", "sample", "standard.txt"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchProjectile",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/v0") },
            AssertionTypeCode: "greater");

        // S8-P3: 1D subchannel SUT + navier-stokes 方程接入
        yield return new MrBlueprint(
            new MrSummary(
                Id: "subchannel-flow-temperature-monotone",
                DisplayName: "1D subchannel — ScaleMassFlux (flow ↑ ⇒ ΔT ↓)",
                SutName: "subchannel-1d",
                TransformationName: "ScaleField",
                AssertionName: "LessThan",
                ValueName: "delta_T",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Energy conservation MP_mono: at fixed wall heat flux q'' and inlet " +
                    "temperature, ΔT = q''·P_h·L/(G·A_xs·c_p) is inversely proportional to " +
                    "mass flux G — higher flow strictly decreases the outlet temperature rise.",
                MrFamily: "Subchannel.Scaling.MassFlux"),
            SampleCaseRelativePath: Path.Combine("subchannel_1d", "sample", "pwr_channel.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchSubchannel",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/boundary/mass_flux") },
            AssertionTypeCode: "less",
            EquationKey: "navier-stokes");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "subchannel-heat-flux-linearity",
                DisplayName: "1D subchannel — ScaleHeatFlux (linearity in q'')",
                SutName: "subchannel-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "delta_T",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Energy conservation MP_mono (linearity in heat input): doubling the wall " +
                    "heat flux q'' must strictly increase ΔT (in fact, by the same factor) " +
                    "because energy balance is linear in q'' at constant flow.",
                MrFamily: "Subchannel.Scaling.HeatFlux"),
            SampleCaseRelativePath: Path.Combine("subchannel_1d", "sample", "pwr_channel.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchSubchannel",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/boundary/heat_flux") },
            AssertionTypeCode: "greater",
            EquationKey: "navier-stokes");

        // S8-P4: 1D diffusion SUT + diffusion 方程接入
        yield return new MrBlueprint(
            new MrSummary(
                Id: "diffusion-source-linearity",
                DisplayName: "1D diffusion — ScaleSource (linearity)",
                SutName: "diffusion-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "phi_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Linearity MP_mono: the steady-state diffusion equation -D·φ″ + Σ_a·φ = S " +
                    "is linear in the source S. Scaling S by factor > 1 must strictly increase " +
                    "the peak flux φ_max (in fact, by the same factor).",
                MrFamily: "Diffusion.Scaling.Source"),
            SampleCaseRelativePath: Path.Combine("diffusion_1d", "sample", "slab.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDiffusion1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/source/strength") },
            AssertionTypeCode: "greater",
            EquationKey: "diffusion");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "diffusion-mesh-richardson",
                DisplayName: "1D diffusion — MeshRichardson (FD convergence)",
                SutName: "diffusion-1d",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "phi_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mesh-refinement Richardson convergence MP_conv: doubling num_points (halving " +
                    "the FD spacing dx) must leave φ_max within FD truncation tolerance — the " +
                    "second-order scheme is already at the fine-mesh plateau.",
                MrFamily: "Diffusion.Convergence.Mesh"),
            SampleCaseRelativePath: Path.Combine("diffusion_1d", "sample", "slab.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDiffusion1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/geometry/num_points") },
            AssertionTypeCode: "approx",
            EquationKey: "diffusion",
            // FD 2 阶 O(dx²)；网格加密后 phi_max 差 ~1e-4 量级（已 plateau）；ToleranceRel=1e-3 → 充裕
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));
    }

    private sealed record MrBlueprint(
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
    private sealed record MrTransformStep(
        string TransformationName,
        string TargetFieldPath);
}

/// <summary>
/// launcher 内部 MR 目录的对外快照,导入 v2 实体表时用。
/// 字段是 <see cref="LauncherCatalogV2Importer"/> 真正用得到的子集
/// (不含 PythonExecutable / WorkRootName / Timeout 等纯运行时配置)。
/// </summary>
internal sealed record MrCatalogEntry(
    MrSummary Mr,
    string SampleCaseRelativePath,
    string RunnerScriptPath,
    string InputParserScriptPath,
    string OutputParserScriptPath,
    string AssertionTypeCode,
    string PrimaryTransformationName,
    string EquationKey);
