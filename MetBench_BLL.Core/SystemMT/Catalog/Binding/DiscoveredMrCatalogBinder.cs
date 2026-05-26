using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetBench_BLL.SystemMT.Assertions;

namespace MetBench_BLL.SystemMT.Catalog.Binding;

/// <summary>
/// Pure, deterministic, IO-free implementation of <see cref="IDiscoveredMrCatalogBinder"/>.
/// Validates the draft against the production catalog schema rules and emits a
/// manifest-compatible <see cref="MrBindingDefinition"/> + <see cref="DiscoveredMrBindingProvenance"/>
/// when accepted, or a list of field-level errors when rejected.
///
/// The binder is the only sanctioned bridge from T4 discovery to T0 catalog state.
/// It must not read or write any file under <c>SUT/&lt;sut&gt;/</c> — the caller is
/// responsible for embedding the returned <see cref="MrBindingDefinition"/> into a
/// future <see cref="SystemMtCatalogDocument"/> under their own audit policy.
/// </summary>
public sealed class DiscoveredMrCatalogBinder : IDiscoveredMrCatalogBinder
{
    /// <summary>
    /// Assertion codes the binder rejects up-front because the Typed Semantic Catalog
    /// scalar predicates do not yet carry <c>SourceStd</c> / <c>FollowupStd</c> /
    /// <c>NoiseMultiplier</c>. Mirrors the documented fail-closed list in
    /// <c>LegacyAssertionPredicateMapper</c> (the binder cannot map these to a typed
    /// predicate via <see cref="LegacyAssertionPredicateMapper.MapScalar"/> until a
    /// noise-aware typed predicate ships).
    /// </summary>
    private static readonly HashSet<string> NoiseAwareFailClosedCodes = new(StringComparer.Ordinal)
    {
        AssertionTypeCodes.LessNoiseAware,
        AssertionTypeCodes.GreaterNoiseAware,
    };

