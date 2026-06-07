using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MetBench_Client.Tests.Architecture;

public sealed class WpfDependencyGovernanceTests
{
    [Fact]
    public void Prism_wpf_is_not_referenced_by_client_project()
    {
        var repoRoot = FindRepositoryRoot();
        var projectFile = Path.Combine(repoRoot, "MetBench_Client", "MetBench_Client.csproj");
        var projectXml = File.ReadAllText(projectFile);

        Assert.DoesNotContain("Prism.Wpf", projectXml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Client_sources_do_not_import_prism_namespaces()
    {
        var repoRoot = FindRepositoryRoot();
        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");
        var matches = Directory.EnumerateFiles(clientRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .Where(file => file.Text.Contains("using Prism.", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repoRoot, file.Path))
            .OrderBy(path => path)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected Prism imports: " + string.Join(", ", matches));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MetBench.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }
}
