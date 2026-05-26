using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Binding;

/// <summary>
/// Outcome of <see cref="IDiscoveredMrCatalogBinder.Bind"/>. Either a manifest-compatible
/// <see cref="MrBindingDefinition"/> + <see cref="DiscoveredMrBindingProvenance"/> (when
/// <see cref="Success"/> is <c>true</c>), or a non-empty list of field-level errors.
/// Never both at the same time: when errors are present, <see cref="Binding"/> and
/// <see cref="Provenance"/> are both <c>null</c>.
/// </summary>
public sealed record DiscoveredMrBindingResult(
    MrBindingDefinition? Binding,
    DiscoveredMrBindingProvenance? Provenance,
    IReadOnlyList<DiscoveredMrBindingError> Errors)
{
    /// <summary>
    /// True iff the binder accepted the draft and emitted a binding plus provenance.
    /// </summary>
    public bool Success => Binding is not null && Provenance is not null && Errors.Count == 0;
}
