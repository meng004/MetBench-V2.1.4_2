using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class SharedReferenceResolver
{
    public bool RoleExists(MrSpec spec, string role) =>
        spec.Roles is not null && spec.Roles.ContainsKey(role);

    public bool MetricExists(MrSpec spec, string metric) =>
        spec.Projections is not null && spec.Projections.ContainsKey(metric);

    public bool MetricExists(PropertySpec spec, string metric) =>
        spec.Projections is not null && spec.Projections.ContainsKey(metric);
}
