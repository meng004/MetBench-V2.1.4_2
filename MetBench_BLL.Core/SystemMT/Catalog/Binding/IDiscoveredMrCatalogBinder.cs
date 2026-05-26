namespace MetBench_BLL.SystemMT.Catalog.Binding;

/// <summary>
/// Controlled boundary between T4 MR discovery and T0 System MT catalog assets.
/// Consumes a deterministic <see cref="DiscoveredMrBindingDraft"/>, validates it against
/// the same schema rules the production catalog enforces, and emits either a
/// manifest-compatible <see cref="MrBindingDefinition"/> + provenance, or a list of
/// field-level errors. The binder is the only sanctioned bridge between the two
/// subsystems and must never mutate active <c>SUT/&lt;sut&gt;/catalog.json</c> files.
/// </summary>
public interface IDiscoveredMrCatalogBinder
{
    DiscoveredMrBindingResult Bind(DiscoveredMrBindingDraft draft);
}
