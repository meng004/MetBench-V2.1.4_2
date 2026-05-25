using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed record RoleOutput(
    string RoleName,
    IReadOnlyDictionary<string, double> Metrics,
    IReadOnlyDictionary<string, Field2DValue>? Fields = null,
    IReadOnlyDictionary<string, StatisticalValue>? Statistics = null);
