using System.Collections.Generic;

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
    ToleranceSpec? DefaultTolerance);

public sealed record PropertyCaseSpec(string Kind);
