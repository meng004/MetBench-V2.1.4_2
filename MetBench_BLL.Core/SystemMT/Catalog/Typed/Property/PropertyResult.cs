using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Property;

public sealed record PropertyResult(
    string PropertyId,
    PropertyStatus Status,
    IReadOnlyList<PropertyPredicateResult> PredicateResults,
    string? Reason = null)
{
    public bool Passed => Status == PropertyStatus.Held;
}

public sealed record PropertyPredicateResult(
    string PredicateId,
    string PredicateKind,
    PropertyStatus Status,
    object? Actual = null,
    object? Expected = null,
    double? Residual = null,
    double? Tolerance = null,
    string? Reason = null);
