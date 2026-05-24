using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

public sealed record TransformStepSpec(
    string TransformationName,
    string? TargetPath,
    IReadOnlyDictionary<string, ParameterExpression>? Parameters);
