using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

/// <summary>
/// Step bindings for the cross-program neutron-transport feature
/// (<see cref="MetBench_SystemMT.Tests"/>/Features/CrossProgramNeutronTransportMrs.feature).
/// Same MR transformation name ("ScaleNuSigmaF" / "ScaleFuelSigmaA") flows into
/// both OpenMOC and OpenMC. Solver selection in the Scenario Outline picks
/// which adapter scripts + python interpreter the runner constructs with;
/// every other moving part is identical across solvers.
/// </summary>
[Binding]
public sealed class CrossProgramSteps
{
    private string _solver = string.Empty;
    private string _sourceInputPath = string.Empty;
    private string _sourceDir = string.Empty;
    private string _followUpInputPath = string.Empty;
    private string _followUpDir = string.Empty;
    private string _transformationName = string.Empty;
    private string _factor = string.Empty;
    private SystemMtResult? _result;

    [Given("a pin-cell neutron-transport source case from {string} for solver {string}")]
    public async Task GivenPinCellSourceCaseForSolver(string assetRelativePath, string solver)
    {
        Skip.IfNot(SolverImportable(solver),
            $"{solver} is not importable from the resolved Python; skipping cross-program scenario.");

        _solver = solver;
        var assetRoot = TestAssetPaths.AssetRoot();
        var sampleSource = Path.Combine(assetRoot, assetRelativePath);
        Assert.True(File.Exists(sampleSource), $"sample case missing: {sampleSource}");

        var workRoot = Path.Combine(Path.GetTempPath(), $"MetBenchCrossProgramBdd-{solver}", Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(workRoot, "source");
        _followUpDir = Path.Combine(workRoot, "followup");
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_followUpDir);

        _sourceInputPath = Path.Combine(_sourceDir, "input.json");
        _followUpInputPath = Path.Combine(_followUpDir, "input.json");
        await File.WriteAllTextAsync(_sourceInputPath, await File.ReadAllTextAsync(sampleSource), CancellationToken.None);
    }

    [Given("an MR transformation {string} with factor {string}")]
    public void GivenMrTransformation(string name, string factor)
    {
        _transformationName = name;
        _factor = factor;
    }

    [When("I run source and the generated follow-up through the {string} solver")]
    public async Task WhenIRunThroughSolver(string solver)
    {
        Assert.Equal(_solver, solver);  // Background sanity: Given and When agree on solver
        Assert.NotEmpty(_transformationName);
        Assert.NotEmpty(_factor);

        var (runnerScript, inputAdapter, outputAdapter, python, assertionName, timeout) = ResolveSolverPaths(solver, _transformationName);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            solver,
            python,
            $"{runnerScript} --input {{input}} --output {{output}}",
            outputAdapter);

        var transformation = new MrTransformation(
            _transformationName,
            new Dictionary<string, string> { ["factor"] = _factor });

        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", _sourceInputPath, _sourceDir, Path.Combine(_sourceDir, "output.json")),
            followUpCaseName: "follow-up",
            followUpInputPath: _followUpInputPath,
            followUpWorkingDirectory: _followUpDir,
            followUpOutputPath: Path.Combine(_followUpDir, "output.json"),
            transformation,
            assertionName,
            timeout);

        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(python),
            new IMrAssertion[] { new GreaterThanAssertion(), new LessThanAssertion() },
            new InputGenerator(
                new PythonInputAdapter(python),
                inputAdapter));

        _result = await runner.RunAsync(task, "k_eff", CancellationToken.None);
    }

    [Then("the follow-up k_eff should be strictly greater than the source k_eff")]
    public void ThenFollowUpKEffGreater()
    {
        Assert.NotNull(_result);
        Assert.True(_result!.Passed, _result.FailureReason);
        var srcKeff = _result.SourceOutput.Values["k_eff"];
        var fuKeff = _result.FollowUpOutput.Values["k_eff"];
        Assert.True(fuKeff > srcKeff,
            $"follow-up k_eff ({fuKeff}) not strictly greater than source k_eff ({srcKeff}) for solver '{_solver}'");
    }

    [Then("the follow-up k_eff should be strictly less than the source k_eff")]
    public void ThenFollowUpKEffLess()
    {
        Assert.NotNull(_result);
        Assert.True(_result!.Passed, _result.FailureReason);
        var srcKeff = _result.SourceOutput.Values["k_eff"];
        var fuKeff = _result.FollowUpOutput.Values["k_eff"];
        Assert.True(fuKeff < srcKeff,
            $"follow-up k_eff ({fuKeff}) not strictly less than source k_eff ({srcKeff}) for solver '{_solver}'");
    }

    private static bool SolverImportable(string solver) => solver switch
    {
        "openmoc" => OpenMocTestPaths.OpenMocImportable(),
        "openmc" => OpenMcTestPaths.OpenMcImportable(),
        _ => false,
    };

    private static (string runner, string inputAdapter, string outputAdapter,
                    string python, string assertionName, TimeSpan timeout)
        ResolveSolverPaths(string solver, string transformationName)
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var assertionName = transformationName switch
        {
            "ScaleNuSigmaF" => "GreaterThan",
            "ScaleFuelSigmaA" => "LessThan",
            _ => throw new InvalidOperationException($"unknown MR transformation: {transformationName}"),
        };

        return solver switch
        {
            "openmoc" => (
                runner: Path.Combine(assetRoot, "openmoc", "openmoc_runner.py"),
                inputAdapter: Path.Combine(assetRoot, "openmoc",
                    transformationName == "ScaleNuSigmaF"
                        ? "openmoc_input_adapter.py"
                        : "openmoc_input_adapter_sigma_a.py"),
                outputAdapter: Path.Combine(assetRoot, "openmoc", "openmoc_output_adapter.py"),
                python: OpenMocTestPaths.OpenMocPython(),
                assertionName: assertionName,
                timeout: TimeSpan.FromMinutes(2)),

            "openmc" => (
                runner: Path.Combine(assetRoot, "openmc", "openmc_runner.py"),
                inputAdapter: Path.Combine(assetRoot, "openmc",
                    transformationName == "ScaleNuSigmaF"
                        ? "openmc_input_adapter.py"
                        : "openmc_input_adapter_sigma_a.py"),
                outputAdapter: Path.Combine(assetRoot, "openmc", "openmc_output_adapter.py"),
                python: OpenMcTestPaths.OpenMcPython(),
                assertionName: assertionName,
                timeout: TimeSpan.FromMinutes(5)),

            _ => throw new InvalidOperationException($"unknown solver: {solver}"),
        };
    }
}
