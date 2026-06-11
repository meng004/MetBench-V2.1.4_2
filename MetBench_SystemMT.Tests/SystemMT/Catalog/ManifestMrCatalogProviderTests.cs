using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog;

public sealed class ManifestMrCatalogProviderTests : System.IDisposable
{
    private readonly string _tmpRoot;

    public ManifestMrCatalogProviderTests()
    {
        _tmpRoot = Path.Combine(Path.GetTempPath(), "metbench-manifest-tests-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpRoot, recursive: true); } catch { /* swallow on cleanup */ }
    }

    private string WriteManifest(string sutDir, string json)
    {
        return WriteManifest(sutDir, "catalog.json", json);
    }

    private string WriteManifest(string sutDir, string fileName, string json)
    {
        var dir = Path.Combine(_tmpRoot, sutDir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, json);
        return path;
    }

    private LauncherOptions Opts() => new(SutRoot: _tmpRoot, SystemPython: "python3", OpenMocPython: "python3");

    private const string ValidSingleMrManifest = """
        {
          "sut_name": "test-sut",
          "program": {
            "program_name": "test-sut",
            "runner_script_relative_path": "runner.py",
            "input_parser_script_relative_path": "in_parser.py",
            "output_parser_script_relative_path": "out_parser.py",
            "input_adapter_script_relative_path": "in_adapter.py",
            "output_adapter_script_relative_path": "out_adapter.py",
            "python_executable_kind": "system"
          },
          "mrs": [
            {
              "mr_id": "test-mr-1",
              "sut_name": "test-sut",
              "display_name": "Test MR 1",
              "description": "Linear test MR.",
              "mr_family": "Test.Linear",
              "transformation_name": "ScaleField",
              "assertion_type_code": "greater",
              "assertion_name": "GreaterThan",
              "value_name": "y",
              "default_parameters": { "factor": "2" },
              "transform_steps": [
                { "transformation_name": "ScaleField", "target_field_path": "/x" }
              ],
              "sample_case_relative_path": "sample/case.json",
              "work_root_name": "MetBenchTestSut",
              "timeout_seconds": 30
            }
          ]
        }
        """;

    [Fact]
    public void Load_yields_entry_with_resolved_paths()
    {
        WriteManifest("test_sut_dir", ValidSingleMrManifest);

        var entries = new ManifestMrCatalogProvider(Opts()).Load();

        var e = Assert.Single(entries);
        Assert.Equal("test-mr-1", e.Mr.Id);
        Assert.Equal("test-sut", e.Mr.SutName);
        Assert.Equal("Test MR 1", e.Mr.DisplayName);
        Assert.Equal("ScaleField", e.Mr.TransformationName);
        Assert.Equal("GreaterThan", e.Mr.AssertionName);
        Assert.Equal("y", e.Mr.ValueName);
        Assert.Equal("greater", e.AssertionTypeCode);
        Assert.Equal("ScaleField", e.PrimaryTransformationName);

        // Path resolution: SutRoot + sutDir + relativePath
        Assert.Equal(Path.Combine(_tmpRoot, "test_sut_dir", "runner.py"), e.RunnerScriptPath);
        Assert.Equal(Path.Combine(_tmpRoot, "test_sut_dir", "in_parser.py"), e.InputParserScriptPath);
        Assert.Equal(Path.Combine(_tmpRoot, "test_sut_dir", "out_parser.py"), e.OutputParserScriptPath);
        // Sample path in JSON uses forward slash ("sample/case.json"); provider normalizes
        // it to the platform separator so manifest output matches the hardcoded Path.Combine
        // style byte-for-byte on every OS (Windows parity regression fix).
        Assert.Equal(Path.Combine("test_sut_dir", "sample", "case.json"), e.SampleCaseRelativePath);
    }

    [Fact]
    public void Load_accepts_manifest_profile_without_changing_runtime_entry()
    {
        var json = ValidSingleMrManifest.Replace(
            "  \"mrs\": [",
            "  \"profile\": {\n" +
            "    \"program_type\": \"Num\",\n" +
            "    \"solver_method\": \"finite-difference\",\n" +
            "    \"runtime_key\": \"system\",\n" +
            "    \"input_contract\": \"JSON params with mesh and coefficient fields\",\n" +
            "    \"output_contract\": \"JSON metrics consumed by typed verifier\",\n" +
            "    \"adapter\": \"python runner under SUT/<sut>/\",\n" +
            "    \"dependency_risk\": \"pure-stdlib\"\n" +
            "  },\n" +
            "  \"mrs\": [");
        WriteManifest("profile_dir", json);

        var e = Assert.Single(new ManifestMrCatalogProvider(Opts()).Load());

        Assert.Equal("test-mr-1", e.Mr.Id);
        Assert.Equal("python3", e.PythonExecutable);
        Assert.Equal(Path.Combine("profile_dir", "sample", "case.json"), e.SampleCaseRelativePath);
    }

    [Fact]
    public void SystemMtCatalogDocument_missing_profile_deserializes_to_default_empty_profile()
    {
        var doc = System.Text.Json.JsonSerializer.Deserialize<SystemMtCatalogDocument>(
            ValidSingleMrManifest,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower });

        Assert.NotNull(doc);
        Assert.NotNull(doc!.Profile);
        Assert.Equal(string.Empty, doc.Profile!.ProgramType);
        Assert.Equal(string.Empty, doc.Profile.SolverMethod);
        Assert.Equal(string.Empty, doc.Profile.RuntimeKey);
        Assert.Equal(string.Empty, doc.Profile.InputContract);
        Assert.Equal(string.Empty, doc.Profile.OutputContract);
        Assert.Equal(string.Empty, doc.Profile.Adapter);
        Assert.Equal(string.Empty, doc.Profile.DependencyRisk);
    }

