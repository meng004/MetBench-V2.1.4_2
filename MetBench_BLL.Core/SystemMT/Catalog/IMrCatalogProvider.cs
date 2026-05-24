using System.Collections.Generic;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Source of runnable System-MT catalog entries (program / MR bindings).
/// Phase A introduces the boundary; Phase B fills in the hardcoded and
/// manifest-backed implementations.
/// </summary>
/// <remarks>
/// Public so WPF (MetBench_Client) can register concrete providers via DI
/// in <c>App.xaml.cs</c>; the launcher consumes the abstraction as a
/// constructor dependency from Task 3 onward.
/// </remarks>
public interface IMrCatalogProvider
{
    /// <summary>Source description for diagnostics (e.g. "Hardcoded" or "Manifest:SUT/.../catalog.json").</summary>
    string SourceDescription { get; }

    /// <summary>Load all runnable catalog entries.</summary>
    IReadOnlyList<MrCatalogEntry> Load();
}
