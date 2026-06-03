using System;
using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Assertions;

namespace MetBench_BLL.SystemMT.Catalog;

public sealed class MrBindingDefinition
{
    public string MrId { get; set; } = string.Empty;
    public string SutName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MrFamily { get; set; } = string.Empty;

    /// <summary>UI-facing single-string display of the transformation (e.g. "ScaleNuSigmaF"); distinct from per-step engine names.</summary>
    public string TransformationName { get; set; } = string.Empty;
    public string AssertionTypeCode { get; set; } = string.Empty;
    public string AssertionName { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultParameters { get; set; } = new();
    public List<MrTransformStepDefinition> TransformSteps { get; set; } = new();

    public double ToleranceRel { get; set; }
    public double ToleranceAbs { get; set; }
    public bool NoiseAware { get; set; }
    public double NoiseMultiplier { get; set; } = 3.0;

    public string EquationKey { get; set; } = string.Empty;
    public string Equation { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string MetaPattern { get; set; } = string.Empty;
    public string SourceLevel { get; set; } = string.Empty;
    public string FailureCorrelation { get; set; } = string.Empty;
    public MrExplanationProfileDefinition? ExplanationProfile { get; set; } = new();

    /// <summary>Per-binding override of the program-level input adapter; openmoc/openmc have per-MR variants.</summary>
    public string InputAdapterScriptRelativePath { get; set; } = string.Empty;
    /// <summary>Per-binding override of the program-level output adapter; default empty = use program-level.</summary>
    public string OutputAdapterScriptRelativePath { get; set; } = string.Empty;

    public string SampleCaseRelativePath { get; set; } = string.Empty;
    /// <summary>Per-binding temp-dir prefix (e.g. "MetBenchOpenMocSigmaA"); required for run isolation and diagnostic grep.</summary>
    public string WorkRootName { get; set; } = string.Empty;
    /// <summary>Process timeout in seconds; no default — authors must choose per SUT runtime (openmoc≈120, openmc≈300, ODE≈30-60).</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// PR-Bol-2A: optional ordered refinement-phase list consumed by the multi-phase
    /// pipeline. Required when <see cref="AssertionTypeCode"/> is <c>"error-monotonic"</c>;
    /// rejected with any other code. Must contain ≥ 2 phases. Phase order is
    /// significant — the last phase becomes the typed predicate's <c>ReferenceRole</c>;
    /// earlier phases become <c>OrderedRoles</c> in declared order.
    /// </summary>
    public List<RefinementPhaseDefinition>? RefinementPhases { get; set; }

    /// <summary>Assertion codes whose semantics REQUIRE a positive tolerance (Approx-class family).</summary>
    private static readonly HashSet<string> ToleranceRequiredCodes = new(StringComparer.Ordinal)
    {
        AssertionTypeCodes.Approx,
        AssertionTypeCodes.ApproxInvariant,
        AssertionTypeCodes.FluxPointwiseApprox,
        AssertionTypeCodes.VarianceRatio,
        AssertionTypeCodes.LessNoiseAware,
        AssertionTypeCodes.GreaterNoiseAware,
        AssertionTypeCodes.CrossProgramAgree,
    };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MrId))
            throw new CatalogValidationException("MrBindingDefinition.MrId is required");
        if (string.IsNullOrWhiteSpace(SutName))
            throw new CatalogValidationException($"MrBindingDefinition '{MrId}' SutName is required");
        if (string.IsNullOrWhiteSpace(TransformationName))
            throw new CatalogValidationException($"MrBindingDefinition '{MrId}' TransformationName is required");
        if (string.IsNullOrWhiteSpace(AssertionTypeCode))
            throw new CatalogValidationException($"MrBindingDefinition '{MrId}' AssertionTypeCode is required");
        if (!AssertionTypeCodes.All.Contains(AssertionTypeCode))
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' AssertionTypeCode '{AssertionTypeCode}' is not a recognized code; expected one of: {string.Join(", ", AssertionTypeCodes.All)}");
        if (string.IsNullOrWhiteSpace(WorkRootName))
            throw new CatalogValidationException($"MrBindingDefinition '{MrId}' WorkRootName is required (per-MR temp-dir prefix)");
        if (TimeoutSeconds <= 0)
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' TimeoutSeconds must be > 0 (authors must choose per SUT runtime)");

