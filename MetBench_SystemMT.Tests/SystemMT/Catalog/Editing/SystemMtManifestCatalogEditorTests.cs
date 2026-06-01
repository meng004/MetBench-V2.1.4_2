using MetBench_BLL.SystemMT.Catalog.Editing;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Editing;

public sealed class SystemMtManifestCatalogEditorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "metbench-manifest-editor-" + Guid.NewGuid().ToString("N"));

    public SystemMtManifestCatalogEditorTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ListManifests_returns_catalog_json_files_with_sut_ids()
    {
        WriteManifest("heat_equation", "heat-scale");
        WriteManifest("openmoc", "openmoc-scale");

        var editor = new SystemMtManifestCatalogEditor(_root);

        var manifests = editor.ListManifests();

        Assert.Equal(new[] { "heat_equation", "openmoc" }, manifests.Select(m => m.SutId).ToArray());
        Assert.All(manifests, m => Assert.EndsWith("catalog.json", m.ManifestPath, StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_blank_mr_id_without_writing_file()
    {
        WriteManifest("heat_equation", "heat-scale");
        var manifestPath = Path.Combine(_root, "heat_equation", "catalog.json");
        var originalJson = File.ReadAllText(manifestPath);
        var editor = new SystemMtManifestCatalogEditor(_root);
        var draft = SystemMtMrBindingDraft.NewForSut("heat_equation") with
        {
            MrId = "",
            DisplayName = "new display",
            TransformationName = "ScaleAmplitude",
            AssertionTypeCode = "greater",
            AssertionName = "greater",
            ValueName = "max_temperature",
            WorkRootName = "new-work",
            TimeoutSeconds = 30,
            TransformStepName = "ScaleAmplitude",
            TransformTargetFieldPath = "initial.amplitude",
            MetaPatternRationale = "Linearity MR: scaling the source should scale the solution.",
            TransformationSemantics = "Scale source input field by factor.",
            ObservableSummary = "Compare selected scalar or field residual after source/follow-up runs.",
            PredicateSummary = "Binary comparison with configured tolerance.",
            ToleranceSummary = "No tolerance for strict ordinal relation.",
            Applicability = "Only valid when the SUT exposes the named metric.",
            FailureMeaning = "MR violation indicates inconsistent response to the declared transformation."
        };

        var result = editor.ValidateDraft("heat_equation", draft);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("MrId", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(originalJson, File.ReadAllText(manifestPath));
    }

    [Fact]
    public void Load_rejects_path_traversal_sut_id()
    {
        WriteManifest("heat_equation", "heat-scale");
        var editor = new SystemMtManifestCatalogEditor(_root);

        var ex = Assert.Throws<ArgumentException>(() => editor.Load("../heat_equation"));

        Assert.Contains("Invalid SUT id", ex.Message);
    }

    [Theory]
    [InlineData("nested/heat_equation")]
    [InlineData("nested\\heat_equation")]
    public void Load_rejects_sut_id_with_directory_separator(string sutId)
    {
        WriteManifest("heat_equation", "heat-scale");
        var editor = new SystemMtManifestCatalogEditor(_root);

        var ex = Assert.Throws<ArgumentException>(() => editor.Load(sutId));

        Assert.Contains("Invalid SUT id", ex.Message);
    }

    [Fact]
    public void SaveDraft_adds_new_binding_when_validation_passes()
    {
        WriteManifest("heat_equation", "heat-scale");
        var editor = new SystemMtManifestCatalogEditor(_root);
        var draft = SystemMtMrBindingDraft.NewForSut("heat_equation") with
        {
            MrId = "heat-scale-draft",
            DisplayName = "draft display",
            Description = "draft description",
            TransformationName = "ScaleAmplitude",
            AssertionTypeCode = "greater",
            AssertionName = "greater",
            ValueName = "max_temperature",
            EquationKey = "fourier",
            MetaPattern = "Mono",
            SampleCaseRelativePath = "sample/base.json",
            WorkRootName = "heat-scale-draft",
            TimeoutSeconds = 30,
            Factor = "2",
            TransformStepName = "ScaleAmplitude",
            TransformTargetFieldPath = "initial.amplitude",
            MetaPatternRationale = "Linearity MR: scaling the source should scale the solution.",
            TransformationSemantics = "Scale source input field by factor.",
            ObservableSummary = "Compare selected scalar or field residual after source/follow-up runs.",
            PredicateSummary = "Binary comparison with configured tolerance.",
            ToleranceSummary = "No tolerance for strict ordinal relation.",
            Applicability = "Only valid when the SUT exposes the named metric.",
            FailureMeaning = "MR violation indicates inconsistent response to the declared transformation."
        };

        var result = editor.SaveDraft("heat_equation", draft);
        var reloaded = editor.Load("heat_equation");

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        var saved = Assert.Single(reloaded.Mrs, mr => mr.MrId == "heat-scale-draft");
        Assert.Equal(2, reloaded.Mrs.Count);
        Assert.NotNull(saved.ExplanationProfile);
        Assert.Equal("Linearity MR: scaling the source should scale the solution.", saved.ExplanationProfile!.MetaPatternRationale);
        Assert.Equal("Scale source input field by factor.", saved.ExplanationProfile.TransformationSemantics);
        Assert.Equal("Compare selected scalar or field residual after source/follow-up runs.", saved.ExplanationProfile.ObservableSummary);
        Assert.Equal("Binary comparison with configured tolerance.", saved.ExplanationProfile.PredicateSummary);
        Assert.Equal("No tolerance for strict ordinal relation.", saved.ExplanationProfile.ToleranceSummary);
        Assert.Equal("Only valid when the SUT exposes the named metric.", saved.ExplanationProfile.Applicability);
        Assert.Equal("MR violation indicates inconsistent response to the declared transformation.", saved.ExplanationProfile.FailureMeaning);
    }

    private void WriteManifest(string sutId, string mrId)
    {
        var dir = Path.Combine(_root, sutId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "catalog.json"), $$"""
        {
          "sut_name": "{{sutId}}",
          "program": {
            "program_kind": "{{sutId}}",
            "program_name": "{{sutId}}",
            "program_type": "Num",
            "equation_key": "fourier",
            "runner_script_relative_path": "runner.py",
            "input_adapter_script_relative_path": "input.py",
            "output_adapter_script_relative_path": "output.py",
            "input_parser_script_relative_path": "input_parser.py",
            "output_parser_script_relative_path": "output_parser.py",
            "python_executable_kind": "system"
          },
          "mrs": [
            {
              "mr_id": "{{mrId}}",
              "sut_name": "{{sutId}}",
              "display_name": "{{mrId}} display",
              "description": "{{mrId}} description",
              "transformation_name": "ScaleAmplitude",
              "assertion_type_code": "greater",
              "assertion_name": "greater",
              "value_name": "max_temperature",
              "default_parameters": { "factor": "2" },
              "transform_steps": [
                { "transformation_name": "ScaleAmplitude", "target_field_path": "initial.amplitude" }
              ],
              "equation_key": "fourier",
              "meta_pattern": "Mono",
              "explanation_profile": {
                "meta_pattern_rationale": "Linearity MR: scaling the source should scale the solution.",
                "transformation_semantics": "Scale source input field by factor.",
                "observable_summary": "Compare selected scalar or field residual after source/follow-up runs.",
                "predicate_summary": "Binary comparison with configured tolerance.",
                "tolerance_summary": "No tolerance for strict ordinal relation.",
                "applicability": "Only valid when the SUT exposes the named metric.",
                "failure_meaning": "MR violation indicates inconsistent response to the declared transformation."
              },
              "sample_case_relative_path": "sample/base.json",
              "work_root_name": "{{mrId}}",
              "timeout_seconds": 30
            }
          ]
        }
        """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
