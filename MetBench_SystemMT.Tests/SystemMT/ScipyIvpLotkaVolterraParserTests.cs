using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// T3C-IVP parser contract tests for the SciPy <c>solve_ivp</c>-backed Lotka-Volterra SUT.
/// The parsers themselves are pure-Python (no scipy import), so they must run unconditionally
/// — only the runner needs SciPy. These tests pin input round-trip, output {values, metadata}
/// shape, and the new <c>num_eval_points</c> param surface.
/// </summary>
public sealed class ScipyIvpLotkaVolterraParserTests
{
    private static string InputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_input_parser.py");
    private static string OutputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_output_parser.py");
    private static string SamplePath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "scipy_ivp_lotka_volterra", "sample", "standard.json");

    [Fact]
    public void InputParser_parse_returns_expected_top_level_keys()
    {
        var stdout = PythonScriptRunner.Run(InputParserPath(), "parse", "--input", SamplePath());
        using var doc = JsonDocument.Parse(stdout);
        var initial = doc.RootElement.GetProperty("initial");
        Assert.Equal(10.0, initial.GetProperty("prey").GetDouble());
        Assert.Equal(10.0, initial.GetProperty("predator").GetDouble());
        var p = doc.RootElement.GetProperty("params");
        Assert.Equal(0.4, p.GetProperty("gamma").GetDouble());
        Assert.True(p.GetProperty("num_eval_points").GetInt32() >= 2,
            "sample must declare a num_eval_points >= 2 so the eval grid has refinement room");
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
              "num_eval_points": 1000, "t_final": 100.0
            }
            """);

        var stdout = PythonScriptRunner.Run(OutputParserPath(), "parse", "--output-file", fakeOut);
        using var doc = JsonDocument.Parse(stdout);
        var values = doc.RootElement.GetProperty("values");
        Assert.Equal(4.0, values.GetProperty("mean_prey").GetDouble());
        Assert.Equal(1000.0, values.GetProperty("num_eval_points").GetDouble());
        Assert.Equal("scipy_ivp_lotka_volterra",
            doc.RootElement.GetProperty("metadata").GetProperty("program").GetString());
    }

    private static string MakeWorkDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "MetBenchScipyIvpLvParserTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }
}
