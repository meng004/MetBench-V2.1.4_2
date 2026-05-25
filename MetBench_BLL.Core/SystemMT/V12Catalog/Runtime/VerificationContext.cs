using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class VerificationContext
{
    public VerificationContext(MrSpec spec, IReadOnlyDictionary<string, RoleOutput> roleOutputs)
    {
        Spec = spec ?? throw new ArgumentException("Validated spec is required.", nameof(spec));
        RoleOutputs = roleOutputs ?? throw new ArgumentNullException(nameof(roleOutputs));

        var validation = spec.Validate();
        if (!validation.IsValid)
        {
            throw new ArgumentException("VerificationContext requires a validated spec.", nameof(spec));
        }
    }

    public MrSpec Spec { get; }
    public IReadOnlyDictionary<string, RoleOutput> RoleOutputs { get; }

    public double GetMetric(string role, string metric)
    {
        if (!RoleOutputs.TryGetValue(role, out var roleOutput))
        {
            throw new ArgumentException($"Unknown role output '{role}'.", nameof(role));
        }

        if (!roleOutput.Metrics.TryGetValue(metric, out var value))
        {
            throw new ArgumentException($"Unknown metric '{metric}' for role '{role}'.", nameof(metric));
        }

        return value;
    }
}
