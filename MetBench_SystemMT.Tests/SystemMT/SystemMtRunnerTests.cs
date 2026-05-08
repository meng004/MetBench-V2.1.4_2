using MetBench_BLL;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class SystemMtRunnerTests
{
    [Fact]
    public async Task RunAsync_executes_source_and_followup_and_asserts_mr()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(root, "source");
        var followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(followUpDir);

        var sourceInput = Path.Combine(sourceDir, "input.txt");
        var followUpInput = Path.Combine(followUpDir, "input.txt");
        await File.WriteAllTextAsync(sourceInput, "3", CancellationToken.None);
        await File.WriteAllTextAsync(followUpInput, "9", CancellationToken.None);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = SystemMtTask.WithFollowUpCase(
            program,
            new SystemMtCase("source", sourceInput, sourceDir, Path.Combine(sourceDir, "output.txt")),
            new SystemMtCase("follow-up", followUpInput, followUpDir, Path.Combine(followUpDir, "output.txt")),
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion());

        var result = await runner.RunAsync(task, "result", CancellationToken.None);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(3, result.Assertion.SourceValue);
        Assert.Equal(9, result.Assertion.FollowUpValue);
    }
}
