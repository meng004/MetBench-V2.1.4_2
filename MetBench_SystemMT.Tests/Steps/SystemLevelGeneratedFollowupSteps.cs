using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class SystemLevelGeneratedFollowupSteps
{
    private string? _sourceInputPath;
    private string? _followUpInputPath;
    private string _sourceDir = string.Empty;
    private string _followUpDir = string.Empty;
    private MrTransformation? _transformation;
    private SystemMtResult? _result;

    [Given("a source MT case with input value {string}")]
    public async Task GivenASourceMtCaseWithInputValue(string sourceValue)
    {
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMtBdd", Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(root, "source");
        _followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_followUpDir);

        _sourceInputPath = Path.Combine(_sourceDir, "input.txt");
        _followUpInputPath = Path.Combine(_followUpDir, "input.txt");
        await File.WriteAllTextAsync(_sourceInputPath, sourceValue, CancellationToken.None);
    }

    [Given("the MR transformation {string} with parameter {string} set to {string}")]
    public void GivenTheMrTransformationWithParameter(string name, string parameterName, string parameterValue)
    {
        _transformation = new MrTransformation(
            name,
            new Dictionary<string, string> { [parameterName] = parameterValue });
    }

    [When("I run source and the generated follow-up with program profile {string}")]
    public async Task WhenIRunSourceAndTheGeneratedFollowUp(string profileName)
    {
        Assert.Equal("example-cli", profileName);
        Assert.NotNull(_sourceInputPath);
        Assert.NotNull(_followUpInputPath);
        Assert.NotNull(_transformation);

        var assetRoot = TestAssetPaths.AssetRoot();
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", _sourceInputPath!, _sourceDir, Path.Combine(_sourceDir, "output.txt")),
            followUpCaseName: "follow-up",
            followUpInputPath: _followUpInputPath!,
            followUpWorkingDirectory: _followUpDir,
            followUpOutputPath: Path.Combine(_followUpDir, "output.txt"),
            _transformation!,
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion(),
            new InputGenerator(
                new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
                Path.Combine(assetRoot, "example_output_adapter.py")));

        _result = await runner.RunAsync(task, "result", CancellationToken.None);
    }

    [Then("the parsed output value {string} of the generated follow-up should be greater than the source")]
    public void ThenTheParsedOutputValueShouldBeGreater(string valueName)
    {
        Assert.NotNull(_result);
        Assert.Equal("result", valueName);
        Assert.True(_result!.Passed, _result.FailureReason);
        Assert.NotNull(_result.InputGeneration);
        Assert.True(_result.InputGeneration!.Succeeded);
    }
}
