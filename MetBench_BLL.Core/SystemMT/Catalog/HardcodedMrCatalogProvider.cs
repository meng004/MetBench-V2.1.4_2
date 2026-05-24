using System;
using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Transitional <see cref="IMrCatalogProvider"/> that wraps the legacy hardcoded
/// blueprints from <see cref="LegacyCatalogFactory"/>. Same 17 MR × 9 SUT entries
/// as the pre-Phase-B launcher; provided so Task 3 can switch the launcher to
/// provider-backed loading without changing behavior. Slated for removal in Task 7
/// once the manifest-backed catalog has shipped.
/// </summary>
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
