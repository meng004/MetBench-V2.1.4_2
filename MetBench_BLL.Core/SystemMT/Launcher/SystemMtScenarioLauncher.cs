using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Production implementation of <see cref="ISystemMtScenarioLauncher"/>.
/// Owns the scenario registry and routes scenario ids to the correct
/// SystemMtRunner construction. Persists every run via the injected
/// <see cref="ISystemMtResultRepository"/>.
/// </summary>
/// <remarks>
/// The scenario list is hard-coded here on purpose: each scenario binds a
/// specific MR + transformation + assertion + value-name + sample-case
/// quartet that has been validated end-to-end in the BDD test suite. Adding a
/// new scenario should be done by extending <see cref="BuildScenarios"/>
/// after the corresponding adapter and assertion are landed.
/// </remarks>
public sealed class SystemMtScenarioLauncher : ISystemMtScenarioLauncher
{
    private readonly LauncherOptions _options;
    private readonly ISystemMtResultRepository _repository;
    private readonly IReadOnlyDictionary<string, ScenarioBlueprint> _scenarios;

    public SystemMtScenarioLauncher(LauncherOptions options, ISystemMtResultRepository repository)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _scenarios = BuildScenarios(options).ToDictionary(s => s.Descriptor.Id, StringComparer.Ordinal);
    }

    public Task<IReadOnlyList<ScenarioDescriptor>> ListAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ScenarioDescriptor> descriptors = _scenarios.Values
            .Select(s => s.Descriptor)
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(descriptors);
    }

    public async Task<ScenarioRunResult> RunAsync(
        string scenarioId,
        IReadOnlyDictionary<string, string>? parameterOverrides = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("Scenario id is required", nameof(scenarioId));
        }
        if (!_scenarios.TryGetValue(scenarioId, out var blueprint))
        {
            throw new ArgumentException($"Unknown scenario id: '{scenarioId}'", nameof(scenarioId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new Dictionary<string, string>(blueprint.Descriptor.DefaultParameters, StringComparer.Ordinal);
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
                $"Scenario '{scenarioId}' sample case not found at {sampleSource}", sampleSource);
        }
        var sampleContent = await File.ReadAllTextAsync(sampleSource, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(sourceInputPath, sampleContent, cancellationToken).ConfigureAwait(false);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            blueprint.Descriptor.SutName,
            blueprint.PythonExecutable,
            $"{blueprint.RunnerScriptPath} --input {{input}} --output {{output}}",
            blueprint.OutputAdapterScriptPath);

        var transformation = new MrTransformation(
            blueprint.Descriptor.TransformationName,
            parameters);

        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", sourceInputPath, sourceDir, Path.Combine(sourceDir, "output.json")),
            followUpCaseName: "follow-up",
            followUpInputPath: followUpInputPath,
            followUpWorkingDirectory: followUpDir,
            followUpOutputPath: Path.Combine(followUpDir, "output.json"),
            transformation,
            blueprint.Descriptor.AssertionName,
            blueprint.Timeout);

        var assertions = new IMrAssertion[]
        {
            new GreaterThanAssertion(),
            new LessThanAssertion(),
        };

        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(blueprint.PythonExecutable),
            assertions,
            new InputGenerator(
                new PythonInputAdapter(blueprint.PythonExecutable),
                blueprint.InputAdapterScriptPath));

        var result = await runner.RunAsync(task, blueprint.Descriptor.ValueName, cancellationToken).ConfigureAwait(false);

        var recordId = await _repository.SaveAsync(blueprint.Descriptor.DisplayName, result, cancellationToken).ConfigureAwait(false);

        return new ScenarioRunResult(
            RecordId: recordId,
            ScenarioId: scenarioId,
            Passed: result.Passed,
            FailureReason: result.FailureReason ?? string.Empty,
            ValueName: result.Assertion.ValueName,
            SourceValue: result.Assertion.SourceValue,
            FollowUpValue: result.Assertion.FollowUpValue,
            SourceElapsed: result.SourceRun.Elapsed,
            FollowUpElapsed: result.FollowUpRun.Elapsed);
    }

    private static IEnumerable<ScenarioBlueprint> BuildScenarios(LauncherOptions options)
    {
        yield return new ScenarioBlueprint(
            new ScenarioDescriptor(
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
                    "neutron-transport solution."),
            SampleCaseRelativePath: Path.Combine("openmoc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_adapter.py"),
            PythonExecutable: options.OpenMocPython,
            WorkRootName: "MetBenchOpenMocNuSigmaF",
            Timeout: TimeSpan.FromMinutes(2));

        yield return new ScenarioBlueprint(
            new ScenarioDescriptor(
                Id: "openmoc-pincell-sigma-a",
                DisplayName: "OpenMOC pin-cell — ScaleFuelSigmaA (k_eff decreases)",
                SutName: "openmoc",
                TransformationName: "ScaleFuelSigmaA",
                AssertionName: "LessThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Scaling the fuel material's absorption cross section by factor > 1 must " +
                    "monotonically decrease the dominant eigenvalue k_eff."),
            SampleCaseRelativePath: Path.Combine("openmoc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_adapter_sigma_a.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_adapter.py"),
            PythonExecutable: options.OpenMocPython,
            WorkRootName: "MetBenchOpenMocSigmaA",
            Timeout: TimeSpan.FromMinutes(2));

        yield return new ScenarioBlueprint(
            new ScenarioDescriptor(
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
                    "t_final by the same factor."),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchHeatEq",
            Timeout: TimeSpan.FromSeconds(60));
    }

    private sealed record ScenarioBlueprint(
        ScenarioDescriptor Descriptor,
        string SampleCaseRelativePath,
        string RunnerScriptPath,
        string InputAdapterScriptPath,
        string OutputAdapterScriptPath,
        string PythonExecutable,
        string WorkRootName,
        TimeSpan Timeout);
}