        // Null-guard mutable collections (public setters allow null assignment).
        if (TransformSteps is null)
            throw new CatalogValidationException($"MrBindingDefinition '{MrId}' TransformSteps must not be null");
        if (DefaultParameters is null)
            throw new CatalogValidationException($"MrBindingDefinition '{MrId}' DefaultParameters must not be null");

        if (TransformSteps.Count == 0)
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' must declare at least one transform step");

        // Recipe-based single-step MRs (EquationKey set, exactly one step, empty path) defer
        // path resolution to the EquationFunctionRegistry recipe (e.g. decay-chain-scale-initial
        // with TransformationName="ScaleInitial" + EquationKey="bateman").
        var isSingleStepRecipe =
            TransformSteps.Count == 1
            && !string.IsNullOrWhiteSpace(EquationKey)
            && string.IsNullOrWhiteSpace(TransformSteps[0]?.TargetFieldPath);

        for (var i = 0; i < TransformSteps.Count; i++)
        {
            if (TransformSteps[i] is null)
                throw new CatalogValidationException(
                    $"MrBindingDefinition '{MrId}' TransformSteps[{i}] must not be null");
            TransformSteps[i].Validate(MrId, i, allowEmptyPath: isSingleStepRecipe);
        }

        // Tolerance invariants (carry PR #88 review-fix-1 lesson):
        // 1) NaN / Infinity / negative are categorically invalid for ALL bindings.
        ValidateToleranceField(ToleranceRel, nameof(ToleranceRel));
        ValidateToleranceField(ToleranceAbs, nameof(ToleranceAbs));
        if (NoiseAware && !(double.IsFinite(NoiseMultiplier) && NoiseMultiplier > 0))
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' NoiseAware=true requires NoiseMultiplier > 0 and finite; got {NoiseMultiplier}");

        // 2) Approx-class assertions (including invariant / flux-pointwise / variance-ratio /
        //    noise-aware / cross-program) require an effective non-zero tolerance source:
        //    either static (ToleranceRel/Abs > 0) or noise-derived (NoiseAware=true && NoiseMultiplier > 0).
        if (ToleranceRequiredCodes.Contains(AssertionTypeCode))
        {
            var hasStatic = ToleranceRel > 0 || ToleranceAbs > 0;
            var hasNoise = NoiseAware && NoiseMultiplier > 0;
            if (!hasStatic && !hasNoise)
                throw new CatalogValidationException(
                    $"MrBindingDefinition '{MrId}' uses '{AssertionTypeCode}' (approx-class) but provides no tolerance: " +
                    "set ToleranceRel/ToleranceAbs > 0, or NoiseAware=true with NoiseMultiplier > 0.");
        }

        // 3) PR-Bol-2A: RefinementPhases is required for "error-monotonic" and rejected for any
        //    other code. Each phase must validate independently.
        var isErrorMonotonic = string.Equals(
            AssertionTypeCode, AssertionTypeCodes.ErrorMonotonic, StringComparison.Ordinal);
        if (isErrorMonotonic)
        {
            // ErrorMonotonicPredicateValidator requires OrderedRoles.Count ≥ 2; with the
            // "last phase = ReferenceRole" convention that means total refinement_phases ≥ 3.
            if (RefinementPhases is null || RefinementPhases.Count < 3)
                throw new CatalogValidationException(
                    $"MrBindingDefinition '{MrId}' uses 'error-monotonic' and requires refinement_phases with at least 3 entries " +
                    $"(≥ 2 ordered roles + 1 reference role; got {(RefinementPhases?.Count ?? 0)}).");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < RefinementPhases.Count; i++)
            {
                if (RefinementPhases[i] is null)
                    throw new CatalogValidationException(
                        $"MrBindingDefinition '{MrId}' refinement_phases[{i}] must not be null");
                RefinementPhases[i].Validate(MrId, i);
                if (!seen.Add(RefinementPhases[i].Role))
                    throw new CatalogValidationException(
                        $"MrBindingDefinition '{MrId}' refinement_phases roles must be unique; " +
                        $"'{RefinementPhases[i].Role}' appears more than once.");
            }
        }
        else if (RefinementPhases is { Count: > 0 })
        {
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' carries refinement_phases but AssertionTypeCode is '{AssertionTypeCode}'; " +
                "refinement_phases is only valid with 'error-monotonic'.");
        }
    }

    private void ValidateToleranceField(double value, string fieldName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' {fieldName} must be a finite non-negative number; got {value}");
    }
}
