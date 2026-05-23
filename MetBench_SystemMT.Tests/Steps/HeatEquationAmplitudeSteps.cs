using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class HeatEquationAmplitudeSteps
{
    private string _sourceInputPath = string.Empty;
    private string _workRoot = string.Empty;
    private readonly Dictionary<string, string> _params = new();
    private PipelineOutcome? _outcome;

    [Given("a heat-equation source case from {string}")]
    public async Task GivenAHeatEquationSourceCase(string assetRelativePath)
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var sampleSource = Path.Combine(assetRoot, assetRelativePath);
        Assert.True(File.Exists(sampleSource), $"sample case missing: {sampleSource}");

        _workRoot = Path.Combine(Path.GetTempPath(), "MetBenchHeatEqBdd", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workRoot);

        _sourceInputPath = Path.Combine(_workRoot, "source.in.json");
        await File.WriteAllTextAsync(_sourceInputPath, await File.ReadAllTextAsync(sampleSource), CancellationToken.None);
    }

    [Given("a heat-equation transformation {string} with parameter {string} set to {string}")]
    public void GivenTheHeatEquationTransformation(string name, string parameterName, string parameterValue)
    {
        _ = name; // "ScaleAmplitude" → mapped to ScaleField in When step
        _params[parameterName] = parameterValue;
    }

    [When("I run source and the generated follow-up through the heat-equation solver")]
    public async Task WhenIRunThroughTheHeatEquationSolver()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var python = TestAssetPaths.PythonExecutable();
        var heatDir = Path.Combine(assetRoot, "heat_equation");

        var ctx = new PipelineContext(
            MrCode: "bdd-heat-equation-amplitude",
            TransformationName: "ScaleField",
            AssertionTypeCode: "greater",
            ValueName: "max_u",
            TargetFieldPath: "/initial/amplitude",
            PathSyntax: "json-pointer",
            Parameters: _params,
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: "heat-equation",
            SourceCasePath: _sourceInputPath,
            WorkingDirectory: _workRoot,
            InputParserCommand: $"\"{python}\" \"{Path.Combine(heatDir, "heat_equation_input_parser.py")}\"",
            OutputParserCommand: $"\"{python}\" \"{Path.Combine(heatDir, "heat_equation_output_parser.py")}\"",
            RunnerCommand: $"\"{python}\" \"{Path.Combine(heatDir, "heat_equation.py")}\"",
            TimeoutSeconds: 30,
            CatalogVersionSha: string.Empty,
            SutVersionSnapshot: string.Empty,
            MetbenchVersion: string.Empty,
            TriggeredBy: "bdd");

        _outcome = await new SystemMtPipeline().ExecuteAsync(ctx, cancellationToken: CancellationToken.None);
    }

    [Then("the heat-equation parsed value {string} of the generated follow-up should be greater than the source")]
    public void ThenParsedValueShouldBeGreater(string valueName)
    {
        Assert.NotNull(_outcome);
        Assert.Equal("max_u", valueName);
        Assert.True(_outcome!.FinalStatus is PipelineStatus.Ok or PipelineStatus.Anomaly,
            $"pipeline failed: {_outcome.ErrorMessage}");
        Assert.True(_outcome.AssertionResult?.Passed ?? false,
            _outcome.AssertionResult?.FailureReason ?? "assertion result null");
        Assert.Equal(0.0, _outcome.SourceMetrics?["stability_violated"] ?? 0.0);
        Assert.Equal(0.0, _outcome.FollowupMetrics?["stability_violated"] ?? 0.0);
    }

    [Then("the heat-equation follow-up max_u should be at least {double} times the source max_u")]
    public void ThenFollowUpMaxRatioFloor(double minimumRatio)
    {
        Assert.NotNull(_outcome);
        var sourceMax = _outcome!.SourceMetrics!["max_u"];
        var followUpMax = _outcome.FollowupMetrics!["max_u"];
        Assert.True(sourceMax > 0, $"source max_u must be positive, got {sourceMax}");
        var ratio = followUpMax / sourceMax;
        Assert.True(ratio >= minimumRatio,
            $"follow-up/source max_u ratio {ratio:F4} below floor {minimumRatio} (source={sourceMax}, follow-up={followUpMax})");
    }

    [Then("the heat-equation follow-up max_u should be at most {double} times the source max_u")]
    public void ThenFollowUpMaxRatioCeiling(double maximumRatio)
    {
        Assert.NotNull(_outcome);
        var sourceMax = _outcome!.SourceMetrics!["max_u"];
        var followUpMax = _outcome.FollowupMetrics!["max_u"];
        Assert.True(sourceMax > 0, $"source max_u must be positive, got {sourceMax}");
        var ratio = followUpMax / sourceMax;
        Assert.True(ratio <= maximumRatio,
            $"follow-up/source max_u ratio {ratio:F4} above ceiling {maximumRatio} (source={sourceMax}, follow-up={followUpMax})");
    }
}
