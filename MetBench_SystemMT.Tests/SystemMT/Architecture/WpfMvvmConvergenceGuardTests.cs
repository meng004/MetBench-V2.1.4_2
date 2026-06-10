using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

/// <summary>
/// Cloud-safe source guard locking the WPF MVVM convergence gains delivered by
/// PR #333 (squash "converge WPF MVVM dependencies"), per
/// <c>docs/superpowers/plans/2026-06-06-wpf-mvvm-convergence-plan.md</c> §3.
///
/// <para>PR #333 collapsed PR-1 (drop dead Prism), PR-2 (drop PropertyChanged.Fody),
/// and PR-3 (Stylet <c>s:Action</c> → <c>Microsoft.Xaml.Behaviors</c>) into a single
/// merge but did not add the per-PR source-guards the plan mandates. Linux CI cannot
/// compile the WPF project, so this fact-scans <c>MetBench_Client</c> sources directly
/// to fail loud if any of the three removed mechanisms returns.</para>
///
/// <para>Scope is exactly the PR-1/PR-2/PR-3 guards (6 assertions). The PR-4 guard
/// (<c>no_manual_inotify_in_vm</c>) is intentionally NOT added here: 4 ViewModels still
/// carry hand-written <c>OnPropertyChanged(</c> calls because PR-4 (migrate to
/// <c>ObservableObject</c>) has not run. Adding it now would fail by design.</para>
/// </summary>
public sealed class WpfMvvmConvergenceGuardTests
{
    // ---- PR-1: Prism dead reference removed ----

    [Fact]
    public void Client_csproj_does_not_reference_Prism()
    {
        var csproj = File.ReadAllText(ClientCsprojPath());
        Assert.False(
            csproj.Contains("Prism", StringComparison.OrdinalIgnoreCase),
            "Prism PackageReference re-introduced in MetBench_Client.csproj (PR-1 regression). "
            + "Prism was a dead reference (0 concrete type uses) removed by PR #333.");
    }

    [Fact]
    public void No_Client_source_uses_Prism()
    {
        var violations = new List<string>();
        foreach (var file in ClientCsFiles())
        {
            var lineIndex = 0;
            foreach (var line in File.ReadAllLines(file))
            {
                lineIndex++;
                if (IsCommentLine(line)) continue;
                if (line.Contains("using Prism", StringComparison.Ordinal))
                    violations.Add($"{RelativeToClient(file)}:{lineIndex}: {line.Trim()}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "'using Prism' re-introduced in MetBench_Client (PR-1 regression). Offenders:\n  - "
            + string.Join("\n  - ", violations));
    }

    // ---- PR-2: PropertyChanged.Fody global weave removed ----

    [Fact]
    public void Client_csproj_does_not_reference_PropertyChanged_Fody()
    {
        var csproj = File.ReadAllText(ClientCsprojPath());
        Assert.False(
            csproj.Contains("PropertyChanged.Fody", StringComparison.OrdinalIgnoreCase)
            || csproj.Contains("Fody", StringComparison.OrdinalIgnoreCase),
            "Fody / PropertyChanged.Fody PackageReference re-introduced in MetBench_Client.csproj "
            + "(PR-2 regression). The global IL INotify weave was removed by PR #333; "
            + "CommunityToolkit.Mvvm [ObservableProperty] is the only sanctioned path.");
    }

    [Fact]
    public void No_FodyWeavers_PropertyChanged_directive_present()
    {
        var path = FodyWeaversPath();
        if (path is null)
            return; // File removed entirely — strongest form of the guard.

        var content = File.ReadAllText(path);
        Assert.False(
            content.Contains("<PropertyChanged", StringComparison.Ordinal),
            $"FodyWeavers.xml at {path} re-introduced the <PropertyChanged /> weave (PR-2 regression). "
            + "Remove the directive or delete the file.");
    }

    // ---- PR-3: Stylet s:Action / s:View.ActionTarget removed ----

    [Fact]
    public void Client_csproj_does_not_reference_Stylet()
    {
        var csproj = File.ReadAllText(ClientCsprojPath());
        Assert.False(
            csproj.Contains("Stylet", StringComparison.OrdinalIgnoreCase),
            "Stylet PackageReference re-introduced in MetBench_Client.csproj (PR-3 regression). "
            + "Stylet action routing was replaced by Microsoft.Xaml.Behaviors in PR #333.");
    }

    [Fact]
    public void No_Xaml_uses_Stylet_action_routing()
    {
        var violations = new List<string>();
        foreach (var file in ClientXamlFiles())
        {
            var lineIndex = 0;
            foreach (var line in File.ReadAllLines(file))
            {
                lineIndex++;
                if (line.Contains("s:Action", StringComparison.Ordinal)
                    || line.Contains("s:View.ActionTarget", StringComparison.Ordinal))
                {
                    violations.Add($"{RelativeToClient(file)}:{lineIndex}: {line.Trim()}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Stylet 's:Action' / 's:View.ActionTarget' routing re-introduced in WPF XAML "
            + "(PR-3 regression). Use [RelayCommand] + Command bindings, or "
            + "i:Interaction.Triggers / i:InvokeCommandAction for non-click events. Offenders:\n  - "
            + string.Join("\n  - ", violations));
    }

    // ---- locators (walk up to the repo's MetBench_Client tree, mirroring
    //      WpfAsyncCorrectnessGuardTests) ----

    private static string ClientRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "MetBench_Client");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            $"Could not locate MetBench_Client from {AppContext.BaseDirectory}.");
    }

    private static string ClientCsprojPath()
    {
        var path = Path.Combine(ClientRoot(), "MetBench_Client.csproj");
        if (!File.Exists(path))
            throw new FileNotFoundException($"MetBench_Client.csproj not found at {path}.");
        return path;
    }

    private static string? FodyWeaversPath()
    {
        var path = Path.Combine(ClientRoot(), "FodyWeavers.xml");
        return File.Exists(path) ? path : null;
    }

    private static IEnumerable<string> ClientCsFiles()
        => Directory.GetFiles(ClientRoot(), "*.cs", SearchOption.AllDirectories);

    private static IEnumerable<string> ClientXamlFiles()
        => Directory.GetFiles(ClientRoot(), "*.xaml", SearchOption.AllDirectories);

    private static string RelativeToClient(string file)
        => Path.GetRelativePath(Path.GetDirectoryName(ClientRoot())!, file);

    private static bool IsCommentLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("*", StringComparison.Ordinal);
    }
}
