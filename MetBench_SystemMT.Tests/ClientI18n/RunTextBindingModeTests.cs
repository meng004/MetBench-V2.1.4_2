using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

/// <summary>
/// Guards against a recurring WPF crash class: <c>Run.Text</c> is registered
/// <c>BindsTwoWayByDefault</c>, so a <c>&lt;Run Text="{Binding ...}"&gt;</c> without an
/// explicit <c>Mode=OneWay</c> attempts a TwoWay binding. When the source is a read-only
/// property (e.g. the <c>LocalizedTextProvider</c> indexer) WPF throws
/// <c>InvalidOperationException</c> at layout time and the page crashes.
///
/// This has already happened twice (MutationCampaignPage — PR #247; PagingBar — this fix).
/// The guard is repo-wide so the next localized <c>&lt;Run&gt;</c> cannot reintroduce it.
/// </summary>
public sealed class RunTextBindingModeTests
{
    private static readonly Regex RunWithTextBinding = new(
        "<Run\\b[^>]*?Text=\"\\{Binding[^}]*\\}\"[^>]*>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    [Fact]
    public void Every_run_text_binding_in_client_views_is_one_way()
    {
        var viewsRoot = Path.Combine(FindRepoRoot(), "MetBench_Client", "Views");
        var violations = new List<string>();

        foreach (var xamlPath in Directory.EnumerateFiles(viewsRoot, "*.xaml", SearchOption.AllDirectories))
        {
            var xaml = File.ReadAllText(xamlPath);
            foreach (Match match in RunWithTextBinding.Matches(xaml))
            {
                if (!match.Value.Contains("Mode=OneWay"))
                {
                    var relative = Path.GetRelativePath(viewsRoot, xamlPath);
                    violations.Add($"{relative}: {match.Value.Trim()}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Run.Text is TwoWay-by-default; bindings without Mode=OneWay crash when the source is read-only. "
                + "Offending <Run> elements:\n" + string.Join("\n", violations));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "MetBench_Client", "Views")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing MetBench_Client/Views.");
    }
}
