namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public enum ProgramKind
{
    NumericalSolver,
    MonteCarlo,
    Surrogate
}

public enum ObservableKind
{
    Scalar,
    Series,
    Vector,
    Summary
}

public enum MutationRepresentationKind
{
    OperatorClassOnly,
    Patch,
    ParameterChange
}

public enum EvidenceKind
{
    ImportedResearchEvidence,
    LocalExecutionEvidence
}

public enum DetectionResult
{
    Detected,
    Survived,
    Inconclusive
}

public enum CompatibilityStatus
{
    NativeSupported,
    MappedSupported,
    RequiresAdapter,
    Unsupported,
    ImportedOnly
}

public enum RuntimeReadiness
{
    ImportedOnly,
    RuntimeCandidate
}

public sealed record SutImportUnit(
    string SchemaVersion,
    SutAsset Sut,
    IReadOnlyList<MrAsset> Mrs,
    IReadOnlyList<IoGroup> IoGroups,
    IReadOnlyList<MutationAsset> Mutations,
    IReadOnlyList<DetectionRecord> Detections,
    Provenance Provenance,
    CompatibilityProfile Compatibility)
{
    public const string CurrentSchemaVersion = "put-import.v1";
}

public sealed record SutAsset(
    string SutId,
    string Name,
    string EquationFamily,
    ProgramKind ProgramKind,
    AdapterSpec Adapter,
    IReadOnlyList<ObservableSpec> Observables,
    IReadOnlyDictionary<string, string> Metadata)
{
    public bool SupportsObservable(string observableName)
    {
        return Observables.Any(o => string.Equals(o.Name, observableName, StringComparison.Ordinal));
    }
}

public sealed record AdapterSpec(
    string AdapterId,
    string Description,
    string SourcePath,
    IReadOnlyList<string> EntryPoints);

public sealed record ObservableSpec(
    string Name,
    ObservableKind Kind,
    string Unit,
    string Description);

public sealed record MrAsset(
    string MrId,
    string SutId,
    string Name,
    IReadOnlyList<string> ObservableRefs,
    TransformBinding TransformBinding,
    AssertionBinding AssertionBinding,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record IoGroup(
    string IoGroupId,
    string SutId,
    string Name,
    IReadOnlyList<string> InputRefs,
    IReadOnlyList<string> OutputRefs,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record MutationAsset(
    string MutationId,
    string SutId,
    string Name,
    string OperatorClass,
    MutationRepresentationKind RepresentationKind,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record DetectionRecord(
    string DetectionId,
    string MrId,
    string MutationId,
    string IoGroupId,
    DetectionResult Result,
    EvidenceKind EvidenceKind,
    string Notes);

public sealed record Provenance(
    string RepositoryUrl,
    string Commit,
    IReadOnlyList<string> SourcePaths,
    string CapturedBy,
    DateTimeOffset CapturedAtUtc)
{
    public static Provenance Empty { get; } = new(
        string.Empty,
        string.Empty,
        Array.Empty<string>(),
        string.Empty,
        DateTimeOffset.MinValue);
}

public sealed record TransformBinding(
    CompatibilityStatus Status,
    string TransformKind,
    ShapeSpec? Shape = null,
    string Notes = "")
{
    public static TransformBinding ImportedOnly(string reason)
    {
        return new TransformBinding(CompatibilityStatus.ImportedOnly, "ImportedOnly", null, reason);
    }
}

public sealed record AssertionBinding(
    CompatibilityStatus Status,
    string PredicateKind,
    string Metric = "",
    ToleranceSpec? Tolerance = null,
    string Notes = "")
{
    public static AssertionBinding ImportedOnly(string reason)
    {
        return new AssertionBinding(CompatibilityStatus.ImportedOnly, "ImportedOnly", string.Empty, null, reason);
    }
}

public sealed record ToleranceSpec(
    string Kind,
    double Value)
{
    public static ToleranceSpec Absolute(double value)
    {
        return new ToleranceSpec("absolute", value);
    }

    public static ToleranceSpec Relative(double value)
    {
        return new ToleranceSpec("relative", value);
    }
}

public sealed record ShapeSpec(
    string Kind)
{
    public static ShapeSpec Scalar()
    {
        return new ShapeSpec("scalar");
    }

    public static ShapeSpec ScalarSeries()
    {
        return new ShapeSpec("scalar-series");
    }
}

public sealed record CompatibilityProfile(
    RuntimeReadiness OverallReadiness,
    IReadOnlyList<string> Findings)
{
    public bool CanPromoteToRuntimeCandidate()
    {
        return OverallReadiness == RuntimeReadiness.RuntimeCandidate;
    }
}

public sealed record SutImportValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static SutImportValidationResult Valid { get; } = new(true, Array.Empty<string>());

    public static SutImportValidationResult Invalid(IEnumerable<string> errors)
    {
        var materialized = errors.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
        return materialized.Length == 0 ? Valid : new SutImportValidationResult(false, materialized);
    }
}
