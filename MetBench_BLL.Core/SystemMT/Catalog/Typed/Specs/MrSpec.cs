using System.Collections.Generic;
using System.Text.Json.Serialization;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

public sealed record MrSpec(
    string Kind,
    string MrId,
    string Name,
    string? Description,
    IReadOnlyDictionary<string, string>? Tags,
    IReadOnlyDictionary<string, ParameterExpression>? Parameters,
    IReadOnlyDictionary<string, RunRoleSpec>? Roles,
    IReadOnlyDictionary<string, ProjectionSpec>? Projections,
    IReadOnlyList<PredicateSpec> Predicates,
    ToleranceSpec? DefaultTolerance)
{
    public ApplicabilitySpec? Applicability { get; init; }

    [JsonIgnore]
    public FiveDTags FiveDTags => FiveDTags.FromTags(Tags);

    public ValidationResult Validate() => new MrSpecValidator(ValidationRegistry.Default).Validate(this);
}

public sealed record RunRoleSpec(string Kind)
{
    public MethodBinding? MethodBinding { get; init; }
    public IReadOnlyList<TransformStepSpec>? Transforms { get; init; }
}
