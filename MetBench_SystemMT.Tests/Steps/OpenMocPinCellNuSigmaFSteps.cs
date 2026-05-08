using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class OpenMocPinCellNuSigmaFSteps
{
    private string _sourceInputPath = string.Empty;
    private string _sourceDir = string.Empty;
    private string _followUpInputPath = string.Empty;
    private string _followUpDir = string.Empty;
    private MrTransformation? _transformation;
    private SystemMtResult? _result;

    [Given("an OpenMOC pin-cell source case from {string}")]
    public async Task GivenAnOpenMocPinCellSourceCase(string assetRelativePath)
    {
        Skip.IfNot(
            OpenMocTestPaths.OpenMocImportable(),
            "OpenMOC is not importable from the resolved Python. Set METBENCH_OPENMOC_PYTHON " +
            "or run `.claude/web-setup.sh` to install OpenMOC into /opt/openmoc-venv.");

        var assetRoot = TestAssetPaths.AssetRoot();
        var sampleSource = Path.Combine(assetRoot, assetRelativePath);
        Assert.True(File.Exists(sampleSource), $"sample case missing: {sampleSource}");

        var workRoot = Path.Combine(Path.GetTempPath(), "MetBenchOpenMocBdd", Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(workRoot, "source");
        _followUpDir = Path.Combine(workRoot, "followup");
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_followUpDir);

        _sourceInputPath = Path.Combine(_sourceDir, "input.json");
        _followUpInputPath = Path.Combine(_followUpDir, "input.json");
        await File.WriteAllTextAsync(_sourceInputPath, await File.ReadAllTextAsync(sampleSource), CancellationToken.None);
    }

    [Given("an OpenMOC MR transformation {string} with parameter {string} set to {string}")]
    public void GivenTheMrTransformation(string name, string parameterName, string parameterValue)
    {
        _transformation = new MrTransformation(
            name,
            new Dictionary<string, string> { [parameterName] = parameterValue });
    }

    [When("I run source and the generated follow-up through OpenMOC")]
    public async Task WhenIRunThroughOpenMoc()
    {
        Assert.NotNull(_transformation);

        var assetRoot = TestAssetPaths.AssetRoot();
        var openmocPython = OpenMocTestPaths.OpenMocPython();
        var runnerScript = Path.Combine(assetRoot, "openmoc", "openmoc_runner.py");
        var inputAdapterScript = Path.Combine(assetRoot, "openmoc", "openmoc_input_adapter.py");
        var outputAdapterScript = Path.Combine(assetRoot, "openmoc", "openmoc_output_adapter.py");

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "openmoc-pincell",
            openmocPython,
            $"{runnerScript} --input {{input}} --output {{output}}",
            outputAdapterScript);

        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", _sourceInputPath, _sourceDir, Path.Combine(_sourceDir, "output.json")),
            followUpCaseName: "follow-up",
            followUpInputPath: _followUpInputPath,
            followUpWorkingDirectory: _followUpDir,
            followUpOutputPath: Path.Combine(_followUpDir, "output.json"),
            _transformation!,
            "GreaterThan",
            TimeSpan.FromMinutes(2));

        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(openmocPython),
            new IMrAssertion[] { new GreaterThanAssertion() },
            new InputGenerator(
                new PythonInputAdapter(openmocPython),
                inputAdapterScript));

        _result = await runner.RunAsync(task, "k_eff", CancellationToken.None);
    }

    [Then("the OpenMOC parsed value {string} of the generated follow-up should be greater than the source")]
    public void ThenTheOpenMocParsedValueShouldBeGreater(string valueName)
    {
        Assert.NotNull(_result);
        Assert.Equal("k_eff", valueName);
        Assert.True(_result!.Passed, _result.FailureReason);
        Assert.NotNull(_result.InputGeneration);
        Assert.True(_result.InputGeneration!.Succeeded);
        // Output adapter encodes converged as exactly 1.0 or 0.0; tolerate
        // any double >= 0.5 to keep this assert robust against future
        // schema tweaks (e.g. emitting an iteration-fraction).
        Assert.True(_result.SourceOutput.Values["converged"] >= 0.5,
            $"source did not converge: converged={_result.SourceOutput.Values["converged"]}");
        Assert.True(_result.FollowUpOutput.Values["converged"] >= 0.5,
            $"follow-up did not converge: converged={_result.FollowUpOutput.Values["converged"]}");
    }

    [Then("the OpenMOC follow-up k_eff should be at least {double} times the source k_eff")]
    public void ThenFollowUpKeffRatioFloor(double minimumRatio)
    {
        Assert.NotNull(_result);
        var sourceKeff = _result!.SourceOutput.Values["k_eff"];
        var followUpKeff = _result.FollowUpOutput.Values["k_eff"];
        Assert.True(sourceKeff > 0, $"source k_eff must be positive, got {sourceKeff}");
        var ratio = followUpKeff / sourceKeff;
        Assert.True(ratio >= minimumRatio,
            $"follow-up/source k_eff ratio {ratio:F4} below floor {minimumRatio} (source={sourceKeff}, follow-up={followUpKeff})");
    }
}
