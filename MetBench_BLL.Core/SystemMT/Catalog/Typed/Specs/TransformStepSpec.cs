using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

public sealed record TransformStepSpec(
    string TransformationName,
    string? TargetPath,
    IReadOnlyDictionary<string, ParameterExpression>? Parameters);
