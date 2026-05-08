using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class ProjectileRangeSteps
{
    private readonly Dictionary<string, SystemMtCase> _cases = new(StringComparer.OrdinalIgnoreCase);
    private SystemMtResult? _result;

    [Given("a projectile MT case named {string} with input {string}")]
    public async Task GivenAProjectileMtCase(string caseName, string inputLine)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "MetBenchProjectileBdd",
            Guid.NewGuid().ToString("N"));
        var caseDir = Path.Combine(root, caseName);
        Directory.CreateDirectory(caseDir);

        var inputPath = Path.Combine(caseDir, "input.txt");
        var outputPath = Path.Combine(caseDir, "output.txt");
        await File.WriteAllTextAsync(inputPath, inputLine, CancellationToken.None);

        _cases[caseName] = new SystemMtCase(caseName, inputPath, caseDir, outputPath);
    }

    [When("I run both projectile cases")]
    public async Task WhenIRunBothProjectileCases()
    {
        var assetRoot = Path.Combine(TestAssetPaths.AssetRoot(), "projectile");
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "projectile",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "projectile.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "projectile_output_adapter.py"));
        var task = SystemMtTask.WithFollowUpCase(
            program,
            _cases["source"],
            _cases["follow-up"],
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion());

        _result = await runner.RunAsync(task, "range", CancellationToken.None);
    }

    [Then("the projectile parsed value {string} of {string} should exceed {string}")]
    public void ThenProjectileParsedValueShouldExceed(
        string valueName,
        string followUpCaseName,
        string sourceCaseName)
    {
        Assert.NotNull(_result);
        Assert.Equal("range", valueName);
        Assert.Equal("follow-up", followUpCaseName);
        Assert.Equal("source", sourceCaseName);
        Assert.True(_result.Passed, _result.FailureReason);
    }

    [Then("the follow-up range should be approximately {int} times the source range")]
    public void ThenFollowUpRangeRatio(int expectedRatio)
    {
        Assert.NotNull(_result);
        var sourceRange = _result!.SourceOutput.Values["range"];
        var followUpRange = _result.FollowUpOutput.Values["range"];
        var ratio = followUpRange / sourceRange;
        Assert.InRange(ratio, expectedRatio - 0.01, expectedRatio + 0.01);
    }
}