    public DiscoveredMrBindingResult Bind(DiscoveredMrBindingDraft draft)
    {
        if (draft is null)
            throw new ArgumentNullException(nameof(draft));

        var errors = new List<DiscoveredMrBindingError>();

        RequireNonBlank(errors, draft.SutId, nameof(DiscoveredMrBindingDraft.SutId));
        RequireNonBlank(errors, draft.MrId, nameof(DiscoveredMrBindingDraft.MrId));
        RequireNonBlank(errors, draft.TransformationName, nameof(DiscoveredMrBindingDraft.TransformationName));
        RequireNonBlank(errors, draft.ValueName, nameof(DiscoveredMrBindingDraft.ValueName));
        RequireNonBlank(errors, draft.DiscoveryMethod, nameof(DiscoveredMrBindingDraft.DiscoveryMethod));
        RequireNonBlank(errors, draft.DiscoveryRunId, nameof(DiscoveredMrBindingDraft.DiscoveryRunId));
        RequireNonBlank(errors, draft.WorkRootName, nameof(DiscoveredMrBindingDraft.WorkRootName));

        ValidateAssertionTypeCode(errors, draft.AssertionTypeCode);
        ValidateConfidence(errors, draft.Confidence);

        if (draft.TimeoutSeconds <= 0)
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.TimeoutSeconds),
                $"TimeoutSeconds must be > 0; got {draft.TimeoutSeconds}."));

        ValidateDefaultParameters(errors, draft.DefaultParameters);
        ValidateSampleCaseRelativePath(errors, draft.SampleCaseRelativePath);
        ValidateTransformSteps(errors, draft.TransformSteps);

        if (errors.Count > 0)
            return new DiscoveredMrBindingResult(Binding: null, Provenance: null, Errors: errors);

        // Build the manifest-compatible binding. Approx-class codes that do not carry a
        // tolerance source (draft has no Tolerance fields) will be rejected by the
        // schema's tolerance invariant — surface that as a binder error rather than an
        // exception so the caller still sees a structured list.
        var binding = new MrBindingDefinition
        {
            MrId = draft.MrId,
            SutName = draft.SutId,
            DisplayName = draft.DisplayName,
            Description = string.Empty,
            MrFamily = string.Empty,
            TransformationName = draft.TransformationName,
            AssertionTypeCode = draft.AssertionTypeCode,
            AssertionName = string.Empty,
            ValueName = draft.ValueName,
            DefaultParameters = new Dictionary<string, string>(draft.DefaultParameters),
            TransformSteps = draft.TransformSteps
                .Select(s => new MrTransformStepDefinition
                {
                    TransformationName = s.TransformationName,
                    TargetFieldPath = s.TargetFieldPath,
                })
                .ToList(),
            EquationKey = draft.EquationKey,
            Equation = string.Empty,
            ProgramType = string.Empty,
            MetaPattern = draft.MetaPattern,
            SourceLevel = "Discovered",
            FailureCorrelation = string.Empty,
            SampleCaseRelativePath = draft.SampleCaseRelativePath,
            WorkRootName = draft.WorkRootName,
            TimeoutSeconds = draft.TimeoutSeconds,
        };

        try
        {
            binding.Validate();
        }
        catch (CatalogValidationException ex)
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(MrBindingDefinition),
                $"Binder output does not pass MrBindingDefinition.Validate(): {ex.Message}"));
            return new DiscoveredMrBindingResult(Binding: null, Provenance: null, Errors: errors);
        }

        var provenance = new DiscoveredMrBindingProvenance(
            DiscoveryMethod: draft.DiscoveryMethod,
            DiscoveryRunId: draft.DiscoveryRunId,
            Confidence: draft.Confidence,
            SutId: draft.SutId,
            MrId: draft.MrId,
            BoundAtUtc: DateTimeOffset.UtcNow);

        return new DiscoveredMrBindingResult(
            Binding: binding,
            Provenance: provenance,
            Errors: Array.Empty<DiscoveredMrBindingError>());
    }

    private static void RequireNonBlank(List<DiscoveredMrBindingError> errors, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(new DiscoveredMrBindingError(field, $"{field} is required (non-empty, non-whitespace)."));
    }

    private static void ValidateAssertionTypeCode(List<DiscoveredMrBindingError> errors, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.AssertionTypeCode),
                "AssertionTypeCode is required (non-empty)."));
            return;
        }

        if (NoiseAwareFailClosedCodes.Contains(code))
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.AssertionTypeCode),
                $"AssertionTypeCode '{code}' is fail-closed in the current Typed Semantic Catalog runtime " +
                "because noise-aware scalar predicates do not yet carry SourceStd / FollowupStd / " +
                "NoiseMultiplier inputs. Defer this candidate until a noise-aware typed predicate ships."));
            return;
        }

        if (!AssertionTypeCodes.All.Contains(code))
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.AssertionTypeCode),
                $"AssertionTypeCode '{code}' is not a recognized code; expected one of: " +
                string.Join(", ", AssertionTypeCodes.All)));
        }
    }

    private static void ValidateConfidence(List<DiscoveredMrBindingError> errors, double confidence)
    {
        if (double.IsNaN(confidence) || double.IsInfinity(confidence))
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.Confidence),
                $"Confidence must be a finite number in [0, 1]; got {confidence}."));
            return;
        }
        if (confidence < 0.0 || confidence > 1.0)
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.Confidence),
                $"Confidence must be in [0, 1]; got {confidence}."));
        }
    }

    private static void ValidateDefaultParameters(
        List<DiscoveredMrBindingError> errors,
        IReadOnlyDictionary<string, string>? dict)
    {
        if (dict is null)
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.DefaultParameters),
                "DefaultParameters must not be null (use an empty dictionary if no parameters)."));
            return;
        }
        foreach (var key in dict.Keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add(new DiscoveredMrBindingError(
                    nameof(DiscoveredMrBindingDraft.DefaultParameters),
                    "DefaultParameters contains a blank key; every parameter must have a non-empty name."));
                return;
            }
        }
    }

    private static void ValidateSampleCaseRelativePath(List<DiscoveredMrBindingError> errors, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return; // sample is optional at draft time; MrBindingDefinition.Validate does not require it.

        if (Path.IsPathRooted(path))
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.SampleCaseRelativePath),
                $"SampleCaseRelativePath must be relative to the SUT directory; got rooted path '{path}'."));
            return;
        }

        // Reject any segment that resolves above the SUT root.
        var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s == ".."))
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.SampleCaseRelativePath),
                $"SampleCaseRelativePath must not traverse above the SUT directory; got '{path}'."));
        }
    }

    private static void ValidateTransformSteps(
        List<DiscoveredMrBindingError> errors,
        IReadOnlyList<DiscoveredMrBindingTransformStep>? steps)
    {
        if (steps is null || steps.Count == 0)
        {
            errors.Add(new DiscoveredMrBindingError(
                nameof(DiscoveredMrBindingDraft.TransformSteps),
                "TransformSteps must declare at least one step."));
            return;
        }

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step is null || string.IsNullOrWhiteSpace(step.TransformationName))
            {
                errors.Add(new DiscoveredMrBindingError(
                    nameof(DiscoveredMrBindingDraft.TransformSteps),
                    $"TransformSteps[{i}] requires a non-blank TransformationName."));
            }
        }
    }
}
