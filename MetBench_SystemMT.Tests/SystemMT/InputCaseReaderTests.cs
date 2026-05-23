using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class InputCaseReaderTests : IDisposable
{
    private readonly string _dir;

    public InputCaseReaderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "MetBenchInputCase", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ReadSamples_flattens_nested_numeric_leaves_and_skips_non_numeric()
    {
        var src = Write("s.json",
            """{"initial":{"N_A":1000.0,"N_B":0.0},"params":{"lambda":0.5,"profile":"gaussian"}}""");

        var samples = InputCaseReader.ReadSamples(src, src);

        // N_A, N_B, lambda kept; profile (string) skipped.
        Assert.Equal(3, samples.Count);
        var byName = samples.ToDictionary(s => s.Name);
        Assert.Equal(1000.0, byName["initial.N_A"].SourceValue);
        Assert.Equal(0.0, byName["initial.N_B"].SourceValue);
        Assert.Equal(0.5, byName["params.lambda"].SourceValue);
        Assert.DoesNotContain(samples, s => s.Name.Contains("profile"));
    }

    [Fact]
    public void ReadSamples_indexes_array_elements()
    {
        var src = Write("a.json", """{"materials":{"sigma_t":[0.22,0.83]}}""");

        var samples = InputCaseReader.ReadSamples(src, src);
        var byName = samples.ToDictionary(s => s.Name);

        Assert.Equal(0.22, byName["materials.sigma_t.0"].SourceValue);
        Assert.Equal(0.83, byName["materials.sigma_t.1"].SourceValue);
    }

    [Fact]
    public void ReadSamples_pairs_source_and_followup_values_per_input()
    {
        var src = Write("src.json", """{"initial":{"x0":1.0,"v0":0.0}}""");
        var flw = Write("flw.json", """{"initial":{"x0":2.0,"v0":0.0}}""");

        var samples = InputCaseReader.ReadSamples(src, flw);

        var x0 = samples.Single(s => s.Name == "initial.x0");
        Assert.Equal(1.0, x0.SourceValue);
        Assert.Equal(2.0, x0.FollowUpValue);
        var v0 = samples.Single(s => s.Name == "initial.v0");
        Assert.Equal(0.0, v0.SourceValue);
        Assert.Equal(0.0, v0.FollowUpValue);
    }

    [Fact]
    public void ReadSamples_handles_root_scalar_input_files()
    {
        var src = Write("scalar-s.txt", "4");
        var flw = Write("scalar-f.txt", "12");

        var samples = InputCaseReader.ReadSamples(src, flw);

        var only = Assert.Single(samples);
        Assert.Equal(4.0, only.SourceValue);
        Assert.Equal(12.0, only.FollowUpValue);
    }

    [Fact]
    public void ReadSamples_returns_empty_when_a_file_is_missing()
    {
        var src = Write("present.json", """{"a":1.0}""");
        var missing = Path.Combine(_dir, "nope.json");

        Assert.Empty(InputCaseReader.ReadSamples(src, missing));
    }

    [Fact]
    public void ReadSamples_returns_empty_for_non_json_content()
    {
        var bad = Write("bad.txt", "this is not json at all");

        Assert.Empty(InputCaseReader.ReadSamples(bad, bad));
    }
}
