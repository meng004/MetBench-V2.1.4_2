using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class OpenMocPinCellSigmaASteps
{
    private string _sourceInputPath = string.Empty;
    private string _workRoot = string.Empty;
    private readonly Dictionary<string, string> _params = new();
    private PipelineOutcome? _outcome;

    [Given("an OpenMOC pin-cell sigma_a source case from {string}")]
    public async Task GivenAnOpenMocSigmaASourceCase(string assetRelativePath)
    {
        Skip.IfNot(
            OpenMocTestPaths.OpenMocImportable(),
            "OpenMOC is not importable from the resolved Python. Set METBENCH_OPENMOC_PYTHON " +
            "or run `.claude/web-setup.sh` to install OpenMOC into /opt/openmoc-venv.");

        var assetRoot = TestAssetPaths.AssetRoot();
        var sampleSource = Path.Combine(assetRoot, assetRelativePath);
        Assert.True(File.Exists(sampleSource), $"sample case missing: {sampleSource}");

        _workRoot = Path.Combine(Path.GetTempPath(), "MetBenchOpenMocSigmaABdd", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workRoot);

        _sourceInputPath = Path.Combine(_workRoot, "source.in.json");
        await File.WriteAllTextAsync(_sourceInputPath, await File.ReadAllTextAsync(sampleSource), CancellationToken.None);
    }

    [Given("the OpenMOC sigma_a transformation {string} with parameter {string} set to {string}")]
    public void GivenTheSigmaATransformation(string name, string parameterName, string parameterValue)
    {
        _ = name; // "ScaleFuelSigmaA" 鈫?mapped to ScaleFuelAbsorption in When step
        _params[parameterName] = parameterValue;
    }

    [When("I run source and the generated follow-up through OpenMOC for sigma_a")]
    public async Task WhenIRunThroughOpenMocSigmaA()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var openmocPython = OpenMocTestPaths.OpenMocPython();
        var openmocDir = Path.Combine(assetRoot, "openmoc");

        var ctx = new PipelineContext(
            MrCode: "bdd-openmoc-sigma-a",
            TransformationName: "ScaleFuelAbsorption",
            AssertionTypeCode: "less",
            ValueName: "k_eff",
            TargetFieldPath: "/materials/fuel",
            PathSyntax: "json-pointer",
            Parameters: _params,
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: "openmoc",
            SourceCasePath: _sourceInputPath,
            WorkingDirectory: _workRoot,
            InputParserInvocation: new ProcessInvocation(openmocPython, new[] { Path.Combine(openmocDir, "openmoc_input_parser.py") }),
            OutputParserInvocation: new ProcessInvocation(openmocPython, new[] { Path.Combine(openmocDir, "openmoc_output_parser.py") }),
            RunnerInvocation: new ProcessInvocation(openmocPython, new[] { Path.Combine(openmocDir, "openmoc_runner.py") }),
            TimeoutSeconds: 120,
            CatalogVersionSha: string.Empty,
            SutVersionSnapshot: string.Empty,
            MetbenchVersion: string.Empty,
            TriggeredBy: "bdd");

        _outcome = await new SystemMtPipeline().ExecuteAsync(ctx, cancellationToken: CancellationToken.None);
    }

    [Then("the OpenMOC parsed value {string} of the generated follow-up should be less than the source")]
    public void ThenTheOpenMocParsedValueShouldBeLess(string valueName)
    {
        Assert.NotNull(_outcome);
        Assert.Equal("k_eff", valueName);
        Assert.True(_outcome!.FinalStatus is PipelineStatus.Ok or PipelineStatus.Anomaly,
            $"pipeline failed: {_outcome.ErrorMessage}");
        Assert.True(_outcome.AssertionResult?.Passed ?? false,
            _outcome.AssertionResult?.FailureReason ?? "assertion result null");
        Assert.True((_outcome.SourceMetrics?["converged"] ?? 0.0) >= 0.5,
            $"source did not converge: converged={_outcome.SourceMetrics?["converged"]}");
        Assert.True((_outcome.FollowupMetrics?["converged"] ?? 0.0) >= 0.5,
            $"follow-up did not converge: converged={_outcome.FollowupMetrics?["converged"]}");
    }

    [Then("the OpenMOC follow-up k_eff should be at most {double} times the source k_eff")]
    public void ThenFollowUpKeffRatioCeiling(double maximumRatio)
    {
        Assert.NotNull(_outcome);
        var sourceKeff = _outcome!.SourceMetrics!["k_eff"];
        var followUpKeff = _outcome.FollowupMetrics!["k_eff"];
        Assert.True(sourceKeff > 0, $"source k_eff must be positive, got {sourceKeff}");
        var ratio = followUpKeff / sourceKeff;
        Assert.True(ratio <= maximumRatio,
            $"follow-up/source k_eff ratio {ratio:F4} above ceiling {maximumRatio} (source={sourceKeff}, follow-up={followUpKeff})");
    }
}
