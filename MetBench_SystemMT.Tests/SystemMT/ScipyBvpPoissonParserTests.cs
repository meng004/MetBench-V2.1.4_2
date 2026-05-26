using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// T3C-BVP parser contract tests for the SciPy <c>solve_bvp</c>-backed 1D Poisson SUT.
/// The parsers themselves are pure-Python (no scipy import), so they must run unconditionally
/// — only the runner needs SciPy. These tests pin input round-trip and output {values, metadata}
/// shape parity with the pure-stdlib <c>SUT/poisson_1d/</c> counterpart.
/// </summary>
public sealed class ScipyBvpPoissonParserTests
{
    private static string InputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_input_parser.py");
    private static string OutputParserPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_output_parser.py");
    private static string SamplePath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "scipy_bvp_poisson_1d", "sample", "standard.json");

    [Fact]
    public void InputParser_parse_returns_expected_top_level_keys()
    {
        var stdout = PythonScriptRunner.Run(InputParserPath(), "parse", "--input", SamplePath());
        using var doc = JsonDocument.Parse(stdout);
        var geom = doc.RootElement.GetProperty("geometry");
        Assert.True(geom.GetProperty("length").GetDouble() > 0,
            "sample must declare a positive geometry.length");
        Assert.True(geom.GetProperty("num_points").GetInt32() >= 3,
            "sample must declare a geometry.num_points >= 3 (the solve_bvp seed mesh minimum)");
        Assert.Equal(1.0, doc.RootElement.GetProperty("source").GetProperty("strength").GetDouble());
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
              "u_max": 0.125, "u_center": 0.125, "u_integral": 0.0833,
              "num_points": 101, "L_length": 1.0
            }
            """);

        var stdout = PythonScriptRunner.Run(OutputParserPath(), "parse", "--output-file", fakeOut);
        using var doc = JsonDocument.Parse(stdout);
        var values = doc.RootElement.GetProperty("values");
        Assert.Equal(0.125, values.GetProperty("u_max").GetDouble());
        Assert.Equal("scipy_bvp_poisson_1d",
            doc.RootElement.GetProperty("metadata").GetProperty("program").GetString());
        Assert.Equal("101",
            doc.RootElement.GetProperty("metadata").GetProperty("num_points").GetString());
    }

    private static string MakeWorkDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "MetBenchScipyBvpPoissonParserTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }
}
