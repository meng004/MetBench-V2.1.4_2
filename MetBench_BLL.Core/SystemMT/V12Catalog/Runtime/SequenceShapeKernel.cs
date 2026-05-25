using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class SequenceShapeKernel
{
    public VerificationResult Evaluate(ShapeSpec shape, SequenceValue sequence)
    {
        var samples = sequence.Samples;
        if (samples.Count < 2)
        {
            var invalid = new SystemMtAssertionResultV2(
                "SequenceShape",
                false,
                samples.Count,
                samples.Count,
                0.0,
                0.0,
                "Sequence must contain at least two samples.",
                "sample_count=1");

            return VerificationResult.FromAssertion(
                invalid,
                new VerificationDiagnostic(samples.Count, samples.Count, 0.0, 0.0));
        }

        var (passed, worstPoint) = shape switch
        {
            BellShapeSpec => EvaluateBell(samples),
            SShapeSpec => EvaluateSShape(samples),
            SignChangeSpec => EvaluateSignChange(samples),
            NonMonotonicSpec => EvaluateNonMonotonic(samples),
            ConstantSlopeSpec => EvaluateConstantSlope(samples),
            _ => throw new ArgumentException($"Unsupported shape '{shape.GetType().Name}'.", nameof(shape))
        };

        var assertion = new SystemMtAssertionResultV2(
            "SequenceShape",
            passed,
            samples.Count,
            samples.Count,
            passed ? 0.0 : 1.0,
            0.0,
            $"{shape.GetType().Name} over sequence",
            passed ? null : $"sample_count={samples.Count}; worst_point={worstPoint}");

        return VerificationResult.FromAssertion(
            assertion,
            new VerificationDiagnostic(samples.Count, samples.Count, passed ? 0.0 : 1.0, 0.0));
    }

    private static (bool Passed, int WorstPoint) EvaluateBell(IReadOnlyList<double> samples) =>
        Wrap(samples, IsBellShape);

    private static (bool Passed, int WorstPoint) EvaluateSShape(IReadOnlyList<double> samples) =>
        Wrap(SecondDifferences(samples), HasSignChange);

    private static (bool Passed, int WorstPoint) EvaluateSignChange(IReadOnlyList<double> samples) =>
        Wrap(samples, HasSignChange);

    private static (bool Passed, int WorstPoint) EvaluateNonMonotonic(IReadOnlyList<double> samples) =>
        Wrap(samples, IsNonMonotonic);

    private static (bool Passed, int WorstPoint) EvaluateConstantSlope(IReadOnlyList<double> samples) =>
        Wrap(samples, HasConstantSlope);

    private delegate bool ShapeEvaluator(IReadOnlyList<double> samples, out int worstPoint);

    private static (bool Passed, int WorstPoint) Wrap(
        IReadOnlyList<double> samples,
        ShapeEvaluator evaluator)
    {
        var passed = evaluator(samples, out var worstPoint);
        return (passed, worstPoint);
    }

    private static bool IsBellShape(IReadOnlyList<double> samples, out int worstPoint)
    {
        worstPoint = IndexOfMax(samples);
        if (worstPoint <= 0 || worstPoint >= samples.Count - 1)
        {
            return false;
        }

        for (var i = 1; i <= worstPoint; i++)
        {
            if (samples[i] <= samples[i - 1])
            {
                worstPoint = i;
                return false;
            }
        }

        for (var i = worstPoint + 1; i < samples.Count; i++)
        {
            if (samples[i] >= samples[i - 1])
            {
                worstPoint = i;
                return false;
            }
        }

        return true;
    }

    private static bool HasSignChange(IReadOnlyList<double> samples, out int worstPoint)
    {
        worstPoint = -1;
        for (var i = 1; i < samples.Count; i++)
        {
            if (Math.Sign(samples[i - 1]) != Math.Sign(samples[i]))
            {
                worstPoint = i;
                return true;
            }
        }

        worstPoint = samples.Count - 1;
        return false;
    }

    private static bool IsNonMonotonic(IReadOnlyList<double> samples, out int worstPoint)
    {
        var hasIncrease = false;
        var hasDecrease = false;
        worstPoint = -1;

        for (var i = 1; i < samples.Count; i++)
        {
            if (samples[i] > samples[i - 1])
            {
                hasIncrease = true;
            }
            else if (samples[i] < samples[i - 1])
            {
                hasDecrease = true;
            }

            if (hasIncrease && hasDecrease)
            {
                worstPoint = i;
                return true;
            }
        }

        worstPoint = samples.Count - 1;
        return false;
    }

    private static bool HasConstantSlope(IReadOnlyList<double> samples, out int worstPoint)
    {
        var diffs = FirstDifferences(samples);
        var baseline = diffs[0];
        worstPoint = -1;

        for (var i = 1; i < diffs.Count; i++)
        {
            if (Math.Abs(diffs[i] - baseline) > 1e-9)
            {
                worstPoint = i + 1;
                return false;
            }
        }

        return true;
    }

    private static List<double> FirstDifferences(IReadOnlyList<double> samples)
    {
        var result = new List<double>(samples.Count - 1);
        for (var i = 1; i < samples.Count; i++)
        {
            result.Add(samples[i] - samples[i - 1]);
        }

        return result;
    }

    private static List<double> SecondDifferences(IReadOnlyList<double> samples) => FirstDifferences(FirstDifferences(samples));

    private static int IndexOfMax(IReadOnlyList<double> samples)
    {
        var index = 0;
        for (var i = 1; i < samples.Count; i++)
        {
            if (samples[i] > samples[index])
            {
                index = i;
            }
        }

        return index;
    }
}
