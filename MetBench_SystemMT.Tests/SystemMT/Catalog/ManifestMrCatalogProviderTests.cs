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
        var dir = Path.Combine(_tmpRoot, sutDir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "catalog.json");
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
        Assert.Equal(Path.Combine("test_sut_dir", "sample/case.json"), e.SampleCaseRelativePath);
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
}
