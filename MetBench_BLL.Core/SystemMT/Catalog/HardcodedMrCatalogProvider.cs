using System;
using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Transitional <see cref="IMrCatalogProvider"/> that wraps the legacy hardcoded
/// blueprints from <see cref="LegacyCatalogFactory"/>. 19 MR × 10 SUT entries
/// (17 + 2 from T3 Poisson 1D); provided so Task 3 can switch the launcher to
/// provider-backed loading without changing behavior. Slated for removal in Task 7
/// once the manifest-backed catalog has shipped and is the WPF default.
/// </summary>
[Obsolete(
    "Transitional Phase B/C wrapper around the hardcoded LegacyCatalogFactory. " +
    "New code should depend on ManifestMrCatalogProvider (data-driven SUT/<sut>/catalog.json). " +
    "Slated for removal in Task 7 of the catalog convergence plan; LegacyCatalogFactory + " +
    "this wrapper will be deleted once VM-side DI registers ManifestMrCatalogProvider explicitly.")]
public sealed class HardcodedMrCatalogProvider : IMrCatalogProvider
{
    private readonly LauncherOptions _options;

    public HardcodedMrCatalogProvider(LauncherOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string SourceDescription => "Hardcoded(LegacyCatalogFactory)";

    public IReadOnlyList<MrCatalogEntry> Load() =>
        LegacyCatalogFactory.Build(_options)
            .Select(MrCatalogEntry.FromBlueprint)
            .ToList();
}
