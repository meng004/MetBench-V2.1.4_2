using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class HeatEquationAmplitudeSteps
{
    private string _sourceInputPath = string.Empty;
    private string _sourceDir = string.Empty;
    private string _followUpInputPath = string.Empty;
    private string _followUpDir = string.Empty;
    private MrTransformation? _transformation;
    private SystemMtResult? _result;

    [Given("a heat-equation source case from {string}")]
    public async Task GivenAHeatEquationSourceCase(string assetRelativePath)
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var sampleSource = Path.Combine(assetRoot, assetRelativePath);
        Assert.True(File.Exists(sampleSource), $"sample case missing: {sampleSource}");

        var workRoot = Path.Combine(Path.GetTempPath(), "MetBenchHeatEqBdd", Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(workRoot, "source");
        _followUpDir = Path.Combine(workRoot, "followup");
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_followUpDir);

        _sourceInputPath = Path.Combine(_sourceDir, "input.json");
        _followUpInputPath = Path.Combine(_followUpDir, "input.json");
        await File.WriteAllTextAsync(_sourceInputPath, await File.ReadAllTextAsync(sampleSource), CancellationToken.None);
    }

    [Given("a heat-equation transformation {string} with parameter {string} set to {string}")]
    public void GivenTheHeatEquationTransformation(string name, string parameterName, string parameterValue)
    {
        _transformation = new MrTransformation(
            name,
            new Dictionary<string, string> { [parameterName] = parameterValue });
    }

    [When("I run source and the generated follow-up through the heat-equation solver")]
    public async Task WhenIRunThroughTheHeatEquationSolver()
    {
        Assert.NotNull(_transformation);

        var assetRoot = TestAssetPaths.AssetRoot();
        var python = TestAssetPaths.PythonExecutable();
        var runnerScript = Path.Combine(assetRoot, "heat_equation", "heat_equation.py");
        var inputAdapterScript = Path.Combine(assetRoot, "heat_equation", "heat_equation_input_adapter.py");
        var outputAdapterScript = Path.Combine(assetRoot, "heat_equation", "heat_equation_output_adapter.py");

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "heat-equation-1d",
            python,
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
            TimeSpan.FromSeconds(30));

        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(python),
            new IMrAssertion[] { new GreaterThanAssertion() },
            new InputGenerator(
                new PythonInputAdapter(python),
                inputAdapterScript));

        _result = await runner.RunAsync(task, "max_u", CancellationToken.None);
    }

    [Then("the heat-equation parsed value {string} of the generated follow-up should be greater than the source")]
    public void ThenParsedValueShouldBeGreater(string valueName)
    {
        Assert.NotNull(_result);
        Assert.Equal("max_u", valueName);
        Assert.True(_result!.Passed, _result.FailureReason);
        Assert.NotNull(_result.InputGeneration);
        Assert.True(_result.InputGeneration!.Succeeded);
        Assert.Equal(0.0, _result.SourceOutput.Values["stability_violated"]);
        Assert.Equal(0.0, _result.FollowUpOutput.Values["stability_violated"]);
    }

    [Then("the heat-equation follow-up max_u should be at least {double} times the source max_u")]
    public void ThenFollowUpMaxRatioFloor(double minimumRatio)
    {
        Assert.NotNull(_result);
        var sourceMax = _result!.SourceOutput.Values["max_u"];
        var followUpMax = _result.FollowUpOutput.Values["max_u"];
        Assert.True(sourceMax > 0, $"source max_u must be positive, got {sourceMax}");
        var ratio = followUpMax / sourceMax;
        Assert.True(ratio >= minimumRatio,
            $"follow-up/source max_u ratio {ratio:F4} below floor {minimumRatio} (source={sourceMax}, follow-up={followUpMax})");
    }

    [Then("the heat-equation follow-up max_u should be at most {double} times the source max_u")]
    public void ThenFollowUpMaxRatioCeiling(double maximumRatio)
    {
        Assert.NotNull(_result);
        var sourceMax = _result!.SourceOutput.Values["max_u"];
        var followUpMax = _result.FollowUpOutput.Values["max_u"];
        Assert.True(sourceMax > 0, $"source max_u must be positive, got {sourceMax}");
        var ratio = followUpMax / sourceMax;
        Assert.True(ratio <= maximumRatio,
            $"follow-up/source max_u ratio {ratio:F4} above ceiling {maximumRatio} (source={sourceMax}, follow-up={followUpMax})");
    }
}
