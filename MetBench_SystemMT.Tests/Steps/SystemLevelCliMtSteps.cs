using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class SystemLevelCliMtSteps
{
    private readonly Dictionary<string, SystemMtCase> _cases = new(StringComparer.OrdinalIgnoreCase);
    private SystemMtResult? _result;

    [Given("a system MT case named {string} with input file {string}")]
    public async Task GivenASystemMtCaseNamedWithInputFile(string caseName, string inputFile)
    {
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMtBdd", Guid.NewGuid().ToString("N"));
        var caseDir = Path.Combine(root, caseName);
        Directory.CreateDirectory(caseDir);

        var inputPath = Path.Combine(caseDir, inputFile);
        var outputPath = Path.Combine(caseDir, "output.txt");
        var value = caseName.Equals("source", StringComparison.OrdinalIgnoreCase) ? "3" : "9";
        await File.WriteAllTextAsync(inputPath, value, CancellationToken.None);

        _cases[caseName] = new SystemMtCase(caseName, inputPath, caseDir, outputPath);
    }

    [When("I run both cases with program profile {string}")]
    public async Task WhenIRunBothCasesWithProgramProfile(string profileName)
    {
        Assert.Equal("example-cli", profileName);

        var assetRoot = TestAssetPaths.AssetRoot();
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = new SystemMtTask(
            program,
            _cases["source"],
            _cases["follow-up"],
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion());

        _result = await runner.RunAsync(task, "result", CancellationToken.None);
    }

    [Then("the parsed output value {string} of {string} should be greater than {string}")]
    public void ThenTheParsedOutputValueOfShouldBeGreaterThan(
        string valueName,
        string followUpCaseName,
        string sourceCaseName)
    {
        Assert.NotNull(_result);
        Assert.Equal("result", valueName);
        Assert.Equal("follow-up", followUpCaseName);
        Assert.Equal("source", sourceCaseName);
        Assert.True(_result.Passed, _result.FailureReason);
    }
}
