using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// ExecutionEvidence v2 typed-verification block. Captures the typed
/// <c>VerificationResult</c> (for MR specs) or <c>PropertyResult</c> (for
/// property specs) produced by the Typed Semantic Catalog runtime.
/// </summary>
/// <remarks>
/// Persistence-shaped POCO with parameterless ctor and public setters so
/// LiteDB's default <c>BsonMapper</c> handles it without bespoke registration.
/// All Typed-Catalog engine internals are projected to primitive types or
/// JSON strings — the persistence layer never references typed-runtime types.
/// </remarks>
public sealed class TypedVerificationEvidence
{
    /// <summary>MR spec or Property spec id (e.g. <c>"heat-equation-amplitude"</c>).</summary>
    public string SpecId { get; set; } = string.Empty;

    /// <summary>Discriminator: <c>"MrSpec"</c> or <c>"PropertySpec"</c>.</summary>
    public string SpecKind { get; set; } = string.Empty;

    /// <summary>For <c>MrSpec</c>: the dispatched predicate id; empty for <c>PropertySpec</c>.</summary>
    public string PredicateId { get; set; } = string.Empty;

    /// <summary>For <c>MrSpec</c>: e.g. <c>"BinaryComparison"</c>, <c>"ScaledEquality"</c>; empty for <c>PropertySpec</c>.</summary>
    public string PredicateKind { get; set; } = string.Empty;

    /// <summary>
    /// Name of the <c>VerifyStatus</c> value (Passed / Failed / SkippedNotApplicable /
    /// SkippedMissingObservable / InvalidSpec) for MR specs, or
    /// <c>PropertyStatus</c> (Held / Violated / SkippedMissingObservable / InvalidSpec)
    /// for property specs.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Mirrors Status == Passed / Held; null when Skipped*/InvalidSpec.</summary>
    public bool? Passed { get; set; }

    /// <summary>Populated for Passed / Failed; null for Skipped*/InvalidSpec or PropertySpec rows.</summary>
    public TypedDiagnosticEvidence? Diagnostic { get; set; }

    /// <summary>Populated when Status is Skipped*/InvalidSpec; equals the typed runtime's <c>DiagnosticContext.Reason</c>.</summary>
    public string? SkipOrInvalidReason { get; set; }

    /// <summary>Ordered projection of <c>PropertyResult.PredicateResults</c>; empty for MR specs.</summary>
    public List<TypedPropertyPredicateEvidence> PropertyPredicates { get; set; } = new();
}
