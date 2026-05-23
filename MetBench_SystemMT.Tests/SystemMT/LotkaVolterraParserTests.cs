using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// P2 contract tests — Lotka-Volterra SUT 的 v2 pipeline 兼容 parser
/// (`lotka_volterra_input_parser.py` / `lotka_volterra_output_parser.py`)。
/// </summary>
public sealed class LotkaVolterraParserTests
{
    private static string InputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "lotka_volterra", "lotka_volterra_input_parser.py");
    private static string OutputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "lotka_volterra", "lotka_volterra_output_parser.py");
    private static string SamplePath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "lotka_volterra", "sample", "classic.json");

    [Fact]
    public void InputParser_parse_returns_expected_top_level_keys()
    {
        var stdout = PythonScriptRunner.Run(InputParserPath(), "parse", "--input", SamplePath());
        using var doc = JsonDocument.Parse(stdout);
        var initial = doc.RootElement.GetProperty("initial");
        Assert.Equal(10.0, initial.GetProperty("prey").GetDouble());
        Assert.Equal(10.0, initial.GetProperty("predator").GetDouble());
        Assert.Equal(0.4, doc.RootElement.GetProperty("params").GetProperty("gamma").GetDouble());
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
              "mean_prey": 4.0, "mean_predator": 2.75,
              "peak_prey": 20.0, "peak_predator": 15.0,
              "prey_final": 8.0, "predator_final": 3.0,
              "num_steps": 20000, "t_final": 100.0
            }
            """);

        var stdout = PythonScriptRunner.Run(OutputParserPath(), "parse", "--output-file", fakeOut);
        using var doc = JsonDocument.Parse(stdout);
        var values = doc.RootElement.GetProperty("values");
        Assert.Equal(4.0, values.GetProperty("mean_prey").GetDouble());
        Assert.Equal(20000.0, values.GetProperty("num_steps").GetDouble());
        Assert.Equal("lotka_volterra",
            doc.RootElement.GetProperty("metadata").GetProperty("program").GetString());
    }

    private static string MakeWorkDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "MetBenchLotkaVolterraParserTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }
}
