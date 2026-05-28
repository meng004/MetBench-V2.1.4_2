using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Governance;

/// <summary>
/// Reads the repo-root governance whitelist at
/// <c>.github/governance/expected-catalog-counts.txt</c> and exposes the
/// catalog meta-fact counts to test fixtures. See
/// <c>docs/superpowers/plans/2026-05-28-p1-catalog-derived-counts-plan.md</c>
/// §3.2 for the data contract and CLAUDE.md §12.4 R1 for the rule this
/// helper enforces.
/// </summary>
/// <remarks>
/// The file format is <c>prefix:id</c> per line, with three prefixes
/// (<c>mr</c> / <c>sut</c> / <c>eq</c>). Lines starting with <c>#</c> and
/// blank lines are ignored. Each call hits the filesystem; the per-call
/// cost is negligible (~70 lines) and avoiding static caches keeps tests
/// independent across xUnit collections.
/// </remarks>
internal static class ExpectedCatalogCountsWhitelist
{
    private const string RelativeWhitelistPath = ".github/governance/expected-catalog-counts.txt";

    /// <summary>All ids whose line begins with <paramref name="prefix"/>:.</summary>
    public static IReadOnlyList<string> ReadAllIds(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix must be non-empty.", nameof(prefix));
        }

        var path = ResolvePath();
        var marker = prefix + ":";
        var ids = new List<string>();
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (!line.StartsWith(marker, StringComparison.Ordinal))
            {
                continue;
            }

            var id = line.Substring(marker.Length).Trim();
            if (id.Length == 0)
            {
                continue;
            }

            ids.Add(id);
        }

        return ids;
    }

    public static IReadOnlyList<string> ReadMrIds() => ReadAllIds("mr");
    public static IReadOnlyList<string> ReadSutNames() => ReadAllIds("sut");
    public static IReadOnlyList<string> ReadEquationKeys() => ReadAllIds("eq");

    public static int MrCount => ReadMrIds().Count;
    public static int SutCount => ReadSutNames().Count;
    public static int EquationCount => ReadEquationKeys().Count;

    /// <summary>
    /// Locates the whitelist by climbing from <see cref="AppContext.BaseDirectory"/>
    /// (e.g. <c>&lt;repo&gt;/MetBench_SystemMT.Tests/bin/Debug/net8.0</c>) until a
    /// directory containing <c>.github/governance/expected-catalog-counts.txt</c>
    /// is found. Fails fast with the candidate paths in the exception message so
    /// CI red output points at the resolver, not at a downstream count mismatch.
    /// </summary>
    private static string ResolvePath()
    {
        var candidates = new List<string>();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, RelativeWhitelistPath);
            candidates.Add(candidate);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate governance whitelist '" + RelativeWhitelistPath +
            "' from AppContext.BaseDirectory='" + AppContext.BaseDirectory +
            "'. Candidates checked: " + string.Join(" ; ", candidates));
    }
}