    [Fact]
    public void Load_accepts_mr_explanation_profile_without_changing_runtime_entry()
    {
        var json = ValidSingleMrManifest.Replace(
            "              \"sample_case_relative_path\": \"sample/case.json\",",
            "              \"explanation_profile\": {\n" +
            "                \"meta_pattern_rationale\": \"Linearity MR: scaling the source should scale the solution.\",\n" +
            "                \"transformation_semantics\": \"Scale source input field by factor.\",\n" +
            "                \"observable_summary\": \"Compare selected scalar or field residual after source/follow-up runs.\",\n" +
            "                \"predicate_summary\": \"Binary comparison with configured tolerance.\",\n" +
            "                \"tolerance_summary\": \"No tolerance for strict ordinal relation.\",\n" +
            "                \"applicability\": \"Only valid when the SUT exposes the named metric.\",\n" +
            "                \"failure_meaning\": \"MR violation indicates inconsistent response to the declared transformation.\"\n" +
            "              },\n" +
            "              \"sample_case_relative_path\": \"sample/case.json\",");
        WriteManifest("mr_profile_dir", json);

        var e = Assert.Single(new ManifestMrCatalogProvider(Opts()).Load());

        Assert.Equal("test-mr-1", e.Mr.Id);
        Assert.Equal("ScaleField", e.PrimaryTransformationName);
        Assert.Equal("greater", e.AssertionTypeCode);
        Assert.Equal(Path.Combine("mr_profile_dir", "sample", "case.json"), e.SampleCaseRelativePath);
    }

    [Fact]
    public void MrBindingDefinition_missing_explanation_profile_deserializes_to_default_empty_profile()
    {
        var doc = System.Text.Json.JsonSerializer.Deserialize<SystemMtCatalogDocument>(
            ValidSingleMrManifest,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower });

