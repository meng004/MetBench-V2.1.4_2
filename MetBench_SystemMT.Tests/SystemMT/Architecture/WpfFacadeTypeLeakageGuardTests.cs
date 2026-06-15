using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

/// <summary>
/// Cloud-safe source guard for the CLAUDE.md §6 launcher-facade type-leakage rule:
/// the WPF view layer (MetBench_Client/ViewModels) must consume ONLY the facade DTOs
/// (MrSummary / MrRunResult / BatchMrRunRequest / BatchProgress / SystemMtResultRecord)
/// and the public seam interfaces (ISystemMtLauncher / IMrCatalogProvider), never the
/// engine-internal types the facade is meant to insulate. Linux CI cannot compile the
/// WPF project, so this fact-scans the ViewModel sources directly and fails loud if a
/// forbidden engine-internal type name appears — turning the previously convention+grep
/// guarantee into an enforced regression gate (assessment recommendation P2).
///
/// <para>Scope is ViewModels/ (the insulation surface). App.xaml.cs is the composition
/// root and legitimately wires concrete engine types into DI, so it is intentionally out
/// of scope. The forbidden names are chosen to be unambiguous — none is a substring of an
/// allowed facade type — so an Ordinal Contains match has no false positives.</para>
/// </summary>
public sealed class WpfFacadeTypeLeakageGuardTests
{
    private static readonly string[] ForbiddenEngineTypes =
    {
        "MrTransformation",
        "SystemMtTask",
        "SystemMtRunner",
        "IMrAssertion",
        "SystemMtCase",
        "MrCatalogEntry",
        "MrBlueprint",
    };

    [Fact]
    public void No_ViewModel_references_engine_internal_types()
    {
        var violations = new List<string>();
        foreach (var file in ClientViewModelFiles())
        {
            var lineIndex = 0;
            foreach (var line in File.ReadAllLines(file))
            {
                lineIndex++;
                if (IsCommentLine(line)) continue;
                foreach (var type in ForbiddenEngineTypes)
                {
                    if (line.Contains(type, StringComparison.Ordinal))
                        violations.Add($"{RelativeToClient(file)}:{lineIndex}: {type} -> {line.Trim()}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "CLAUDE.md §6 type-leakage rule violated: a MetBench_Client ViewModel references an "
            + "engine-internal type the ISystemMtLauncher facade is supposed to insulate. Consume the "
            + "facade DTOs (MrSummary / MrRunResult / SystemMtResultRecord) or the public seam "
            + "interfaces instead, so the planned IR refactor can change internals without breaking "
            + "views. Offenders:\n  - " + string.Join("\n  - ", violations));
    }

    // ---- locators (mirror WpfMvvmConvergenceGuardTests) ----

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

    private static IEnumerable<string> ClientViewModelFiles()
    {
        var dir = Path.Combine(ClientRoot(), "ViewModels");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"MetBench_Client/ViewModels not found at {dir}.");
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