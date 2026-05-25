namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Persistence-shaped projection of
/// <c>MetBench_BLL.SystemMT.Catalog.Typed.Property.PropertyPredicateResult</c>.
/// Object-typed <c>Expected</c> / <c>Actual</c> fields are serialised to JSON
/// with <see cref="System.Globalization.CultureInfo.InvariantCulture"/> so the
/// stored representation is culture-independent.
/// </summary>
public sealed class TypedPropertyPredicateEvidence
{
    public string PredicateId { get; set; } = string.Empty;

    public string PredicateKind { get; set; } = string.Empty;

    /// <summary>Name of the <c>PropertyStatus</c> enum value (Held / Violated / SkippedMissingObservable / InvalidSpec).</summary>
    public string Status { get; set; } = string.Empty;

    public double? Residual { get; set; }

    public double? Tolerance { get; set; }

    public string? Reason { get; set; }

    /// <summary>System.Text.Json-serialised <c>Expected</c> object; null when not provided.</summary>
    public string? ExpectedJson { get; set; }

    /// <summary>System.Text.Json-serialised <c>Actual</c> object; null when not provided.</summary>
    public string? ActualJson { get; set; }
}
