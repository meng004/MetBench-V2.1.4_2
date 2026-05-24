using System.Collections.Generic;
using System.Text.Json.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Validation;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

public sealed record PropertySpec(
    string Kind,
    string PropertyId,
    string Name,
    string? Description,
    IReadOnlyDictionary<string, string>? Tags,
    PropertyCaseSpec? Case,
    IReadOnlyDictionary<string, ProjectionSpec>? Projections,
    IReadOnlyList<PropertyPredicateSpec> Assertions,
    ToleranceSpec? DefaultTolerance)
{
    [JsonIgnore]
    public FiveDTags FiveDTags => FiveDTags.FromTags(Tags);

    public ValidationResult Validate() => new PropertySpecValidator(ValidationRegistry.Default).Validate(this);
}

public sealed record PropertyCaseSpec(string Kind);
