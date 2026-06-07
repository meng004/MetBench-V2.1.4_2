using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

    [Fact]
    public void Client_sources_do_not_use_wpf_ui_message_box_dialogs()
    {
        var repoRoot = FindRepositoryRoot();
        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");
        var forbiddenTerms = new[]
        {
            "Wpf.Ui.Controls.MessageBox",
            "ShowDialogAsync",
        };

        var matches = EnumerateClientFiles(clientRoot, "*.cs")
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .SelectMany(file => forbiddenTerms
                .Where(term => file.Text.Contains(term, StringComparison.Ordinal))
                .Select(term => $"{Path.GetRelativePath(repoRoot, file.Path)} contains {term}"))
            .OrderBy(match => match)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected WPF-UI dialog usage: " + string.Join(", ", matches));
    }

    [Fact]
    public void Informational_dialog_results_are_not_used_for_branching()
    {
        var repoRoot = FindRepositoryRoot();
        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");
        var assignmentPattern = new Regex(@"\b(?:var|bool)\s+\w+\s*=\s*await\s+showMessageAsync\s*\(", RegexOptions.Compiled);

        var matches = EnumerateClientFiles(clientRoot, "*.cs")
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .Where(file => assignmentPattern.IsMatch(file.Text))
            .Select(file => Path.GetRelativePath(repoRoot, file.Path))
            .OrderBy(path => path)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected informational dialog result branching: " + string.Join(", ", matches));
    }

    [Fact]
    public void Xaml_no_longer_uses_wpf_ui_theme_resource_extension()
    {
        var repoRoot = FindRepositoryRoot();
        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");

        var matches = EnumerateClientFiles(clientRoot, "*.xaml")
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .Where(file => file.Text.Contains("ui:ThemeResource", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repoRoot, file.Path))
            .OrderBy(path => path)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected WPF-UI ThemeResource extension usage: " + string.Join(", ", matches));
    }

    [Fact]
    public void Client_app_does_not_register_wpf_ui_theme_service()
    {
        var repoRoot = FindRepositoryRoot();
        var appFile = Path.Combine(repoRoot, "MetBench_Client", "App.xaml.cs");
        var appText = File.ReadAllText(appFile);
        var forbiddenTerms = new[]
        {
            "IThemeService",
            "ThemeService",
        };

        var matches = forbiddenTerms
            .Where(term => appText.Contains(term, StringComparison.Ordinal))
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected WPF-UI ThemeService registration terms: " + string.Join(", ", matches));
    }

    [Fact]
    public void View_models_and_helpers_do_not_reference_wpf_ui_appearance()
    {
        var repoRoot = FindRepositoryRoot();
        var clientRoot = Path.Combine(repoRoot, "MetBench_Client");
        var scopedRoots = new[]
        {
            Path.Combine(clientRoot, "ViewModels"),
            Path.Combine(clientRoot, "Helpers"),
        };

        var matches = scopedRoots
            .SelectMany(root => EnumerateClientFiles(root, "*.cs"))
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .Where(file => file.Text.Contains("Wpf.Ui.Appearance", StringComparison.Ordinal)
                || file.Text.Contains("using Wpf.Ui.Appearance", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repoRoot, file.Path))
            .OrderBy(path => path)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected WPF-UI appearance dependency in view-model/helper code: " + string.Join(", ", matches));
    }

    [Fact]
    public void Settings_page_no_longer_uses_wpf_ui_controls_or_namespaces()
    {
        var repoRoot = FindRepositoryRoot();
        var settingsFiles = new[]
        {
            Path.Combine(repoRoot, "MetBench_Client", "Views", "Pages", "SettingsPage.xaml"),
            Path.Combine(repoRoot, "MetBench_Client", "Views", "Pages", "SettingsPage.xaml.cs"),
        };
        var forbiddenTerms = new[]
        {
            "http://schemas.lepo.co/wpfui/2022/xaml",
            "Wpf.Ui",
            "ui:",
        };

        var matches = settingsFiles
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .SelectMany(file => forbiddenTerms
                .Where(term => file.Text.Contains(term, StringComparison.Ordinal))
                .Select(term => $"{Path.GetRelativePath(repoRoot, file.Path)} contains {term}"))
            .OrderBy(match => match)
            .ToArray();

        Assert.True(matches.Length == 0, "Unexpected WPF-UI Settings page usage: " + string.Join(", ", matches));
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
