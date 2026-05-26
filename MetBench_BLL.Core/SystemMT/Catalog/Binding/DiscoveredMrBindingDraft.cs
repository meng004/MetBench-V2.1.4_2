using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Binding;

/// <summary>
/// Deterministic input DTO for <see cref="IDiscoveredMrCatalogBinder.Bind"/>. Represents
/// a T4 MR discovery candidate ready for evaluation against the T0 System MT catalog
/// schema. The binder consumes this DTO and emits either a manifest-compatible
/// <see cref="MrBindingDefinition"/> + provenance, or a list of field-level errors.
///
/// All fields are required; the binder fails closed on blank required strings,
/// unsupported assertion codes, out-of-range confidence, unsafe sample paths, etc.
/// </summary>
public sealed record DiscoveredMrBindingTransformStep(
    string TransformationName,
    string TargetFieldPath);

/// <summary>
/// Discovery candidate handed to the T4-to-T0 binder.
/// </summary>
public sealed record DiscoveredMrBindingDraft(
    string SutId,
    string MrId,
    string DisplayName,
    string EquationKey,
    string MetaPattern,
    string TransformationName,
    string AssertionTypeCode,
    string ValueName,
    string SampleCaseRelativePath,
    string WorkRootName,
    int TimeoutSeconds,
    IReadOnlyDictionary<string, string> DefaultParameters,
    IReadOnlyList<DiscoveredMrBindingTransformStep> TransformSteps,
    string DiscoveryMethod,
    string DiscoveryRunId,
    double Confidence);
