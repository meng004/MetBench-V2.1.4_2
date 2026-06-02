namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public static class SutImportValidator
{
    public static SutImportValidationResult Validate(SutImportUnit? unit)
    {
        var errors = new List<string>();
        if (unit is null)
        {
            return SutImportValidationResult.Invalid(new[] { "SUT import unit is required." });
        }

        if (!string.Equals(unit.SchemaVersion, SutImportUnit.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            errors.Add($"Unsupported schema version '{unit.SchemaVersion}'. Expected '{SutImportUnit.CurrentSchemaVersion}'.");
        }

        ValidateProvenance(unit.Provenance, errors);

        if (unit.Sut is null)
        {
            errors.Add("SUT asset is required.");
            return SutImportValidationResult.Invalid(errors);
        }

        if (string.IsNullOrWhiteSpace(unit.Sut.SutId))
        {
            errors.Add("SUT id is required.");
        }

        if (unit.Sut.SutId == "P9" && unit.Sut.ProgramKind != ProgramKind.Surrogate)
        {
            errors.Add("P9 must be classified as ProgramKind.Surrogate because the imported asset is a deterministic OpenMC surrogate.");
        }

        var observableNames = unit.Sut.Observables.Select(o => o.Name).ToHashSet(StringComparer.Ordinal);
        if (observableNames.Count != unit.Sut.Observables.Count)
        {
            errors.Add("Duplicate observable ids are not allowed.");
        }

        var mrIds = ValidateUnique(unit.Mrs, m => m.MrId, "MR", errors);
        var ioIds = ValidateUnique(unit.IoGroups, g => g.IoGroupId, "IO group", errors);
        var mutationIds = ValidateUnique(unit.Mutations, m => m.MutationId, "mutation", errors);

        foreach (var mr in unit.Mrs)
        {
            if (!string.Equals(mr.SutId, unit.Sut.SutId, StringComparison.Ordinal))
            {
                errors.Add($"MR '{mr.MrId}' violates single-SUT closure: '{mr.SutId}' does not match '{unit.Sut.SutId}'.");
            }

            foreach (var observable in mr.ObservableRefs)
            {
                if (!observableNames.Contains(observable))
                {
                    errors.Add($"MR '{mr.MrId}' references unknown observable '{observable}'.");
                }
            }
        }

        foreach (var io in unit.IoGroups)
        {
            if (!string.Equals(io.SutId, unit.Sut.SutId, StringComparison.Ordinal))
            {
                errors.Add($"IO group '{io.IoGroupId}' violates single-SUT closure: '{io.SutId}' does not match '{unit.Sut.SutId}'.");
            }
        }

        foreach (var mutation in unit.Mutations)
        {
            if (!string.Equals(mutation.SutId, unit.Sut.SutId, StringComparison.Ordinal))
            {
                errors.Add($"Mutation '{mutation.MutationId}' violates single-SUT closure: '{mutation.SutId}' does not match '{unit.Sut.SutId}'.");
            }
        }

        foreach (var detection in unit.Detections)
        {
            if (!mrIds.Contains(detection.MrId))
            {
                errors.Add($"Detection '{detection.DetectionId}' references unknown MR '{detection.MrId}'.");
            }

            if (!mutationIds.Contains(detection.MutationId))
            {
                errors.Add($"Detection '{detection.DetectionId}' references unknown mutation '{detection.MutationId}'.");
            }

            if (!ioIds.Contains(detection.IoGroupId))
            {
                errors.Add($"Detection '{detection.DetectionId}' references unknown IO group '{detection.IoGroupId}'.");
            }
        }

        return SutImportValidationResult.Invalid(errors);
    }

    private static void ValidateProvenance(Provenance provenance, ICollection<string> errors)
    {
        if (provenance is null
            || string.IsNullOrWhiteSpace(provenance.RepositoryUrl)
            || string.IsNullOrWhiteSpace(provenance.Commit)
            || provenance.SourcePaths.Count == 0
            || provenance.SourcePaths.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("Complete provenance is required: repository URL, commit, and source paths.");
        }
    }

    private static HashSet<string> ValidateUnique<T>(
        IReadOnlyList<T> values,
        Func<T, string> idSelector,
        string label,
        ICollection<string> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var id = idSelector(value);
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"{label} id is required.");
                continue;
            }

            if (!ids.Add(id))
            {
                errors.Add($"Duplicate {label} id '{id}'.");
            }
        }

        return ids;
    }
}
