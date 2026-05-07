using MetBench_BLL;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class SystemMtModelTests
{
    [Fact]
    public void SystemProgram_exposes_program_type_and_data()
    {
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            "python",
            "example_cli.py --input {input} --output {output}",
            "example_output_adapter.py");

        Assert.Equal("System", program.ProgramType);
        Assert.Equal("example-cli", program.ProfileName);
        Assert.Equal("python", program.ExecutablePath);
        Assert.Equal("example_cli.py --input {input} --output {output}", program.ArgumentTemplate);
        Assert.Equal("example_output_adapter.py", program.OutputAdapterPath);
    }

    [Fact]
    public void SystemMtCase_rejects_empty_case_name()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new SystemMtCase("", "input.txt", "work", "output.txt"));

        Assert.Contains("CaseName", error.Message);
    }

    [Fact]
    public void SystemMtTask_requires_different_source_and_followup_names()
    {
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            "python",
            "example_cli.py --input {input} --output {output}",
            "adapter.py");
        var source = new SystemMtCase("same", "source.txt", "work/source", "out.txt");
        var followUp = new SystemMtCase("same", "followup.txt", "work/followup", "out.txt");

        var error = Assert.Throws<ArgumentException>(() =>
            new SystemMtTask(program, source, followUp, "GreaterThan", TimeSpan.FromSeconds(5)));

        Assert.Contains("Source and follow-up case names must be different", error.Message);
    }
}
