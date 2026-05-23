using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// P2 contract tests — decay-chain SUT 的 v2 pipeline 兼容 parser
/// (`decay_chain_input_parser.py` / `decay_chain_output_parser.py`)。
/// 计划见 docs/superpowers/plans/2026-05-22-systemmt-engine-unification-plan.md。
/// </summary>
public sealed class DecayChainParserTests
{
    private static string InputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "decay_chain", "decay_chain_input_parser.py");
    private static string OutputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "decay_chain", "decay_chain_output_parser.py");
    private static string SamplePath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "decay_chain", "sample", "three_nuclide.json");

    [Fact]
    public void InputParser_parse_returns_expected_top_level_keys()
    {
        var stdout = PythonScriptRunner.Run(InputParserPath(), "parse", "--input", SamplePath());
        using var doc = JsonDocument.Parse(stdout);
        var initial = doc.RootElement.GetProperty("initial");
        Assert.Equal(1000.0, initial.GetProperty("N_A").GetDouble());
        Assert.Equal(0.0, initial.GetProperty("N_B").GetDouble());
        Assert.True(doc.RootElement.TryGetProperty("params", out _));
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
              "N_A_final": 6.7, "N_B_final": 3.4, "N_C_final": 989.9,
              "N_B_peak": 200.5, "total": 1000.0, "num_steps": 2000, "t_final": 10.0
            }
            """);

        var stdout = PythonScriptRunner.Run(OutputParserPath(), "parse", "--output-file", fakeOut);
        using var doc = JsonDocument.Parse(stdout);
        var values = doc.RootElement.GetProperty("values");
        Assert.Equal(989.9, values.GetProperty("N_C_final").GetDouble());
        Assert.Equal(2000.0, values.GetProperty("num_steps").GetDouble());
        Assert.Equal("decay_chain",
            doc.RootElement.GetProperty("metadata").GetProperty("program").GetString());
    }

    private static string MakeWorkDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "MetBenchDecayChainParserTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }
}
