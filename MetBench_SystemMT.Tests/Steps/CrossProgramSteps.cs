using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

/// <summary>
/// Step bindings for the cross-program neutron-transport feature
/// (<see cref="MetBench_SystemMT.Tests"/>/Features/CrossProgramNeutronTransportMrs.feature).
/// Same MR transformation name ("ScaleNuSigmaF" / "ScaleFuelSigmaA") flows into
/// both OpenMOC and OpenMC via the W2 pipeline.
/// </summary>
[Binding]
public sealed class CrossProgramSteps
{
    private string _solver = string.Empty;
    private string _sourceInputPath = string.Empty;
    private string _workRoot = string.Empty;
    private string _transformationName = string.Empty;
    private string _factor = string.Empty;
    private PipelineOutcome? _outcome;

    [Given("a pin-cell neutron-transport source case from {string} for solver {string}")]
    public async Task GivenPinCellSourceCaseForSolver(string assetRelativePath, string solver)
    {
        Skip.IfNot(SolverImportable(solver),
            $"{solver} is not importable from the resolved Python; skipping cross-program scenario.");

        _solver = solver;
        var assetRoot = TestAssetPaths.AssetRoot();
        var sampleSource = Path.Combine(assetRoot, assetRelativePath);
        Assert.True(File.Exists(sampleSource), $"sample case missing: {sampleSource}");

        _workRoot = Path.Combine(Path.GetTempPath(), $"MetBenchCrossProgramBdd-{solver}", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workRoot);

        _sourceInputPath = Path.Combine(_workRoot, "source.in.json");
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
        Assert.Equal(_solver, solver);
        Assert.NotEmpty(_transformationName);
        Assert.NotEmpty(_factor);

        var (w2TransformName, targetFieldPath, assertionTypeCode, python, timeoutSeconds) =
            ResolveSolverProfile(solver, _transformationName);

        var assetRoot = TestAssetPaths.AssetRoot();
        var solverDir = Path.Combine(assetRoot, solver);

        var ctx = new PipelineContext(
            MrCode: $"bdd-{solver}-{_transformationName}",
            TransformationName: w2TransformName,
            AssertionTypeCode: assertionTypeCode,
            ValueName: "k_eff",
            TargetFieldPath: targetFieldPath,
            PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string> { ["factor"] = _factor },
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: solver,
            SourceCasePath: _sourceInputPath,
            WorkingDirectory: _workRoot,
            InputParserCommand: $"\"{python}\" \"{Path.Combine(solverDir, $"{solver}_input_parser.py")}\"",
            OutputParserCommand: $"\"{python}\" \"{Path.Combine(solverDir, $"{solver}_output_parser.py")}\"",
            RunnerCommand: $"\"{python}\" \"{Path.Combine(solverDir, $"{solver}_runner.py")}\"",
            TimeoutSeconds: timeoutSeconds,
            CatalogVersionSha: string.Empty,
            SutVersionSnapshot: string.Empty,
            MetbenchVersion: string.Empty,
            TriggeredBy: "bdd");

        _outcome = await new SystemMtPipeline().ExecuteAsync(ctx, cancellationToken: CancellationToken.None);
    }

    [Then("the follow-up k_eff should be strictly greater than the source k_eff")]
    public void ThenFollowUpKEffGreater()
    {
        Assert.NotNull(_outcome);
        Assert.True(_outcome!.AssertionResult?.Passed ?? false, _outcome.AssertionResult?.FailureReason ?? _outcome.ErrorMessage);
        var srcKeff = _outcome.SourceMetrics!["k_eff"];
        var fuKeff = _outcome.FollowupMetrics!["k_eff"];
        Assert.True(fuKeff > srcKeff,
            $"follow-up k_eff ({fuKeff}) not strictly greater than source k_eff ({srcKeff}) for solver '{_solver}'");
    }

    [Then("the follow-up k_eff should be strictly less than the source k_eff")]
    public void ThenFollowUpKEffLess()
    {
        Assert.NotNull(_outcome);
        Assert.True(_outcome!.AssertionResult?.Passed ?? false, _outcome.AssertionResult?.FailureReason ?? _outcome.ErrorMessage);
        var srcKeff = _outcome.SourceMetrics!["k_eff"];
        var fuKeff = _outcome.FollowupMetrics!["k_eff"];
        Assert.True(fuKeff < srcKeff,
            $"follow-up k_eff ({fuKeff}) not strictly less than source k_eff ({srcKeff}) for solver '{_solver}'");
    }

    private static bool SolverImportable(string solver) => solver switch
    {
        "openmoc" => OpenMocTestPaths.OpenMocImportable(),
        "openmc" => OpenMcTestPaths.OpenMcImportable(),
        _ => false,
    };

    private static (string w2TransformName, string targetFieldPath,
                    string assertionTypeCode, string python, int timeoutSeconds)
        ResolveSolverProfile(string solver, string transformationName)
    {
        var (w2Transform, targetPath, assertCode) = transformationName switch
        {
            "ScaleNuSigmaF"    => ("ScaleField",           "/materials/fuel/nu_sigma_f", "greater"),
            "ScaleFuelSigmaA"  => ("ScaleFuelAbsorption",  "/materials/fuel",            "less"),
            _ => throw new InvalidOperationException($"unknown MR transformation: {transformationName}"),
        };

        var (python, timeoutSeconds) = solver switch
        {
            "openmoc" => (OpenMocTestPaths.OpenMocPython(), 120),
            "openmc"  => (OpenMcTestPaths.OpenMcPython(),   300),
            _ => throw new InvalidOperationException($"unknown solver: {solver}"),
        };

        return (w2Transform, targetPath, assertCode, python, timeoutSeconds);
    }
}
