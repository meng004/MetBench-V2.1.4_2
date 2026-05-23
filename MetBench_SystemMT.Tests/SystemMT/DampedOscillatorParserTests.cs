using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// P2 contract tests — damped-oscillator SUT 的 v2 pipeline 兼容 parser
/// (`damped_oscillator_input_parser.py` / `damped_oscillator_output_parser.py`)。
/// </summary>
public sealed class DampedOscillatorParserTests
{
    private static string InputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "damped_oscillator", "damped_oscillator_input_parser.py");
    private static string OutputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "damped_oscillator", "damped_oscillator_output_parser.py");
    private static string SamplePath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "damped_oscillator", "sample", "underdamped.json");

    [Fact]
    public void InputParser_parse_returns_expected_top_level_keys()
    {
        var stdout = PythonScriptRunner.Run(InputParserPath(), "parse", "--input", SamplePath());
        using var doc = JsonDocument.Parse(stdout);
        var initial = doc.RootElement.GetProperty("initial");
        Assert.Equal(1.0, initial.GetProperty("x0").GetDouble());
        Assert.Equal(0.0, initial.GetProperty("v0").GetDouble());
        Assert.Equal(2.0, doc.RootElement.GetProperty("params").GetProperty("omega").GetDouble());
    }

    [Fact]
    public void InputParser_round_trip_preserves_dict()
    {
        var work = MakeWorkDir();
        var dictFile = Path.Combine(work, "dict.json");
        var outFile = Path.Combine(work, "out.json");

        var first = PythonScriptRunner.Run(InputParserPath(), "parse", "--input", SamplePath());
        File.WriteAllText(dictFile, first);
        PythonScriptRunner.Run(InputParserPath(), "write", "--dict-file", dictFile, "--output", outFile);
        var second = PythonScriptRunner.Run(InputParserPath(), "parse", "--input", outFile);

        Assert.Equal(
            JsonDocument.Parse(first).RootElement.GetRawText(),
            JsonDocument.Parse(second).RootElement.GetRawText());
    }

    [Fact]
    public void OutputParser_parse_emits_values_and_metadata()
    {
        var work = MakeWorkDir();
        var fakeOut = Path.Combine(work, "result.json");
        File.WriteAllText(fakeOut, """
            {
              "x_final": 0.1, "v_final": -0.05, "max_abs_displacement": 0.95,
              "energy_final": 0.1, "num_steps": 4000, "t_final": 20.0
            }
            """);

        var stdout = PythonScriptRunner.Run(OutputParserPath(), "parse", "--output-file", fakeOut);
        using var doc = JsonDocument.Parse(stdout);
        var values = doc.RootElement.GetProperty("values");
        Assert.Equal(0.95, values.GetProperty("max_abs_displacement").GetDouble());
        Assert.Equal(4000.0, values.GetProperty("num_steps").GetDouble());
        Assert.Equal("damped_oscillator",
            doc.RootElement.GetProperty("metadata").GetProperty("program").GetString());
    }

    private static string MakeWorkDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "MetBenchDampedOscillatorParserTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }
}
