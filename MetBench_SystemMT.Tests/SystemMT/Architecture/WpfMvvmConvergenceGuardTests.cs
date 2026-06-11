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
/// <para>Scope covers the full PR-1/PR-2/PR-3/PR-4 guard set (8 assertions). PR-4
/// (migrate the last hand-written <c>OnPropertyChanged(</c> call sites to
/// <c>[ObservableProperty]</c> / <c>[NotifyPropertyChangedFor]</c> / <c>SetProperty</c>)
/// is now complete, so <c>No_ViewModel_calls_OnPropertyChanged_manually</c> plus its
/// <c>Models/</c> companion <c>No_Model_calls_OnPropertyChanged_manually</c> pin the
/// gain — only the CommunityToolkit generated/protected notification surface remains.
/// The <c>Models/</c> companion closes the chain-end-review gap (the PR-4 migration also
/// touched <c>ApplicationEx.IsChecked</c> / <c>DomainEx.IsChecked</c> under <c>Models/</c>,
/// which the ViewModel-scoped guard never inspected) — see
/// <c>docs/superpowers/specs/2026-06-11-wpf-mvvm-convergence-chain-post-merge-review.md</c>.</para>
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

    // ---- PR-4: hand-written OnPropertyChanged( call sites removed from ViewModels ----

    [Fact]
    public void No_ViewModel_calls_OnPropertyChanged_manually()
    {
        var violations = new List<string>();
        foreach (var file in ClientViewModelFiles())
        {
            var lineIndex = 0;
            foreach (var line in File.ReadAllLines(file))
            {
                lineIndex++;
                if (IsCommentLine(line)) continue;
                // Match the invocation 'OnPropertyChanged(' only. Generated/override
                // partial method declarations are named 'On<Name>Changed(' and do not
                // contain this substring, so they are not flagged.
                if (line.Contains("OnPropertyChanged(", StringComparison.Ordinal))
                    violations.Add($"{RelativeToClient(file)}:{lineIndex}: {line.Trim()}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Hand-written 'OnPropertyChanged(' re-introduced in WPF ViewModels (PR-4 regression). "
            + "Use [ObservableProperty] (with [NotifyPropertyChangedFor] for derived props) or "
            + "SetProperty(ref field, value) instead. Offenders:\n  - "
            + string.Join("\n  - ", violations));
    }

    [Fact]
    public void No_Model_calls_OnPropertyChanged_manually()
    {
        // Chain-end-review companion to the ViewModel guard: the PR-4 migration also
        // converted ApplicationEx.IsChecked / DomainEx.IsChecked under Models/ to
        // [ObservableProperty]. The ViewModel-scoped guard never inspected Models/, so a
        // re-introduced manual raise there would pass silently. This pins that surface too.
        var violations = new List<string>();
        foreach (var file in ClientModelFiles())
        {
            var lineIndex = 0;
            foreach (var line in File.ReadAllLines(file))
            {
                lineIndex++;
                if (IsCommentLine(line)) continue;
                if (line.Contains("OnPropertyChanged(", StringComparison.Ordinal))
                    violations.Add($"{RelativeToClient(file)}:{lineIndex}: {line.Trim()}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Hand-written 'OnPropertyChanged(' re-introduced in WPF Models (PR-4 regression). "
            + "Use [ObservableProperty] (with [NotifyPropertyChangedFor] for derived props) or "
            + "SetProperty(ref field, value) instead. Offenders:\n  - "
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

    private static IEnumerable<string> ClientViewModelFiles()
    {
        var dir = Path.Combine(ClientRoot(), "ViewModels");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"MetBench_Client/ViewModels not found at {dir}.");
        return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
    }

    private static IEnumerable<string> ClientModelFiles()
    {
        var dir = Path.Combine(ClientRoot(), "Models");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"MetBench_Client/Models not found at {dir}.");
        return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
    }

    private static string RelativeToClient(string file)
        => Path.GetRelativePath(Path.GetDirectoryName(ClientRoot())!, file);

    private static bool IsCommentLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("*", StringComparison.Ordinal);
    }
}
