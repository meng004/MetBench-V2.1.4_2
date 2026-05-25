using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

public sealed record ApplicabilitySpec(
    IReadOnlyList<ConditionExpr> Conditions);
