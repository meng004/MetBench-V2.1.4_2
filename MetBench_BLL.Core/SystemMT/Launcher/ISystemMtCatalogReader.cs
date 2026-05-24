using MetBench_BLL.SystemMT.Catalog;

namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Read-only view of the runnable System-MT catalog for import / bootstrap workflows.
/// </summary>
public interface ISystemMtCatalogReader
{
    IReadOnlyList<MrCatalogEntry> GetCatalogEntries();
}
