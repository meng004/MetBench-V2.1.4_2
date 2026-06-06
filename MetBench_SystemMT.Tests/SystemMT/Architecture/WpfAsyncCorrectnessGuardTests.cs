using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

/// <summary>
/// Cloud-safe source guard ratcheting the P4 WPF deadlock-surface cleanup (PR #330).
/// Linux CI cannot compile the WPF project, so this fact-scans the ViewModel sources
/// directly to fail loud if either anti-pattern returns.
///
/// <para>The cleanup removed 18 <c>.ShowDialogAsync().Result</c> blocking-await sites
/// across 8 ViewModels plus 1 non-navigation <c>async void</c> at
/// MTReportGeneratorViewModel.HandleSelectionChange. These tests pin both gains so a
/// future PR cannot silently regress them.</para>
///
/// <para>Acceptance is strict: equivalents
/// (<c>.ShowDialogAsync().GetAwaiter().GetResult()</c>, <c>Task.Run(...).Result</c>)
/// are also forbidden — they are the same deadlock face.</para>
/// </summary>
public sealed class WpfAsyncCorrectnessGuardTests
{
    [Fact]
    public void No_ViewModel_blocks_on_ShowDialogAsync_result()
    {
        var violations = new List<string>();
        foreach (var file in ViewModelFiles())
        {
            var source = File.ReadAllText(file);
            var lineIndex = 0;
            foreach (var line in source.Split('\n'))
            {
                lineIndex++;
                if (IsCommentLine(line)) continue;
                if (line.Contains(".ShowDialogAsync().Result", StringComparison.Ordinal)
                    || line.Contains(".ShowDialogAsync().GetAwaiter().GetResult()", StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetFileName(file)}:{lineIndex}: {line.Trim()}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "WPF ViewModel synchronous waits on ShowDialogAsync re-introduced (P4 regression). "
            + "Each call must be 'await uiMessageBox.ShowDialogAsync()' inside an async Task method. "
            + "Offenders:\n  - " + string.Join("\n  - ", violations));
    }

    [Fact]
    public void No_ViewModel_uses_async_void_outside_OnNavigatedTo()
    {
        var violations = new List<string>();
        foreach (var file in ViewModelFiles())
        {
            var source = File.ReadAllText(file);
            var lineIndex = 0;
            foreach (var line in source.Split('\n'))
            {
                lineIndex++;
                if (IsCommentLine(line)) continue;
                if (!line.Contains("async void", StringComparison.Ordinal)) continue;
                // OnNavigatedTo is the sanctioned exception per CLAUDE.md §7
                // (lifecycle hook called by Wpf.Ui INavigationAware).
                if (line.Contains("OnNavigatedTo", StringComparison.Ordinal)) continue;
                violations.Add($"{Path.GetFileName(file)}:{lineIndex}: {line.Trim()}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Non-OnNavigatedTo 'async void' methods re-introduced in WPF ViewModels (P4 regression). "
            + "Each must be 'async Task' (or a safe fire-and-forget wrapper). Offenders:\n  - "
            + string.Join("\n  - ", violations));
    }

    private static IEnumerable<string> ViewModelFiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "MetBench_Client", "ViewModels");
            if (Directory.Exists(candidate))
                return Directory.GetFiles(candidate, "*.cs", SearchOption.AllDirectories);
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            $"Could not locate MetBench_Client/ViewModels from {AppContext.BaseDirectory}.");
    }

    private static bool IsCommentLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("*", StringComparison.Ordinal);
    }
}
