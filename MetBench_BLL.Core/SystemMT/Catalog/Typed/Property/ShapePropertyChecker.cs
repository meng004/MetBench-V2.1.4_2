using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Property;

public sealed class ShapePropertyChecker : IPropertyChecker
{
    private readonly SequenceShapeKernel _shapeKernel = new();

    public PropertyResult Check(PropertySpec spec, PropertyVerificationContext context)
    {
        if (!context.SpecValidation.IsValid)
        {
            return new PropertyResult(spec.PropertyId, PropertyStatus.InvalidSpec, Array.Empty<PropertyPredicateResult>(), "Property validation failed before runtime checking.");
        }

        var results = new List<PropertyPredicateResult>();
        foreach (var assertion in spec.Assertions)
        {
            if (assertion is not ShapePropertyPredicate shape)
            {
                continue;
            }

            if (!context.TryGetSequence(shape.Metric, out var sequence))
            {
                results.Add(new PropertyPredicateResult(shape.PredicateId, nameof(ShapePropertyPredicate), PropertyStatus.SkippedMissingObservable, Reason: $"Missing sequence '{shape.Metric}'."));
                return new PropertyResult(spec.PropertyId, PropertyStatus.SkippedMissingObservable, results, $"Missing sequence '{shape.Metric}'.");
            }

            VerificationResult verification;
            try
            {
                verification = _shapeKernel.Evaluate(shape.Shape, sequence);
            }
            catch (ArgumentException ex)
            {
                results.Add(new PropertyPredicateResult(shape.PredicateId, nameof(ShapePropertyPredicate), PropertyStatus.InvalidSpec, Reason: ex.Message));
                return new PropertyResult(spec.PropertyId, PropertyStatus.InvalidSpec, results, ex.Message);
            }
            var status = verification.Status switch
            {
                VerifyStatus.Passed => PropertyStatus.Held,
                VerifyStatus.Failed => PropertyStatus.Violated,
                VerifyStatus.SkippedMissingObservable => PropertyStatus.SkippedMissingObservable,
                _ => PropertyStatus.InvalidSpec
            };

            results.Add(new PropertyPredicateResult(
                shape.PredicateId,
                nameof(ShapePropertyPredicate),
                status,
                Actual: verification.Diagnostic?.Actual,
                Expected: verification.Diagnostic?.Expected,
                Residual: verification.Diagnostic?.Residual,
                Tolerance: verification.Diagnostic?.Tolerance,
                Reason: verification.Assertion?.FailureReason ?? verification.Assertion?.Expression ?? verification.Context?.Reason));

            if (status != PropertyStatus.Held)
            {
                return new PropertyResult(spec.PropertyId, status, results, verification.Assertion?.FailureReason ?? verification.Assertion?.Expression ?? verification.Context?.Reason);
            }
        }

        return new PropertyResult(spec.PropertyId, PropertyStatus.Held, results);
    }
}
