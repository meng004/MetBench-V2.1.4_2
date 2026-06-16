using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class SystemLevelGeneratedFollowupSteps
{
    private string? _sourceInputPath;
    private string _workRoot = string.Empty;
    private readonly Dictionary<string, string> _params = new();
    private PipelineOutcome? _outcome;

    [Given("a source MT case with input value {string}")]
    public async Task GivenASourceMtCaseWithInputValue(string sourceValue)
    {
        _workRoot = Path.Combine(Path.GetTempPath(), "MetBenchSystemMtBdd", Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(_workRoot, "source");
        Directory.CreateDirectory(sourceDir);

        _sourceInputPath = Path.Combine(sourceDir, "input.txt");
        await File.WriteAllTextAsync(_sourceInputPath, sourceValue, CancellationToken.None);
    }

    [Given("the MR transformation {string} with parameter {string} set to {string}")]
    public void GivenTheMrTransformationWithParameter(string name, string parameterName, string parameterValue)
    {
        _ = name; // "ScalarMultiply" 鈫?mapped to ScaleField at /value in When step
        _params[parameterName] = parameterValue;
    }

    [When("I run source and the generated follow-up with program profile {string}")]
    public async Task WhenIRunSourceAndTheGeneratedFollowUp(string profileName)
    {
        Assert.Equal("example-cli", profileName);
        Assert.NotNull(_sourceInputPath);

        var assetRoot = TestAssetPaths.AssetRoot();
        var python = TestAssetPaths.PythonExecutable();

        // "ScalarMultiply" with multiplier maps to ScaleField at /value with factor=multiplier
        var factor = _params.GetValueOrDefault("multiplier", "1");

        var ctx = new PipelineContext(
            MrCode: "bdd-example-cli-generated",
            TransformationName: "ScaleField",
            AssertionTypeCode: "greater",
            ValueName: "result",
            TargetFieldPath: "/value",
            PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string> { ["factor"] = factor },
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: "example-cli",
            SourceCasePath: _sourceInputPath!,
            WorkingDirectory: _workRoot,
            InputParserInvocation: new ProcessInvocation(python, new[] { Path.Combine(assetRoot, "example_cli_input_parser.py") }),
            OutputParserInvocation: new ProcessInvocation(python, new[] { Path.Combine(assetRoot, "example_cli_output_parser.py") }),
            RunnerInvocation: new ProcessInvocation(python, new[] { Path.Combine(assetRoot, "example_cli.py") }),
            TimeoutSeconds: 10,
            CatalogVersionSha: string.Empty,
            SutVersionSnapshot: string.Empty,
            MetbenchVersion: string.Empty,
            TriggeredBy: "bdd");

        _outcome = await new SystemMtPipeline().ExecuteAsync(ctx, cancellationToken: CancellationToken.None);
    }

    [Then("the parsed output value {string} of the generated follow-up should be greater than the source")]
    public void ThenTheParsedOutputValueShouldBeGreater(string valueName)
    {
        Assert.NotNull(_outcome);
        Assert.Equal("result", valueName);
        Assert.True(_outcome!.FinalStatus is PipelineStatus.Ok or PipelineStatus.Anomaly,
            $"pipeline failed: {_outcome.ErrorMessage}");
        Assert.True(_outcome.AssertionResult?.Passed ?? false,
            _outcome.AssertionResult?.FailureReason ?? "assertion result null");
    }
}
