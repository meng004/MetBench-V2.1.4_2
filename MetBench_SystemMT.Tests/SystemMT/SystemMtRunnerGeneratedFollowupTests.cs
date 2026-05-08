using MetBench_BLL;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class SystemMtRunnerGeneratedFollowupTests
{
    [Fact]
    public async Task RunAsync_generates_followup_input_when_only_transformation_is_provided()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(root, "source");
        var followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(followUpDir);

        var sourceInput = Path.Combine(sourceDir, "input.txt");
        await File.WriteAllTextAsync(sourceInput, "4", CancellationToken.None);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", sourceInput, sourceDir, Path.Combine(sourceDir, "output.txt")),
            followUpCaseName: "follow-up",
            followUpInputPath: Path.Combine(followUpDir, "input.txt"),
            followUpWorkingDirectory: followUpDir,
            followUpOutputPath: Path.Combine(followUpDir, "output.txt"),
            new MrTransformation("ScalarMultiply", new Dictionary<string, string> { ["multiplier"] = "3" }),
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion(),
            new InputGenerator(
                new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
                Path.Combine(assetRoot, "example_output_adapter.py")));

        var result = await runner.RunAsync(task, "result", CancellationToken.None);

        Assert.True(result.Passed, result.FailureReason);
        Assert.NotNull(result.InputGeneration);
        Assert.True(result.InputGeneration!.Succeeded);
        Assert.Equal(4, result.Assertion.SourceValue);
        Assert.Equal(12, result.Assertion.FollowUpValue);
    }

    [Fact]
    public async Task RunAsync_returns_failure_when_input_generation_fails()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(root, "source");
        var followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(followUpDir);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase(
                "source",
                Path.Combine(sourceDir, "missing-source.txt"),
                sourceDir,
                Path.Combine(sourceDir, "output.txt")),
            followUpCaseName: "follow-up",
            followUpInputPath: Path.Combine(followUpDir, "input.txt"),
            followUpWorkingDirectory: followUpDir,
            followUpOutputPath: Path.Combine(followUpDir, "output.txt"),
            new MrTransformation("ScalarMultiply", new Dictionary<string, string> { ["multiplier"] = "3" }),
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion(),
            new InputGenerator(
                new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
                Path.Combine(assetRoot, "example_output_adapter.py")));

        var result = await runner.RunAsync(task, "result", CancellationToken.None);

        Assert.False(result.Passed);
        Assert.NotNull(result.InputGeneration);
        Assert.False(result.InputGeneration!.Succeeded);
        Assert.Contains("source input file does not exist", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }
}
