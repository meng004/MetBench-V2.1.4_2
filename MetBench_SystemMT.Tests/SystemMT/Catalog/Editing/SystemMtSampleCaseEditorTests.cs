using MetBench_BLL.SystemMT.Catalog.Editing;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Editing;

public sealed class SystemMtSampleCaseEditorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "metbench-sample-editor-" + Guid.NewGuid().ToString("N"));

    public SystemMtSampleCaseEditorTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ListSamples_returns_json_files_sorted_ordinal()
    {
        SeedSut("heat_equation", "heat-scale");
        WriteSample("heat_equation", "case-b.json", "{}");
        WriteSample("heat_equation", "case-a.json", "{}");
        var editor = new SystemMtSampleCaseEditor(_root);

        var samples = editor.ListSamples("heat_equation");

        // base.json from SeedSut + a + b
        Assert.Equal(new[] { "base.json", "case-a.json", "case-b.json" }, samples.Select(s => s.FileName).ToArray());
        Assert.All(samples, s => Assert.Equal("heat_equation", s.SutId));
    }

    [Theory]
    [InlineData("BAD NAME.json")] // space + uppercase
    [InlineData("UPPERCASE.json")]
    [InlineData("missing-extension")]
    [InlineData("-leading-dash.json")]
    public void ValidateDraft_rejects_invalid_file_names(string fileName)
    {
        SeedSut("heat_equation", "heat-scale");
        var editor = new SystemMtSampleCaseEditor(_root);

        var result = editor.ValidateDraft("heat_equation", fileName, "{}");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Invalid sample file name", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_malformed_json_body()
    {
        SeedSut("heat_equation", "heat-scale");
        var editor = new SystemMtSampleCaseEditor(_root);

        var result = editor.ValidateDraft("heat_equation", "draft.json", "{not json");

        Assert.False(result.Success);
        // JsonException's message text varies; just assert the result is a failure.
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void SaveDraft_writes_atomically_and_round_trips()
    {
        SeedSut("heat_equation", "heat-scale");
        var editor = new SystemMtSampleCaseEditor(_root);

        var result = editor.SaveDraft("heat_equation", "fresh.json", "{ \"k\": 1 }");

        Assert.True(result.Success, string.Join(" | ", result.Errors));
        var roundTrip = editor.LoadSample("heat_equation", "fresh.json");
        Assert.Equal("{ \"k\": 1 }", roundTrip);
        // No leftover tmp file
        var samplePath = Path.Combine(_root, "heat_equation", "sample", "fresh.json");
        Assert.False(File.Exists(samplePath + ".tmp"));
    }

    [Fact]
    public void Delete_fail_closed_when_manifest_mr_references_the_sample()
    {
        // heat-scale MR references sample/base.json by default (see SeedSut).
        SeedSut("heat_equation", "heat-scale");
        var editor = new SystemMtSampleCaseEditor(_root);

        var result = editor.Delete("heat_equation", "base.json");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("heat-scale", StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(_root, "heat_equation", "sample", "base.json")));
    }

    [Fact]
    public void Delete_succeeds_when_no_mr_references_the_sample()
    {
        SeedSut("heat_equation", "heat-scale");
        WriteSample("heat_equation", "orphan.json", "{}");
        var editor = new SystemMtSampleCaseEditor(_root);

        var result = editor.Delete("heat_equation", "orphan.json");

        Assert.True(result.Success, string.Join(" | ", result.Errors));
        Assert.False(File.Exists(Path.Combine(_root, "heat_equation", "sample", "orphan.json")));
    }

    private void SeedSut(string sutId, string mrId)
    {
        var dir = Path.Combine(_root, sutId);
        Directory.CreateDirectory(Path.Combine(dir, "sample"));
        File.WriteAllText(Path.Combine(dir, "sample", "base.json"), "{ \"factor\": 1 }");
        File.WriteAllText(Path.Combine(dir, "catalog.json"), $$"""
        {
          "sut_name": "{{sutId}}",
          "program": {
            "program_name": "{{sutId}}",
            "program_type": "Num",
            "runner_script_relative_path": "runner.py",
            "python_executable_kind": "system"
          },
          "mrs": [
            {
              "mr_id": "{{mrId}}",
              "sut_name": "{{sutId}}",
              "transformation_name": "ScaleAmplitude",
              "assertion_type_code": "greater",
              "assertion_name": "greater",
              "value_name": "max_temperature",
              "default_parameters": { "factor": "2" },
              "transform_steps": [
                { "transformation_name": "ScaleAmplitude", "target_field_path": "initial.amplitude" }
              ],
              "sample_case_relative_path": "sample/base.json"
            }
          ]
        }
        """);
    }

    private void WriteSample(string sutId, string fileName, string jsonText)
    {
        var dir = Path.Combine(_root, sutId, "sample");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, fileName), jsonText);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
