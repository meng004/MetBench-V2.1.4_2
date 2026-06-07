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

    [Fact]
    public void Stylet_is_not_referenced_by_client_project_or_sources()
    {
        var repoRoot = FindRepositoryRoot();
        var projectFile = Path.Combine(repoRoot, "MetBench_Client", "MetBench_Client.csproj");
        var projectXml = File.ReadAllText(projectFile);

        Assert.DoesNotContain("Stylet.Start", projectXml, StringComparison.OrdinalIgnoreCase);

        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");
        var sourceMatches = EnumerateClientFiles(clientRoot, "*.cs")
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .Where(file => file.Text.Contains("using Stylet", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repoRoot, file.Path))
            .OrderBy(path => path)
            .ToArray();

        Assert.True(sourceMatches.Length == 0, "Unexpected Stylet imports: " + string.Join(", ", sourceMatches));
    }

    [Fact]
    public void Xaml_no_longer_uses_stylet_action_bindings()
    {
        var repoRoot = FindRepositoryRoot();
        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");
        var forbiddenTerms = new[]
        {
            "https://github.com/canton7/Stylet",
            "s:Action",
            "s:View.ActionTarget",
        };

        var matches = EnumerateClientFiles(clientRoot, "*.xaml")
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .SelectMany(file => forbiddenTerms
                .Where(term => file.Text.Contains(term, StringComparison.Ordinal))
                .Select(term => $"{Path.GetRelativePath(repoRoot, file.Path)} contains {term}"))
            .OrderBy(match => match)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected Stylet XAML bindings: " + string.Join(", ", matches));
    }

    [Fact]
    public void PropertyChanged_fody_weaver_is_not_present()
    {
        var repoRoot = FindRepositoryRoot();
        var projectFile = Path.Combine(repoRoot, "MetBench_Client", "MetBench_Client.csproj");
        var projectXml = File.ReadAllText(projectFile);

        Assert.DoesNotContain("PropertyChanged.Fody", projectXml, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(repoRoot, "MetBench_Client", "FodyWeavers.xml")));
        Assert.False(File.Exists(Path.Combine(repoRoot, "MetBench_Client", "FodyWeavers.xsd")));
    }

    [Fact]
    public void Wpf_ui_tray_is_not_referenced_by_client_project_or_sources()
    {
        var repoRoot = FindRepositoryRoot();
        var projectFile = Path.Combine(repoRoot, "MetBench_Client", "MetBench_Client.csproj");
        var projectXml = File.ReadAllText(projectFile);

        Assert.DoesNotContain("WPF-UI.Tray", projectXml, StringComparison.OrdinalIgnoreCase);

        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");
        var forbiddenTerms = new[]
        {
            "NotifyIcon",
            "TitleBar.Tray",
            "ITaskBarService",
            "TaskBarService",
        };

        var matches = EnumerateClientFiles(clientRoot, "*.cs")
            .Concat(EnumerateClientFiles(clientRoot, "*.xaml"))
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .SelectMany(file => forbiddenTerms
                .Where(term => file.Text.Contains(term, StringComparison.Ordinal))
                .Select(term => $"{Path.GetRelativePath(repoRoot, file.Path)} contains {term}"))
            .OrderBy(match => match)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected WPF-UI tray usage: " + string.Join(", ", matches));
    }

    private static IEnumerable<string> EnumerateClientFiles(string clientRoot, string searchPattern)
    {
        return Directory.EnumerateFiles(clientRoot, searchPattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
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