        var binding = Assert.Single(doc!.Mrs);
        Assert.NotNull(binding.ExplanationProfile);
        Assert.Equal(string.Empty, binding.ExplanationProfile!.MetaPatternRationale);
        Assert.Equal(string.Empty, binding.ExplanationProfile.TransformationSemantics);
        Assert.Equal(string.Empty, binding.ExplanationProfile.ObservableSummary);
        Assert.Equal(string.Empty, binding.ExplanationProfile.PredicateSummary);
        Assert.Equal(string.Empty, binding.ExplanationProfile.ToleranceSummary);
        Assert.Equal(string.Empty, binding.ExplanationProfile.Applicability);
        Assert.Equal(string.Empty, binding.ExplanationProfile.FailureMeaning);
    }

    [Fact]
    public void Live_manifest_MR_profiles_cover_mono_inv_and_conv_examples()
    {
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        var entries = new ManifestMrCatalogProvider(options).Load();

        Assert.Contains(entries, e => e.Mr.Id == "poisson-source-superposition" && e.MetaPattern == "Mono");
        Assert.Contains(entries, e => e.Mr.Id == "subchannel-friction-invariance" && e.MetaPattern == "Inv");
        Assert.Contains(entries, e => e.Mr.Id == "poisson-mesh-richardson" && e.MetaPattern == "Conv");

        foreach (var (sutDir, mrId) in new[]
        {
            ("poisson_1d", "poisson-source-superposition"),
            ("subchannel_1d", "subchannel-friction-invariance"),
            ("poisson_1d", "poisson-mesh-richardson"),
        })
        {
            var path = Path.Combine(TestAssetPaths.AssetRoot(), sutDir, "catalog.json");
            var doc = System.Text.Json.JsonSerializer.Deserialize<SystemMtCatalogDocument>(
                File.ReadAllText(path),
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower })!;
            var profile = doc.Mrs.Single(m => m.MrId == mrId).ExplanationProfile;
            Assert.NotNull(profile);
            Assert.False(string.IsNullOrWhiteSpace(profile!.MetaPatternRationale));
            Assert.False(string.IsNullOrWhiteSpace(profile.TransformationSemantics));
            Assert.False(string.IsNullOrWhiteSpace(profile.ObservableSummary));
            Assert.False(string.IsNullOrWhiteSpace(profile.PredicateSummary));
            Assert.False(string.IsNullOrWhiteSpace(profile.ToleranceSummary));
            Assert.False(string.IsNullOrWhiteSpace(profile.Applicability));
            Assert.False(string.IsNullOrWhiteSpace(profile.FailureMeaning));
        }
    }

    [Fact]
    public void Load_normalizes_forward_slash_in_relative_path_to_platform_separator()
    {
        // Regression fix: in PR #91 the manifest's JSON forward-slash sample path
        // ("sample/pincell.json") was Path.Combined onto the sut dir without normalization,
        // producing "openmoc\sample/pincell.json" on Windows (mixed separators) that did not
        // match the hardcoded launcher's "openmoc\sample\pincell.json", breaking
        // CatalogParityTests on Windows. After the fix the manifest emits clean separators.
        var json = ValidSingleMrManifest.Replace(
            "\"sample_case_relative_path\": \"sample/case.json\"",
            "\"sample_case_relative_path\": \"sample/case.json\"");  // already forward-slash; the fixture default
        WriteManifest("normalize_dir", json);

        var entries = new ManifestMrCatalogProvider(Opts()).Load();
        var entry = Assert.Single(entries);

        // SampleCaseRelativePath should use ONLY the platform separator, never a foreign one.
        var foreignSeparator = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
        Assert.DoesNotContain(foreignSeparator, entry.SampleCaseRelativePath);

        // And it must equal the same Path.Combine sequence the hardcoded launcher would build.
        Assert.Equal(
            Path.Combine("normalize_dir", "sample", "case.json"),
            entry.SampleCaseRelativePath);
    }

    [Fact]
    public void Load_normalizes_backslash_in_relative_path_to_platform_separator()
    {
        // Belt-and-suspenders: if a manifest author writes a Windows-style backslash in JSON
        // (uncommon but valid JSON when properly escaped), normalize it the same way.
        var json = ValidSingleMrManifest.Replace(
            "\"sample_case_relative_path\": \"sample/case.json\"",
            "\"sample_case_relative_path\": \"sample\\\\case.json\"");
        WriteManifest("backslash_dir", json);

        var entries = new ManifestMrCatalogProvider(Opts()).Load();
        var entry = Assert.Single(entries);

        var foreignSeparator = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
        Assert.DoesNotContain(foreignSeparator, entry.SampleCaseRelativePath);
        Assert.Equal(
            Path.Combine("backslash_dir", "sample", "case.json"),
            entry.SampleCaseRelativePath);
    }

    [Fact]
    public void Load_throws_on_missing_required_field()
    {
        WriteManifest("bad_sut", """{"sut_name": ""}""");

        var ex = Assert.Throws<CatalogValidationException>(() => new ManifestMrCatalogProvider(Opts()).Load());
        Assert.Contains("SutName", ex.Message);
    }

    [Fact]
    public void Load_throws_when_approx_assertion_lacks_tolerance()
    {
        var json = """
            {
              "sut_name": "x",
              "program": {
                "program_name": "x",
                "runner_script_relative_path": "r.py",
                "python_executable_kind": "system"
              },
              "mrs": [
                {
                  "mr_id": "x-approx-no-tol",
                  "sut_name": "x",
                  "transformation_name": "ScaleField",
                  "assertion_type_code": "approx",
                  "assertion_name": "ApproxEqual",
                  "value_name": "v",
                  "default_parameters": { "factor": "2" },
                  "transform_steps": [
                    { "transformation_name": "ScaleField", "target_field_path": "/x" }
                  ],
                  "sample_case_relative_path": "sample/case.json",
                  "work_root_name": "MetBenchX",
                  "timeout_seconds": 30
                }
              ]
            }
            """;
        WriteManifest("x_dir", json);

        var ex = Assert.Throws<CatalogValidationException>(() => new ManifestMrCatalogProvider(Opts()).Load());
        Assert.Contains("tolerance", ex.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_throws_on_malformed_JSON()
    {
        WriteManifest("bad_json", "{ not even close to json");

        var ex = Assert.Throws<CatalogValidationException>(() => new ManifestMrCatalogProvider(Opts()).Load());
        Assert.Contains("JSON parse failed", ex.Message);
    }

    [Fact]
    public void Load_throws_when_program_block_missing()
    {
        var json = """
            {
              "sut_name": "x",
              "mrs": [
                {
                  "mr_id": "x-mr", "sut_name": "x",
                  "transformation_name": "ScaleField",
                  "assertion_type_code": "greater",
                  "transform_steps": [
                    { "transformation_name": "ScaleField", "target_field_path": "/x" }
                  ],
                  "work_root_name": "MetBenchX",
                  "timeout_seconds": 30
                }
              ]
            }
            """;
        WriteManifest("x_dir", json);

        var ex = Assert.Throws<CatalogValidationException>(() => new ManifestMrCatalogProvider(Opts()).Load());
        Assert.Contains("program", ex.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_throws_on_unknown_AssertionTypeCode()
    {
        var json = ValidSingleMrManifest.Replace("\"assertion_type_code\": \"greater\"", "\"assertion_type_code\": \"approximate\"");
        WriteManifest("typo_sut", json);

        var ex = Assert.Throws<CatalogValidationException>(() => new ManifestMrCatalogProvider(Opts()).Load());
        Assert.Contains("not a recognized code", ex.Message);
    }

    [Fact]
    public void Load_throws_when_binding_SutName_does_not_match_document_SutName()
    {
        var json = """
            {
              "sut_name": "doc-sut",
              "program": {
                "program_name": "p", "runner_script_relative_path": "r.py", "python_executable_kind": "system"
              },
              "mrs": [
                {
                  "mr_id": "mismatch-mr",
                  "sut_name": "binding-sut",
                  "transformation_name": "ScaleField",
                  "assertion_type_code": "greater",
                  "transform_steps": [
                    { "transformation_name": "ScaleField", "target_field_path": "/x" }
                  ],
                  "work_root_name": "MetBenchMM",
                  "timeout_seconds": 30
                }
              ]
            }
            """;
        WriteManifest("doc_sut_dir", json);

        var ex = Assert.Throws<CatalogValidationException>(() => new ManifestMrCatalogProvider(Opts()).Load());
        Assert.Contains("does not match document SutName", ex.Message);
    }

    [Fact]
    public void Load_discovers_multiple_manifests_in_subdirectories()
    {
        WriteManifest("sut_one", ValidSingleMrManifest);
        WriteManifest("sut_two", ValidSingleMrManifest.Replace("test-mr-1", "test-mr-2"));

        var entries = new ManifestMrCatalogProvider(Opts()).Load();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Mr.Id == "test-mr-1");
        Assert.Contains(entries, e => e.Mr.Id == "test-mr-2");
    }

    [Fact]
    public void Load_returns_empty_when_SutRoot_does_not_exist()
    {
        var entries = new ManifestMrCatalogProvider(new LauncherOptions(
            SutRoot: "/nonexistent/path", SystemPython: "python3", OpenMocPython: "python3")).Load();

        Assert.Empty(entries);
    }

    [Fact]
    public void SourceDescription_identifies_manifest_origin()
    {
        var p = new ManifestMrCatalogProvider(Opts());
        Assert.Contains("Manifest:", p.SourceDescription);
        Assert.Contains(_tmpRoot, p.SourceDescription);
    }

    [Fact]
    public void Constructor_rejects_null_options()
    {
        Assert.Throws<System.ArgumentNullException>(() => new ManifestMrCatalogProvider(null!));
    }

    [Fact]
    public void Load_accepts_explicit_manifest_path_list()
    {
        var p1 = WriteManifest("sut_one", ValidSingleMrManifest);
        WriteManifest("sut_two", ValidSingleMrManifest.Replace("test-mr-1", "test-mr-2"));

        // Explicit list bypasses auto-discovery; only the listed manifest is loaded.
        var entries = new ManifestMrCatalogProvider(Opts(), new[] { p1 }).Load();

        Assert.Single(entries);
        Assert.Equal("test-mr-1", entries[0].Mr.Id);
    }

    // ---- PR-1 T1 manifest-driven runtime environments ------------------------------------
    // New manifest python_executable_kind values must resolve through
    // LauncherOptions.RuntimePythons without growing per-runtime fields. Unknown non-system
    // keys stay loadable so launcher preflight can record RuntimeProfileMissing evidence.

    private const string FutureRuntimeManifest = """
        {
          "sut_name": "fenics-demo",
          "program": {
            "program_name": "fenics-demo",
            "runner_script_relative_path": "runner.py",
            "input_parser_script_relative_path": "in_parser.py",
            "output_parser_script_relative_path": "out_parser.py",
            "input_adapter_script_relative_path": "in_adapter.py",
            "output_adapter_script_relative_path": "out_adapter.py",
            "python_executable_kind": "fenics"
          },
          "mrs": [
            {
              "mr_id": "fenics-demo-mr",
              "sut_name": "fenics-demo",
              "display_name": "FEniCS demo MR",
              "description": "Future-runtime probe MR.",
              "mr_family": "Future.Runtime.Probe",
              "transformation_name": "ScaleField",
              "assertion_type_code": "greater",
              "assertion_name": "GreaterThan",
              "value_name": "y",
              "default_parameters": { "factor": "2" },
              "transform_steps": [
                { "transformation_name": "ScaleField", "target_field_path": "/x" }
              ],
              "sample_case_relative_path": "sample/case.json",
              "work_root_name": "MetBenchFenicsDemo",
              "timeout_seconds": 30
            }
          ]
        }
        """;

    [Fact]
    public void Load_resolves_future_runtime_key_through_RuntimePythons_map()
    {
        WriteManifest("fenics_demo_dir", FutureRuntimeManifest);

        var opts = new LauncherOptions(
            SutRoot: _tmpRoot,
            SystemPython: "python3",
            OpenMocPython: "python3",
            RuntimePythons: new Dictionary<string, string>
            {
                ["fenics"] = "/venv/fenics/bin/python",
            });

        var entries = new ManifestMrCatalogProvider(opts).Load();
        var entry = Assert.Single(entries);
        Assert.Equal("/venv/fenics/bin/python", entry.PythonExecutable);
    }

    [Fact]
    public void Load_preserves_unknown_unmapped_runtime_key_for_launcher_preflight()
    {
        WriteManifest("fenics_demo_dir", FutureRuntimeManifest);

        var opts = new LauncherOptions(
            SutRoot: _tmpRoot,
            SystemPython: "python3",
            OpenMocPython: "python3");

        var entries = new ManifestMrCatalogProvider(opts).Load();
        var entry = Assert.Single(entries);
        Assert.Equal("fenics", entry.RuntimeKey);
        Assert.Equal(string.Empty, entry.PythonExecutable);
    }

    [Fact]
    public void Load_routes_legacy_openmoc_runtime_key_to_OpenMocPython_when_no_map_entry()
    {
        var json = ValidSingleMrManifest.Replace(
            "\"python_executable_kind\": \"system\"",
            "\"python_executable_kind\": \"openmoc\"");
        WriteManifest("openmoc_compat_dir", json);

        var opts = new LauncherOptions(
            SutRoot: _tmpRoot,
            SystemPython: "python3",
            OpenMocPython: "/legacy/openmoc/bin/python");

        var entries = new ManifestMrCatalogProvider(opts).Load();
        var entry = Assert.Single(entries);
        Assert.Equal("/legacy/openmoc/bin/python", entry.PythonExecutable);
    }

    [Fact]
    public void Load_routes_legacy_scipy_runtime_key_to_ScipyPython_when_no_map_entry()
    {
        var json = ValidSingleMrManifest.Replace(
            "\"python_executable_kind\": \"system\"",
            "\"python_executable_kind\": \"scipy\"");
        WriteManifest("scipy_compat_dir", json);

        var opts = new LauncherOptions(
            SutRoot: _tmpRoot,
            SystemPython: "python3",
            OpenMocPython: "python3",
            ScipyPython: "/venv/scipy/bin/python");

        var entries = new ManifestMrCatalogProvider(opts).Load();
        var entry = Assert.Single(entries);
        Assert.Equal("/venv/scipy/bin/python", entry.PythonExecutable);
    }
}
