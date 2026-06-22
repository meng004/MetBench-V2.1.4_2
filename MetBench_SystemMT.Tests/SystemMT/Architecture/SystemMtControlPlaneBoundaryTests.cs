using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

/// <summary>
/// Source guards for the System MT control-plane boundary in CLAUDE.md section 0:
/// API / Business MCP adapters express intent, runtime backends execute SUTs,
/// and the pipeline owns the MT workflow through runtime executor abstractions.
/// </summary>
public sealed class SystemMtControlPlaneBoundaryTests
{
    private static readonly string[] RuntimeExecutorImplementationNames =
    {
        "DockerMcpProcessExecutor",
        "DockerRuntimeProcessExecutor",
    };

    private static readonly string[] RuntimeMcpImplementationTerms =
    {
        "DockerMcpRuntimeClient",
        "DockerMcpProcessExecutor",
        "DockerRuntimeProcessExecutor",
        "run_sut_command",
    };

    private static readonly string[] PublicApiRawPathTerms =
    {
        "Argv",
        "Command",
        "Manifest",
        "ManifestPath",
        "ArtifactRoot",
        "PackageRoot",
        "StagingRoot",
        "ExportRoot",
        "ArtifactPath",
        "WorkingDirectory",
        "Executable",
        "ExecutablePath",
    };

    private static readonly string[] RedundantPublicControlPlaneTerms =
    {
        "Workflow",
        "workflow",
    };

    private static readonly string[] SemanticTerms =
    {
        "workflow",
        "job",
        "operation / job kind",
        "submit_run",
        "execution",
        "runtime run",
        "cancel",
        "kill",
    };

    private static readonly string[] SemanticEnvironments =
    {
        "API",
        "Business MCP",
        "Runtime MCP",
    };

    [Fact]
    public void SystemMtPipeline_does_not_reference_runtime_executor_implementations_directly()
    {
        var root = SolutionRoot();
        var pipelinePath = Path.Combine(
            root,
            "MetBench_BLL.Core",
            "SystemMT",
            "Pipeline",
            "SystemMtPipeline.cs");
        var text = File.ReadAllText(pipelinePath);

        var violations = RuntimeExecutorImplementationNames
            .Where(term => text.Contains(term, StringComparison.Ordinal))
            .Select(term => $"{Path.GetRelativePath(root, pipelinePath)}: {term}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "SystemMtPipeline must not dispatch directly to concrete runtime executor implementations. "
            + "Backend selection belongs behind IRuntimeProcessExecutor / RuntimeProcessExecutorRegistry. "
            + "Offenders:\n  - " + string.Join("\n  - ", violations));
    }

    [Fact]
    public void Control_plane_adapters_do_not_reference_runtime_mcp_implementation_terms()
    {
        var root = SolutionRoot();
        var violations = new List<string>();

        foreach (var file in ExistingCsFiles(ControlPlaneAdapterRoots(root)))
        {
            ScanFileForTerms(root, file, RuntimeMcpImplementationTerms, violations);
        }

        Assert.True(
            violations.Count == 0,
            "API / Business MCP control-plane adapters must not call or name lower-level runtime MCP "
            + "implementation details directly. Route SUT execution through SystemMtJobService / "
            + "SystemMtLauncher / SystemMtPipeline and runtime executor abstractions. Offenders:\n  - "
            + string.Join("\n  - ", violations.Order(StringComparer.Ordinal)));
    }

    [Fact]
    public void Public_api_sources_do_not_expose_raw_filesystem_or_control_roots()
    {
        var root = SolutionRoot();
        var violations = new List<string>();

        foreach (var file in ExistingCsFiles(PublicApiRoots(root), skipProgram: true))
        {
            ScanFileForTerms(root, file, PublicApiRawPathTerms, violations);
        }

        Assert.True(
            violations.Count == 0,
            "Public API DTO/control-plane sources must not expose raw host filesystem roots or artifact "
            + "paths. Use opaque artifact ids plus the System MT artifact access service instead. "
            + "Offenders:\n  - " + string.Join("\n  - ", violations.Order(StringComparer.Ordinal)));
    }

    [Fact]
    public void Public_api_and_business_mcp_do_not_expose_workflow_as_a_resource()
    {
        var root = SolutionRoot();
        var violations = new List<string>();

        foreach (var file in ExistingPublicSurfaceFiles(root))
        {
            ScanFileForTerms(root, file, RedundantPublicControlPlaneTerms, violations);
        }

        Assert.True(
            violations.Count == 0,
            "The public control-plane vocabulary uses durable jobs as the resource. Workflow is a "
            + "descriptive internal process term, not a REST or MCP resource. Offenders:\n  - "
            + string.Join("\n  - ", violations.Order(StringComparer.Ordinal)));
    }

