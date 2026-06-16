using System.Xml.Linq;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

public sealed class CoverageToolchainGuardTests
{
    [Theory]
    [InlineData("MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj")]
    [InlineData("MetBench_Client.Tests/MetBench_Client.Tests.csproj")]
    public void Test_projects_include_coverlet_collector_for_line_coverage_artifacts(string projectPath)
    {
        var project = XDocument.Load(Path.Combine(RepositoryRoot(), projectPath));
        XNamespace ns = project.Root?.Name.Namespace ?? XNamespace.None;

        var packages = project
            .Descendants(ns + "PackageReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("coverlet.collector", packages);
    }

    [Fact]
    public void Wpf_windows_do_not_ship_notimplemented_placeholders()
    {
        var windowsDir = Path.Combine(RepositoryRoot(), "MetBench_Client", "Views", "Windows");
        var offenders = Directory.EnumerateFiles(windowsDir, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("throw new NotImplementedException", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "WPF windows must not leave runtime interface members as NotImplementedException: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void Async_execution_artifact_export_does_not_call_sync_markdown_generation()
    {
        var exporterPath = Path.Combine(
            RepositoryRoot(),
            "MetBench_BLL.Core",
            "SystemMT",
            "ImportExport",
            "ExecutionArtifacts",
            "ExecutionArtifactExporter.cs");
        var source = File.ReadAllText(exporterPath);

        Assert.DoesNotContain(".GenerateExecution(request.ExecutionId", source, StringComparison.Ordinal);
        Assert.Contains(".GenerateExecutionAsync(request.ExecutionId", source, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MetBench.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
