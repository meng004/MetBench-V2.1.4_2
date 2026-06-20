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
        "PackageRoot",
        "StagingRoot",
        "ExportRoot",
        "ArtifactPath",
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

        foreach (var file in ExistingCsFiles(PublicApiRoots(root)))
        {
            ScanFileForTerms(root, file, PublicApiRawPathTerms, violations);
        }

        Assert.True(
            violations.Count == 0,
            "Public API DTO/control-plane sources must not expose raw host filesystem roots or artifact "
            + "paths. Use opaque artifact ids plus the System MT artifact access service instead. "
            + "Offenders:\n  - " + string.Join("\n  - ", violations.Order(StringComparer.Ordinal)));
    }

    private static string[] ControlPlaneAdapterRoots(string root)
    {
        return
        [
            Path.Combine(root, "MetBench_Api"),
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
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Api"),
        ];
    }

    private static IEnumerable<string> ExistingCsFiles(IEnumerable<string> roots)
    {
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
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
