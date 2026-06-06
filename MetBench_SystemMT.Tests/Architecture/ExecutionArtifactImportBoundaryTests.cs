using Xunit;

namespace MetBench_SystemMT.Tests.Architecture;

public sealed class ExecutionArtifactImportBoundaryTests
{
    private static readonly string[] ForbiddenSymbols =
    {
        "ImportExecutionArtifact",
        "ExecutionArtifactImporter",
        "ImportExecutionEvidence",
        "ImportExecutionResult",
    };

    [Fact]
    public void Production_execution_artifact_package_does_not_introduce_import_api()
    {
        var root = LocateRepoRoot();

        // Scan every production surface where an execution-result / evidence import path
        // could be introduced: the export package itself plus the job operation handlers
        // that dispatch artifact work. A silent skip on a missing directory would let the
        // guard pass vacuously, so each scanned root must exist.
        var productionRoots = new[]
        {
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "ImportExport", "ExecutionArtifacts"),
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Jobs", "Operations"),
        };

        foreach (var productionRoot in productionRoots)
        {
            Assert.True(
                Directory.Exists(productionRoot),
                $"Guarded production root must exist: {productionRoot}");

            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (var forbidden in ForbiddenSymbols)
                {
                    Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
                }
            }
        }
    }

    private static string LocateRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "MetBench.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }
}
