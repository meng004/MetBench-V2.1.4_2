namespace MetBench_BLL.SystemMT.Catalog.Binding;

/// <summary>
/// One field-level error emitted by <see cref="IDiscoveredMrCatalogBinder.Bind"/>. The
/// binder collects all errors in a single pass rather than throwing, so callers see the
/// full list and can present every violation to the discovery operator at once.
/// </summary>
/// <param name="Field">
/// Name of the offending field on <see cref="DiscoveredMrBindingDraft"/> (use
/// <c>nameof(DiscoveredMrBindingDraft.Xyz)</c> so static rename refactors stay in sync).
/// </param>
/// <param name="Message">
/// Human-readable description naming the rule that was violated and the offending value
/// where appropriate (e.g. "AssertionTypeCode 'approximately-equal' is not a recognized code").
/// </param>
public sealed record DiscoveredMrBindingError(string Field, string Message);
