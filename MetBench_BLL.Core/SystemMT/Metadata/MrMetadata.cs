namespace MetBench_BLL.SystemMT.Metadata;

/// <summary>
/// Which numeric comparison an MR's assertion performs — recorded as metadata
/// so the relation's expected-output semantics are queryable, not buried in
/// the assertion name.
/// </summary>
public enum MrComparisonType
{
    /// <summary>
    /// Monotone order comparison (GreaterThan / LessThan) — no tolerance. The
    /// whole current catalog is ordinal; the two tolerance modes below land
    /// with the deferred scaled-equality MRs.
    /// </summary>
    Ordinal,

    /// <summary>Equality within a fixed absolute numeric threshold.</summary>
    Absolute,

    /// <summary>Equality within a tolerated relative deviation ratio.</summary>
    Relative,
}

/// <summary>
/// Structured, persistable description of one metamorphic relation in the
/// System-MT catalog. Annotates a catalog entry (by <see cref="MrId"/>, which
/// matches <c>MrSummary.Id</c>) with parameter semantics, the input/output
/// transformation relations, and the comparison type — the MR metadata the
/// MR/program metadata plan calls for.
/// </summary>
public sealed class MrMetadata
{
    /// <summary>
    /// LiteDB document identity. Left <see cref="Guid.Empty"/> by callers;
    /// the repository assigns a fresh Guid on first insert.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Stable MR id — matches the catalog <c>MrSummary.Id</c>. Unique.
    /// </summary>
    public string MrId { get; set; } = string.Empty;

    /// <summary>
    /// Key of the equation this MR exercises — joins to
    /// <see cref="EquationMetadata.EquationKey"/>.
    /// </summary>
    public string EquationKey { get; set; } = string.Empty;

    /// <summary>Prose statement of the relation's physical meaning.</summary>
    public string PhysicalMeaning { get; set; } = string.Empty;

    /// <summary>How the source input is transformed into the follow-up input.</summary>
    public string InputTransformation { get; set; } = string.Empty;

    /// <summary>The expected relation between source and follow-up outputs.</summary>
    public string OutputRelation { get; set; } = string.Empty;

    /// <summary>Whether the assertion compares absolutely or relatively.</summary>
    public MrComparisonType ComparisonType { get; set; }

    /// <summary>Why the MR belongs to its declared meta-pattern family.</summary>
    public string MetaPatternRationale { get; set; } = string.Empty;

    /// <summary>Audit prose for source-to-follow-up input semantics.</summary>
    public string TransformationSemantics { get; set; } = string.Empty;

    /// <summary>Audit prose for the observed source/follow-up output quantity.</summary>
    public string ObservableSummary { get; set; } = string.Empty;

    /// <summary>Audit prose for the predicate and comparison semantics.</summary>
    public string PredicateSummary { get; set; } = string.Empty;

    /// <summary>Audit prose for tolerance/noise treatment.</summary>
    public string ToleranceSummary { get; set; } = string.Empty;

    /// <summary>Applicability and non-applicability conditions for the MR.</summary>
    public string Applicability { get; set; } = string.Empty;

    /// <summary>What an MR failure means for the SUT or adapter under test.</summary>
    public string FailureMeaning { get; set; } = string.Empty;

    /// <summary>Per-parameter symbols, physical meaning and value ranges.</summary>
    public List<MrParameter> Parameters { get; set; } = new();
}

/// <summary>One parameter referenced by an MR.</summary>
public sealed class MrParameter
{
    public string Symbol { get; set; } = string.Empty;

    public string PhysicalMeaning { get; set; } = string.Empty;

    public string ValueRange { get; set; } = string.Empty;
}
