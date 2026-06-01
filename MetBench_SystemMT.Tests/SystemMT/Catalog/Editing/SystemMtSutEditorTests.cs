using System.Text.Json;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Catalog.Editing;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Editing;

public sealed class SystemMtSutEditorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "metbench-sut-editor-" + Guid.NewGuid().ToString("N"));

    public SystemMtSutEditorTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ListSuts_returns_descriptors_in_sut_id_order_with_program_summary()
    {
        WriteManifest("heat_equation", "heat-scale", programName: "heat-runner", equation: "Fourier");
        WriteManifest("openmoc", "openmoc-scale", programName: "openmoc", equation: "Boltzmann");

        var editor = new SystemMtSutEditor(_root);

        var suts = editor.ListSuts();

        Assert.Equal(new[] { "heat_equation", "openmoc" }, suts.Select(s => s.SutId).ToArray());
        Assert.Equal("heat-runner", suts[0].ProgramName);
        Assert.Equal("Fourier", suts[0].Equation);
        Assert.All(suts, s => Assert.EndsWith("catalog.json", s.ManifestPath, StringComparison.Ordinal));
    }

    [Fact]
    public void Load_returns_program_draft_with_all_ten_fields()
    {
        WriteManifest("heat_equation", "heat-scale", programName: "heat-runner", equation: "Fourier");
        var editor = new SystemMtSutEditor(_root);

        var draft = editor.Load("heat_equation");

        Assert.Equal("heat_equation", draft.SutName);
        Assert.Equal("heat-runner", draft.ProgramName);
        Assert.Equal("Fourier", draft.Equation);
        Assert.Equal("fourier", draft.EquationKey);
        Assert.Equal("Num", draft.ProgramType);
        Assert.Equal("runner.py", draft.RunnerScriptRelativePath);
        Assert.Equal("system", draft.PythonExecutableKind);
        Assert.Equal("Num", draft.ProfileProgramType);
        Assert.Equal("finite-difference", draft.SolverMethod);
        Assert.Equal("system", draft.RuntimeKey);
        Assert.Equal("JSON params with mesh and coefficient fields", draft.InputContract);
        Assert.Equal("JSON metrics consumed by typed verifier", draft.OutputContract);
        Assert.Equal("python runner under SUT/<sut>/", draft.Adapter);
        Assert.Equal("pure-stdlib", draft.DependencyRisk);
    }

    [Fact]
    public void ValidateDraft_rejects_runner_script_that_does_not_exist()
    {
        WriteManifest("heat_equation", "heat-scale");
        var editor = new SystemMtSutEditor(_root);
        var draft = editor.Load("heat_equation") with
        {
            RunnerScriptRelativePath = "missing_runner.py",
        };

        var result = editor.ValidateDraft("heat_equation", draft);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("missing_runner.py", StringComparison.Ordinal));
    }

    [Fact]
    public void SaveDraft_preserves_existing_mr_bindings_verbatim()
    {
        WriteManifest("heat_equation", "heat-scale");
        var editor = new SystemMtSutEditor(_root);
        var draft = editor.Load("heat_equation") with
        {
            Equation = "FourierUpdated",
            ProgramType = "MC",
            SolverMethod = "stochastic-transport",
            RuntimeKey = "openmc",
        };

        var result = editor.SaveDraft("heat_equation", draft);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        var reloaded = LoadCatalogJson("heat_equation");
        Assert.Equal("FourierUpdated", reloaded.Program!.Equation);
        Assert.Equal("MC", reloaded.Program.ProgramType);
        Assert.NotNull(reloaded.Profile);
        Assert.Equal("stochastic-transport", reloaded.Profile!.SolverMethod);
        Assert.Equal("openmc", reloaded.Profile.RuntimeKey);
        // Mrs section preserved
        Assert.Single(reloaded.Mrs);
        Assert.Equal("heat-scale", reloaded.Mrs[0].MrId);
    }

    [Fact]
    public void SaveDraft_creates_new_sut_directory_with_empty_mrs_array()
    {
        // Place a runner.py so the script-existence check passes for the new SUT.
        var newSutDir = Path.Combine(_root, "new_sut");
        Directory.CreateDirectory(newSutDir);
        File.WriteAllText(Path.Combine(newSutDir, "runner.py"), "# stub");

        var editor = new SystemMtSutEditor(_root);
        var draft = SystemMtSutProgramDraft.NewForSut("new_sut") with
        {
            RunnerScriptRelativePath = "runner.py",
            ProgramName = "new_sut",
            ProfileProgramType = "Num",
            SolverMethod = "finite-difference",
            RuntimeKey = "system",
            InputContract = "JSON params with mesh and coefficient fields",
            OutputContract = "JSON metrics consumed by typed verifier",
            Adapter = "python runner under SUT/<sut>/",
            DependencyRisk = "pure-stdlib",
        };

        var result = editor.SaveDraft("new_sut", draft);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        Assert.True(File.Exists(Path.Combine(newSutDir, "catalog.json")));
        var json = File.ReadAllText(Path.Combine(newSutDir, "catalog.json"));
        Assert.Contains("\"sut_name\": \"new_sut\"", json, StringComparison.Ordinal);
        Assert.Contains("\"profile\": {", json, StringComparison.Ordinal);
        Assert.Contains("\"solver_method\": \"finite-difference\"", json, StringComparison.Ordinal);
        Assert.Contains("\"mrs\": []", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../heat_equation")]
    [InlineData("nested/heat_equation")]
    [InlineData("nested\\heat_equation")]
    public void Load_rejects_path_traversal_and_separator_sut_ids(string sutId)
    {
        WriteManifest("heat_equation", "heat-scale");
        var editor = new SystemMtSutEditor(_root);

        var ex = Assert.Throws<ArgumentException>(() => editor.Load(sutId));

        Assert.Contains("Invalid SUT id", ex.Message);
    }

    private SystemMtCatalogDocument LoadCatalogJson(string sutId)
    {
        var path = Path.Combine(_root, sutId, "catalog.json");
        return JsonSerializer.Deserialize<SystemMtCatalogDocument>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })!;
    }

    private void WriteManifest(string sutId, string mrId, string programName = "", string equation = "Fourier")
    {
        var dir = Path.Combine(_root, sutId);
        Directory.CreateDirectory(dir);
        // Create stub script files so script-existence checks pass.
        File.WriteAllText(Path.Combine(dir, "runner.py"), "# runner stub");
        File.WriteAllText(Path.Combine(dir, "input_parser.py"), "# parser stub");
        File.WriteAllText(Path.Combine(dir, "output_parser.py"), "# parser stub");
        File.WriteAllText(Path.Combine(dir, "input.py"), "# adapter stub");
        File.WriteAllText(Path.Combine(dir, "output.py"), "# adapter stub");

        var effectiveProgramName = string.IsNullOrEmpty(programName) ? sutId : programName;
        File.WriteAllText(Path.Combine(dir, "catalog.json"), $$"""
        {
          "sut_name": "{{sutId}}",
          "program": {
            "program_name": "{{effectiveProgramName}}",
            "equation": "{{equation}}",
            "program_type": "Num",
            "equation_key": "fourier",
            "runner_script_relative_path": "runner.py",
            "input_adapter_script_relative_path": "input.py",
            "output_adapter_script_relative_path": "output.py",
            "input_parser_script_relative_path": "input_parser.py",
            "output_parser_script_relative_path": "output_parser.py",
            "python_executable_kind": "system"
          },
          "profile": {
            "program_type": "Num",
            "solver_method": "finite-difference",
            "runtime_key": "system",
            "input_contract": "JSON params with mesh and coefficient fields",
            "output_contract": "JSON metrics consumed by typed verifier",
            "adapter": "python runner under SUT/<sut>/",
            "dependency_risk": "pure-stdlib"
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
