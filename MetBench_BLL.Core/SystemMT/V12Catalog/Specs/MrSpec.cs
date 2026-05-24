using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

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
    ToleranceSpec? DefaultTolerance);

public sealed record RunRoleSpec(string Kind);
