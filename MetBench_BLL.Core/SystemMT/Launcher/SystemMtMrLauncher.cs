using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Production implementation of <see cref="ISystemMtMrLauncher"/>.
/// Owns the MR registry and routes MR ids to the correct
/// SystemMtRunner construction. Persists every run via the injected
/// <see cref="ISystemMtResultRepository"/>.
/// </summary>
/// <remarks>
/// The MR list is hard-coded here on purpose: each MR binds a
/// specific MR + transformation + assertion + value-name + sample-case
/// quartet that has been validated end-to-end in the BDD test suite. Adding a
/// new MR should be done by extending <see cref="BuildMrCatalog"/>
/// after the corresponding adapter and assertion are landed.
/// </remarks>
public sealed class SystemMtMrLauncher : ISystemMtMrLauncher
{
    private readonly LauncherOptions _options;
    private readonly ISystemMtResultRepository _repository;
    private readonly IAnomalyService _anomalyService;
    private readonly AnomalySeverityThresholds _severityThresholds;
    private readonly IReadOnlyDictionary<string, MrBlueprint> _mrCatalog;

    public SystemMtMrLauncher(
        LauncherOptions options,
        ISystemMtResultRepository repository,
        IAnomalyService anomalyService,
        AnomalySeverityThresholds? severityThresholds = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _anomalyService = anomalyService ?? throw new ArgumentNullException(nameof(anomalyService));
        _severityThresholds = severityThresholds ?? AnomalySeverityThresholds.Default;
        _mrCatalog = BuildMrCatalog(options).ToDictionary(s => s.Mr.Id, StringComparer.Ordinal);
    }

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

        var workRoot = Path.Combine(Path.GetTempPath(), blueprint.WorkRootName, Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(workRoot, "source");
        var followUpDir = Path.Combine(workRoot, "followup");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(followUpDir);

        var sourceInputPath = Path.Combine(sourceDir, "input.json");
        var followUpInputPath = Path.Combine(followUpDir, "input.json");

        var sampleSource = Path.Combine(_options.SutRoot, blueprint.SampleCaseRelativePath);
        if (!File.Exists(sampleSource))
        {
            throw new FileNotFoundException(
                $"MR '{mrId}' sample case not found at {sampleSource}", sampleSource);
        }
        var sampleContent = await File.ReadAllTextAsync(sampleSource, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(sourceInputPath, sampleContent, cancellationToken).ConfigureAwait(false);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            blueprint.Mr.SutName,
            blueprint.PythonExecutable,
            $"{blueprint.RunnerScriptPath} --input {{input}} --output {{output}}",
            blueprint.OutputAdapterScriptPath);

        var transformation = new MrTransformation(
            blueprint.Mr.TransformationName,
            parameters);

        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", sourceInputPath, sourceDir, Path.Combine(sourceDir, "output.json")),
            followUpCaseName: "follow-up",
            followUpInputPath: followUpInputPath,
            followUpWorkingDirectory: followUpDir,
            followUpOutputPath: Path.Combine(followUpDir, "output.json"),
            transformation,
            blueprint.Mr.AssertionName,
            blueprint.Timeout);

        var assertions = new IMrAssertion[]
        {
            new GreaterThanAssertion(),
            new LessThanAssertion(),
            new ApproxEqualAssertion(),
        };

        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(blueprint.PythonExecutable),
            assertions,
            new InputGenerator(
                new PythonInputAdapter(blueprint.PythonExecutable),
                blueprint.InputAdapterScriptPath));

        var result = await runner.RunAsync(task, blueprint.Mr.ValueName, cancellationToken).ConfigureAwait(false);

        var recordId = await _repository.SaveAsync(blueprint.Mr.DisplayName, result, cancellationToken).ConfigureAwait(false);

        await RecordAnomalyIfFailedAsync(blueprint.Mr.DisplayName, recordId, result, cancellationToken).ConfigureAwait(false);

        return new MrRunResult(
            RecordId: recordId,
            MrId: mrId,
            Passed: result.Passed,
            FailureReason: result.FailureReason ?? string.Empty,
            ValueName: result.Assertion.ValueName,
            SourceValue: result.Assertion.SourceValue,
            FollowUpValue: result.Assertion.FollowUpValue,
            SourceElapsed: result.SourceRun.Elapsed,
            FollowUpElapsed: result.FollowUpRun.Elapsed);
    }

    /// <summary>
    /// UC-B7 — 失败 run 自动建一条 Anomaly。internal 暴露给 V2Anomaly 测试做 wiring 验证
    /// （InternalsVisibleTo MetBench_SystemMT.Tests）。
    /// </summary>
    internal async Task RecordAnomalyIfFailedAsync(
        string mrName,
        string recordId,
        SystemMtResult result,
        CancellationToken cancellationToken)
    {
        if (result.Passed) return;
        var severity = AnomalyClassifier.ClassifySeverity(result, _severityThresholds);
        var category = AnomalyClassifier.ClassifyCategory(result);
        await _anomalyService.RecordAnomalyAsync(
            mrName, recordId, severity, category, cancellationToken).ConfigureAwait(false);
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
            TransformSteps: new[]
            {
                new MrTransformStep("ScaleField", "/initial/N_A"),
                new MrTransformStep("ScaleField", "/initial/N_B"),
                new MrTransformStep("ScaleField", "/initial/N_C"),
            },
            AssertionTypeCode: "greater");

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
        string AssertionTypeCode);

    /// <summary>v2 pipeline 的单步变换规格。多步在 launcher.RunAsync 内包 <c>CompositeTransform</c>。</summary>
    private sealed record MrTransformStep(
        string TransformationName,
        string TargetFieldPath);
}
