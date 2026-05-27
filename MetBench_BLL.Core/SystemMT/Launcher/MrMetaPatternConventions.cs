namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Maps an <see cref="MrSummary.MrFamily"/> slug (e.g. <c>Burgers.Invariance.Mass</c>,
/// <c>ScipyBvp.Poisson.Convergence.SeedMesh</c>) to its canonical meta-pattern
/// slug (<c>Mono</c> / <c>Inv</c> / <c>Conv</c>), per the convention documented in
/// <c>CLAUDE.md §2.2 T4</c>.
///
/// <para>
/// The convention is: each <c>MrFamily</c> contains exactly one segment matching
/// one of the canonical keywords <c>Scaling</c> / <c>Invariance</c> / <c>Convergence</c>.
/// Returns empty string when no segment matches — callers that need to fail
/// loud (e.g. <c>MetaPatternMatrixAuditor</c> guard tests) should treat empty as
/// "Unclassified" and surface it.
/// </para>
///
/// <para>
/// Used by <see cref="MrCatalogEntry.FromBlueprint(MrBlueprint)"/> to fill the
/// <see cref="MrCatalogEntry.MetaPattern"/> projection so the
/// <see cref="MetBench_BLL.SystemMT.Coverage.MetaPatternMatrixAuditor"/> produces
/// the same matrix regardless of whether entries come from the manifest-backed
/// <c>ManifestMrCatalogProvider</c> or the legacy <c>HardcodedMrCatalogProvider</c>.
/// </para>
/// </summary>
internal static class MrMetaPatternConventions
{
    public const string Mono = "Mono";
    public const string Inv = "Inv";
    public const string Conv = "Conv";

    public static string FromMrFamily(string mrFamily)
    {
        if (string.IsNullOrEmpty(mrFamily)) return string.Empty;
        var segments = mrFamily.Split('.');
        foreach (var segment in segments)
        {
            if (segment.Equals("Scaling", StringComparison.Ordinal)) return Mono;
            if (segment.Equals("Invariance", StringComparison.Ordinal)) return Inv;
            if (segment.Equals("Convergence", StringComparison.Ordinal)) return Conv;
        }
        return string.Empty;
    }
}
