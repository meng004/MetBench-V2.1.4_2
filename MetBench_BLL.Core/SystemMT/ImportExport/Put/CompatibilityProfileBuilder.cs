namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public static class CompatibilityProfileBuilder
{
    public static CompatibilityProfile Build(SutImportUnit unit)
    {
        var findings = new List<string>();
        if (!SutImportValidator.Validate(unit).IsValid)
        {
            findings.Add("Import unit must pass validation before compatibility can be promoted.");
            return new CompatibilityProfile(RuntimeReadiness.ImportedOnly, findings);
        }

        foreach (var mr in unit.Mrs)
        {
            if (!IsRuntimeSupported(mr.TransformBinding.Status))
            {
                findings.Add($"MR '{mr.MrId}' has no explicit runtime transform binding.");
            }

            if (!IsRuntimeSupported(mr.AssertionBinding.Status))
            {
                findings.Add($"MR '{mr.MrId}' has no explicit runtime assertion binding.");
            }

            if (unit.Sut.SutId == "P9" && IsRuntimeSupported(mr.TransformBinding.Status) && IsRuntimeSupported(mr.AssertionBinding.Status))
            {
                ValidateP9NoiseSemantics(mr, findings);
            }
        }

        return findings.Count == 0 && unit.Mrs.Count > 0
            ? new CompatibilityProfile(RuntimeReadiness.RuntimeCandidate, findings)
            : new CompatibilityProfile(RuntimeReadiness.ImportedOnly, findings);
    }

    private static bool IsRuntimeSupported(CompatibilityStatus status)
    {
        return status is CompatibilityStatus.NativeSupported or CompatibilityStatus.MappedSupported;
    }

    private static void ValidateP9NoiseSemantics(MrAsset mr, ICollection<string> findings)
    {
        var assertion = mr.AssertionBinding;
        var mentionsSigma = assertion.Notes.Contains("sigma_k", StringComparison.OrdinalIgnoreCase)
            || assertion.PredicateKind.Contains("sigma_k", StringComparison.OrdinalIgnoreCase);
        var noiseAware = assertion.PredicateKind.Contains("Noise", StringComparison.OrdinalIgnoreCase)
            || assertion.PredicateKind.Contains("Statistical", StringComparison.OrdinalIgnoreCase);

        if (!mentionsSigma || !noiseAware)
        {
            findings.Add($"MR '{mr.MrId}' requires explicit sigma_k and noise-aware/statistical assertion semantics before runtime promotion.");
        }
    }
}
