using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Property;

public sealed class PropertyVerificationContext
{
    public PropertyVerificationContext(
        PropertySpec spec,
        IReadOnlyDictionary<string, double>? metrics = null,
        IReadOnlyDictionary<string, SequenceValue>? sequences = null)
    {
        Spec = spec ?? throw new ArgumentException("Validated property spec is required.", nameof(spec));
        Metrics = metrics ?? new Dictionary<string, double>();
        Sequences = sequences ?? new Dictionary<string, SequenceValue>();
        SpecValidation = spec.Validate();
    }

    public PropertySpec Spec { get; }
    public IReadOnlyDictionary<string, double> Metrics { get; }
    public IReadOnlyDictionary<string, SequenceValue> Sequences { get; }
    public ValidationResult SpecValidation { get; }

    public bool TryGetMetric(string metric, out double value) => Metrics.TryGetValue(metric, out value);

    public double GetMetric(string metric)
    {
        if (!TryGetMetric(metric, out var value))
        {
            throw new ArgumentException($"Unknown metric '{metric}'.", nameof(metric));
        }

        return value;
    }

    public bool TryGetSequence(string metric, out SequenceValue value) => Sequences.TryGetValue(metric, out value!);

    public SequenceValue GetSequence(string metric)
    {
        if (!TryGetSequence(metric, out var value))
        {
            throw new ArgumentException($"Unknown sequence '{metric}'.", nameof(metric));
        }

        return value;
    }
}
