using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

public sealed record ApplicabilitySpec(
    IReadOnlyList<ConditionExpr> Conditions);
