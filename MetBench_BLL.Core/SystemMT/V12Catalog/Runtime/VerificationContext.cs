using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using MetBench_BLL.SystemMT.V12Catalog.Validation;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class VerificationContext
{
    public VerificationContext(
        MrSpec spec,
        IReadOnlyDictionary<string, RoleOutput> roleOutputs,
        IReadOnlyDictionary<string, double>? inputs = null)
    {
        Spec = spec ?? throw new ArgumentException("Validated spec is required.", nameof(spec));
        RoleOutputs = roleOutputs ?? throw new ArgumentNullException(nameof(roleOutputs));
        Inputs = inputs ?? new Dictionary<string, double>();
        SpecValidation = spec.Validate();
    }

    public MrSpec Spec { get; }
    public IReadOnlyDictionary<string, RoleOutput> RoleOutputs { get; }
    public IReadOnlyDictionary<string, double> Inputs { get; }
    public ValidationResult SpecValidation { get; }

    public bool TryGetMetric(string role, string metric, out double value)
    {
        if (!RoleOutputs.TryGetValue(role, out var roleOutput))
        {
            value = default;
            return false;
        }

        if (!roleOutput.Metrics.TryGetValue(metric, out var metricValue))
        {
            value = default;
            return false;
        }

        value = metricValue;
        return true;
    }

    public double GetMetric(string role, string metric)
    {
        if (!TryGetMetric(role, metric, out var value))
        {
            throw new ArgumentException($"Unknown metric '{metric}' for role '{role}'.", nameof(metric));
        }

        return value;
    }

    public bool TryGetField(string role, string metric, out Field2DValue value)
    {
        if (!RoleOutputs.TryGetValue(role, out var roleOutput) ||
            roleOutput.Fields is null ||
            !roleOutput.Fields.TryGetValue(metric, out var fieldValue))
        {
            value = default!;
            return false;
        }

        value = fieldValue;
        return true;
    }

    public Field2DValue GetField(string role, string metric)
    {
        if (!TryGetField(role, metric, out var value))
        {
            throw new ArgumentException($"Unknown field '{metric}' for role '{role}'.", nameof(metric));
        }

        return value;
    }
}