    [Fact]
    public void Semantic_workflow_is_internal_process_term_not_public_resource()
    {
        var root = SolutionRoot();
        var design = ReadControlPlaneDesign(root);

        Assert.Contains("`workflow`", design, StringComparison.Ordinal);
        Assert.Contains("It is not a public REST or MCP resource.", design, StringComparison.Ordinal);
        Assert.Contains("A submit target, durable record, queue item, or Runtime MCP command.", design, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_job_is_the_public_business_control_plane_resource()
    {
        var root = SolutionRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "MetBench_Api", "SystemMtApiEndpoints.cs"));

        Assert.Contains("MapPost(\"/jobs\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/jobs/{jobId:guid}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/jobs/{jobId:guid}/result\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/runs\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/runs", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_operation_or_job_kind_is_internal_classification_not_public_resource()
    {
        var root = SolutionRoot();
        var design = ReadControlPlaneDesign(root);
        var endpoints = File.ReadAllText(Path.Combine(root, "MetBench_Api", "SystemMtApiEndpoints.cs"));

        Assert.Contains("`operation` / `job kind`", design, StringComparison.Ordinal);
        Assert.True(Enum.IsDefined(typeof(SystemMtJobKind), SystemMtJobKind.RunMr));
        Assert.True(Enum.IsDefined(typeof(SystemMtJobKind), SystemMtJobKind.ExportReport));
        Assert.DoesNotContain("MapPost(\"/operations", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/operations", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_submit_run_is_a_command_that_creates_a_job_receipt()
    {
        var root = SolutionRoot();
        var design = ReadControlPlaneDesign(root);
        var businessServer = File.ReadAllText(Path.Combine(root, "infra", "mcp", "metbench-business", "server.py"));

        Assert.Contains("Submit creates a `jobId`; it does not create an `ExecutionId`.", design, StringComparison.Ordinal);
        Assert.Contains("\"submit_run\"", businessServer, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/systemmt/jobs\"", businessServer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/api/v1/systemmt/runs\"", businessServer, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_execution_is_persisted_result_identifier_not_submit_receipt()
    {
        var root = SolutionRoot();
        var design = ReadControlPlaneDesign(root);
        var controlPlane = File.ReadAllText(Path.Combine(
            root,
            "MetBench_BLL.Core",
            "SystemMT",
            "ControlPlane",
            "SystemMtControlPlaneService.cs"));

        Assert.Contains(
            "The core recorder creates `ExecutionId` after the job runs far enough to produce persisted result/evidence.",
            design,
            StringComparison.Ordinal);
        Assert.Contains("status?.ExecutionId", controlPlane, StringComparison.Ordinal);
        Assert.Contains("GetByExecutionAsync(executionId", controlPlane, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_runtime_run_is_runtime_mcp_invocation_id_not_business_job()
    {
        var root = SolutionRoot();
        var design = ReadControlPlaneDesign(root);
        var runtimeServer = File.ReadAllText(Path.Combine(root, "infra", "mcp", "docker-runtime", "server.py"));
        var businessServer = File.ReadAllText(Path.Combine(root, "infra", "mcp", "metbench-business", "server.py"));

        Assert.Contains("Runtime MCP creates runtime `run_id` values; it does not create jobs or executions.", design, StringComparison.Ordinal);
        Assert.Contains("\"run_id\"", runtimeServer, StringComparison.Ordinal);
        Assert.Contains("RUN_RECORDS", runtimeServer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"run_id\"", businessServer, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_cancel_is_business_control_action_against_job_id()
    {
        var root = SolutionRoot();
        var design = ReadControlPlaneDesign(root);
        var endpoints = File.ReadAllText(Path.Combine(root, "MetBench_Api", "SystemMtApiEndpoints.cs"));
        var businessServer = File.ReadAllText(Path.Combine(root, "infra", "mcp", "metbench-business", "server.py"));

        Assert.Contains("`cancel` |", design, StringComparison.Ordinal);
        Assert.Contains("Business control plane", design, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/jobs/{jobId:guid}/cancel\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("client(\"POST\"", businessServer, StringComparison.Ordinal);
        Assert.Contains("/api/v1/systemmt/jobs/", businessServer, StringComparison.Ordinal);
        Assert.Contains("/cancel", businessServer, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete(\"/jobs/{jobId:guid}\"", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_kill_is_runtime_only_and_not_business_mcp_tool()
    {
        var root = SolutionRoot();
        var design = ReadControlPlaneDesign(root);
        var runtimeServer = File.ReadAllText(Path.Combine(root, "infra", "mcp", "docker-runtime", "server.py"));
        var businessServer = File.ReadAllText(Path.Combine(root, "infra", "mcp", "metbench-business", "server.py"));

        Assert.Contains("`kill` |", design, StringComparison.Ordinal);
        Assert.Contains("Runtime execution plane", design, StringComparison.Ordinal);
        Assert.Contains("\"kill_run\"", runtimeServer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"kill_run\"", businessServer, StringComparison.Ordinal);
    }

    [Fact]
    public void Control_plane_design_defines_submit_execution_and_runtime_boundaries()
    {
        var root = SolutionRoot();
        var text = ReadControlPlaneDesign(root);

        Assert.Contains(
            "Submit creates a `jobId`; it does not create an `ExecutionId`.",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "Runtime MCP creates runtime `run_id` values; it does not create jobs or executions.",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "The core recorder creates `ExecutionId` after the job runs far enough to produce persisted result/evidence.",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void E2e_cleanup_uses_cancel_action_not_delete_job_resource()
    {
        var root = SolutionRoot();
        var e2ePath = Path.Combine(
            root,
            "MetBench_SystemMT.Tests",
            "SystemMT",
            "Acceptance",
            "SystemMtApiBusinessRuntimeMcpEndToEndTests.cs");
        var text = File.ReadAllText(e2ePath);

        Assert.DoesNotContain("HttpMethod.Delete", text, StringComparison.Ordinal);
        Assert.Contains("HttpMethod.Post", text, StringComparison.Ordinal);
        Assert.Contains("/cancel", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_validation_matrix_documents_every_term_in_every_environment()
    {
        var root = SolutionRoot();
        var matrixPath = Path.Combine(root, "docs", "uat", "control-semantics-validation-matrix.md");
        var text = File.ReadAllText(matrixPath);

        foreach (var semantic in SemanticTerms)
        {
            foreach (var environment in SemanticEnvironments)
            {
                Assert.Contains($"| `{semantic}` | {environment} |", text, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [MemberData(nameof(SemanticEnvironmentCases))]
    public void Semantic_validation_matrix_has_environment_specific_guard(
        string semantic,
        string environment,
        string[] requiredTerms,
        string[] forbiddenTerms)
    {
        var root = SolutionRoot();
        var source = ReadSemanticEnvironmentSource(root, environment);

        foreach (var term in requiredTerms)
        {
            Assert.True(
                source.Contains(term, StringComparison.Ordinal),
                $"Semantic '{semantic}' in '{environment}' must contain '{term}'.");
        }

        foreach (var term in forbiddenTerms)
        {
            Assert.True(
                !source.Contains(term, StringComparison.Ordinal),
                $"Semantic '{semantic}' in '{environment}' must not contain '{term}'.");
        }
    }

    public static IEnumerable<object[]> SemanticEnvironmentCases()
    {
        yield return Case("workflow", "API", [], ["workflow", "Workflow"]);
        yield return Case("workflow", "Business MCP", [], ["workflow", "Workflow"]);
        yield return Case("workflow", "Runtime MCP", [], ["workflow", "Workflow"]);

        yield return Case("job", "API", ["MapPost(\"/jobs\"", "MapGet(\"/jobs/{jobId:guid}\""], ["MapPost(\"/runs", "MapGet(\"/runs"]);
        yield return Case("job", "Business MCP", ["\"get_job\"", "/api/v1/systemmt/jobs"], []);
        yield return Case("job", "Runtime MCP", [], ["jobId", "job_id"]);

        yield return Case("operation / job kind", "API", ["SystemMtJobKind", "RunMr"], ["MapPost(\"/operations", "MapGet(\"/operations"]);
        yield return Case("operation / job kind", "Business MCP", [], ["submit_operation", "job_kind", "/operations"]);
        yield return Case("operation / job kind", "Runtime MCP", [], ["SystemMtJobKind", "job_kind", "/operations"]);

        yield return Case("submit_run", "API", ["SubmitRunAsync", "SystemMtJobReceiptResponse"], ["MapPost(\"/runs"]);
        yield return Case("submit_run", "Business MCP", ["\"submit_run\"", "\"/api/v1/systemmt/jobs\""], ["\"/api/v1/systemmt/runs\""]);
        yield return Case("submit_run", "Runtime MCP", [], ["submit_run"]);

        yield return Case("execution", "API", ["ExecutionId", "GetByExecutionAsync"], []);
        yield return Case("execution", "Business MCP", ["\"get_evidence\""], ["ExecutionId", "execution_id"]);
        yield return Case("execution", "Runtime MCP", [], ["ExecutionId", "execution_id"]);

        yield return Case("runtime run", "API", ["SourceRunId", "FollowupRunId"], ["run_sut_command"]);
        yield return Case("runtime run", "Business MCP", [], ["\"run_id\"", "run_sut_command"]);
        yield return Case("runtime run", "Runtime MCP", ["\"run_id\"", "RUN_RECORDS"], ["jobId", "ExecutionId"]);

        yield return Case("cancel", "API", ["MapPost(\"/jobs/{jobId:guid}/cancel\"", "CancelAsync"], ["MapDelete(\"/jobs/{jobId:guid}\""]);
        yield return Case("cancel", "Business MCP", ["\"cancel_job\"", "/cancel"], ["\"kill_run\""]);
        yield return Case("cancel", "Runtime MCP", [], ["\"cancel_job\""]);

        yield return Case("kill", "API", [], ["kill_run"]);
        yield return Case("kill", "Business MCP", [], ["\"kill_run\""]);
        yield return Case("kill", "Runtime MCP", ["\"kill_run\"", "kill_run("], ["\"cancel_job\""]);

        static object[] Case(
            string semantic,
            string environment,
            string[] requiredTerms,
            string[] forbiddenTerms) =>
            [semantic, environment, requiredTerms, forbiddenTerms];
    }

    private static string[] ControlPlaneAdapterRoots(string root)
    {
        return
        [
            Path.Combine(root, "MetBench_Api"),
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "ControlPlane"),
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Mcp"),
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "BusinessMcp"),
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Api"),
        ];
    }

    private static string[] PublicApiRoots(string root)
    {
        return
        [
            Path.Combine(root, "MetBench_Api"),
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "ControlPlane"),
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Api"),
        ];
    }

    private static IEnumerable<string> ExistingCsFiles(IEnumerable<string> roots, bool skipProgram = false)
    {
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (skipProgram && Path.GetFileName(file).Equals("Program.cs", StringComparison.Ordinal))
                    continue;
                yield return file;
            }
        }
    }

    private static IEnumerable<string> ExistingPublicSurfaceFiles(string root)
    {
        foreach (var file in ExistingCsFiles(PublicApiRoots(root), skipProgram: true))
            yield return file;

        foreach (var rootPath in new[]
        {
            Path.Combine(root, "infra", "mcp", "metbench-business"),
        })
        {
            if (!Directory.Exists(rootPath))
                continue;

            foreach (var pattern in new[] { "*.py" })
            {
                foreach (var file in Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories))
                    yield return file;
            }
        }
    }

    private static void ScanFileForTerms(
        string solutionRoot,
        string file,
        IEnumerable<string> terms,
        ICollection<string> violations)
    {
        var lineNumber = 0;
        foreach (var line in File.ReadLines(file))
        {
            lineNumber++;
            foreach (var term in terms)
            {
                if (line.Contains(term, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(solutionRoot, file)}:{lineNumber}: {term}");
                }
            }
        }
    }

    private static string ReadControlPlaneDesign(string root) => File.ReadAllText(Path.Combine(
        root,
        "docs",
        "superpowers",
        "specs",
        "2026-06-21-systemmt-api-mcp-control-plane-design.md"));

    private static string ReadSemanticEnvironmentSource(string root, string environment)
    {
        string[] files = environment switch
        {
            "API" =>
            [
                Path.Combine(root, "MetBench_Api", "SystemMtApiEndpoints.cs"),
                Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "ControlPlane", "SystemMtControlPlaneService.cs"),
                Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "ControlPlane", "ISystemMtControlPlaneService.cs"),
                Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Jobs", "SystemMtJobService.cs"),
                Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Jobs", "Operations", "SystemMtJobKind.cs"),
            ],
            "Business MCP" =>
            [
                Path.Combine(root, "infra", "mcp", "metbench-business", "server.py"),
            ],
            "Runtime MCP" =>
            [
                Path.Combine(root, "infra", "mcp", "docker-runtime", "server.py"),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown semantic environment."),
        };

        return string.Join("\n", files.Where(File.Exists).Select(File.ReadAllText));
    }

    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate solution root from {AppContext.BaseDirectory}.");
    }
}
