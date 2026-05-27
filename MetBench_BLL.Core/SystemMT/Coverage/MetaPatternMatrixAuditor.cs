using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Coverage;

/// <summary>
/// Audits (equation × meta-pattern) coverage across the system-MT MR catalog
/// to surface which combinations are filled and which are still empty.
///
/// <para>
/// Built for Phase 4 (PR-T3-7) of the T2/T3 sequenced plan
/// (<c>docs/superpowers/plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md</c>):
/// drives gap-fill MR candidate selection in Phase 5.
/// </para>
///
/// <para>
/// Stateless / pure: takes an <see cref="IMrCatalogProvider"/>, returns a
/// snapshot record. No side effects, no I/O beyond what the provider performs.
/// </para>
/// </summary>
public static class MetaPatternMatrixAuditor
{
    /// <summary>
    /// Canonical meta-pattern slugs declared in <c>CLAUDE.md §2.2 T4</c>. Order is
    /// stable and used both by <see cref="CoverageMatrix.MetaPatterns"/> and by
    /// the gap-listing Cartesian product so downstream consumers (spec doc,
    /// test fixtures) see a deterministic order.
    /// </summary>
    public static readonly IReadOnlyList<string> CanonicalMetaPatterns =
        new[] { "Mono", "Inv", "Conv" };

    /// <summary>Slug attached to MRs whose manifest <c>meta_pattern</c> field is empty.</summary>
    public const string UnclassifiedMetaPattern = "Unclassified";

    /// <summary>
    /// Build a <see cref="CoverageMatrix"/> from the provider's currently loaded
    /// MR entries. The matrix is computed over the Cartesian product of the
    /// distinct equation keys found in the catalog × the <see cref="CanonicalMetaPatterns"/>
    /// triple; cells with at least one MR are <em>filled</em>, cells with zero are
    /// <em>gaps</em>. MRs whose <see cref="MrCatalogEntry.MetaPattern"/> is empty
    /// are surfaced under <see cref="UnclassifiedMetaPattern"/> (extra cell, not
    /// counted as a gap candidate — fix the manifest instead).
    /// </summary>
    public static CoverageMatrix Audit(IMrCatalogProvider provider)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));

        var entries = provider.Load();

        // 1. Group MRs by (equation, meta-pattern).
        var grouped = new Dictionary<(string Equation, string MetaPattern), List<string>>();
        foreach (var entry in entries)
        {
            var equationKey = entry.EquationKey ?? string.Empty;
            var metaPattern = string.IsNullOrWhiteSpace(entry.MetaPattern)
                ? UnclassifiedMetaPattern
                : entry.MetaPattern;
            var key = (equationKey, metaPattern);
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<string>();
                grouped[key] = list;
            }
            list.Add(entry.Mr.Id);
        }

        // 2. Materialize the filled cells, ordered by (equation, meta-pattern) for
        //    deterministic spec-doc rendering and downstream consumers.
        var cells = grouped
            .OrderBy(kv => kv.Key.Equation, StringComparer.Ordinal)
            .ThenBy(kv => kv.Key.MetaPattern, StringComparer.Ordinal)
            .Select(kv => new CoverageCell(
                kv.Key.Equation,
                kv.Key.MetaPattern,
                kv.Value.OrderBy(id => id, StringComparer.Ordinal).ToList()))
            .ToList();

        // 3. Compute gaps over the {distinct equations} × {Mono, Inv, Conv} Cartesian
        //    product. Equations only appear after at least one MR is registered
        //    against them (no speculative "all reactor anchors" axis here — the gap
        //    list answers "where can the next MR drop without a new SUT", not
        //    "what's missing across all 5 anchors plus 11 PDE classes".)
        //    Unclassified rows do not create gap candidates.
        var equations = entries
            .Select(e => e.EquationKey ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        var filledKeys = new HashSet<(string, string)>(
            cells.Select(c => (c.EquationKey, c.MetaPattern)));

        var gaps = new List<(string EquationKey, string MetaPattern)>();
        foreach (var eq in equations)
        {
            foreach (var mp in CanonicalMetaPatterns)
            {
                if (!filledKeys.Contains((eq, mp)))
                {
                    gaps.Add((eq, mp));
                }
            }
        }

        return new CoverageMatrix(cells, gaps, equations, CanonicalMetaPatterns);
    }
}

/// <summary>
/// Single filled cell of the (equation × meta-pattern) matrix: lists every MR id
/// bound to this equation under this meta-pattern.
/// </summary>
public sealed record CoverageCell(
    string EquationKey,
    string MetaPattern,
    IReadOnlyList<string> MrIds);

/// <summary>
/// Snapshot of the (equation × meta-pattern) coverage matrix at the time
/// <see cref="MetaPatternMatrixAuditor.Audit"/> ran.
/// </summary>
/// <param name="Cells">Filled cells (at least one MR each), sorted by (equation, meta-pattern).</param>
/// <param name="Gaps">Empty cells in the Cartesian product over the equations actually present in the catalog, sorted by (equation, meta-pattern).</param>
/// <param name="Equations">Distinct equation keys present in the catalog, sorted.</param>
/// <param name="MetaPatterns">Canonical meta-pattern slugs (Mono / Inv / Conv).</param>
public sealed record CoverageMatrix(
    IReadOnlyList<CoverageCell> Cells,
    IReadOnlyList<(string EquationKey, string MetaPattern)> Gaps,
    IReadOnlyList<string> Equations,
    IReadOnlyList<string> MetaPatterns);
