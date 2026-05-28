using MetBench_BLL.SystemMT.Catalog.Editing;

namespace MetBench_BLL.SystemMT.Metadata.Editing;

public sealed class SystemMtEquationEditor : ISystemMtEquationEditor
{
    private readonly ISystemMtMetadataRepository _repository;
    private readonly IReadOnlyDictionary<string, EquationMetadata> _builtInByKey;

    public SystemMtEquationEditor(ISystemMtMetadataRepository repository)
        : this(repository, SystemMtMetadataCatalog.Equations)
    {
    }

    /// <summary>Constructor for tests that need to inject a known seed list.</summary>
    public SystemMtEquationEditor(
        ISystemMtMetadataRepository repository,
        IReadOnlyList<EquationMetadata> builtInSeed)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        ArgumentNullException.ThrowIfNull(builtInSeed);
        _builtInByKey = builtInSeed.ToDictionary(
            e => e.EquationKey,
            e => e,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<EquationListItem>> ListEquationsAsync(CancellationToken cancellationToken = default)
    {
        var items = _builtInByKey.Values.Select(e => new EquationListItem(
            e.EquationKey, e.Name, e.CanonicalForm, EquationSourceKinds.BuiltIn)).ToList();

        var userRows = await _repository.ListEquationsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var row in userRows)
        {
            // Skip rows whose key is shadowed by a Built-in seed; the seed wins.
            if (_builtInByKey.ContainsKey(row.EquationKey))
                continue;
            items.Add(new EquationListItem(
                row.EquationKey, row.Name, row.CanonicalForm, EquationSourceKinds.User));
        }

        return items
            .OrderBy(i => i.EquationKey, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<EquationMetadataDraft?> LoadAsync(string equationKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(equationKey))
            return null;
        if (_builtInByKey.ContainsKey(equationKey))
            return null;
        var row = await _repository.GetEquationAsync(equationKey, cancellationToken).ConfigureAwait(false);
        return row is null ? null : EquationMetadataDraft.FromMetadata(row);
    }

    public SystemMtManifestEditResult ValidateDraft(EquationMetadataDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(draft.EquationKey))
            errors.Add("EquationKey is required");
        if (string.IsNullOrWhiteSpace(draft.Name))
            errors.Add("Name is required");
        if (!string.IsNullOrWhiteSpace(draft.EquationKey)
            && _builtInByKey.ContainsKey(draft.EquationKey))
            errors.Add(
                $"EquationKey '{draft.EquationKey}' collides with a Built-in seed equation (case-insensitive); pick a different key");

        return errors.Count == 0
            ? SystemMtManifestEditResult.Ok()
            : SystemMtManifestEditResult.Fail(errors.ToArray());
    }

    public async Task<SystemMtManifestEditResult> SaveDraftAsync(EquationMetadataDraft draft, CancellationToken cancellationToken = default)
    {
        var validation = ValidateDraft(draft);
        if (!validation.Success)
            return validation;

        await _repository.UpsertEquationAsync(draft.ToMetadata(), cancellationToken).ConfigureAwait(false);
        return SystemMtManifestEditResult.Ok();
    }

    public async Task<SystemMtManifestEditResult> DeleteAsync(string equationKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(equationKey))
            return SystemMtManifestEditResult.Fail("EquationKey is required");

        if (_builtInByKey.ContainsKey(equationKey))
            return SystemMtManifestEditResult.Fail(
                $"EquationKey '{equationKey}' is a Built-in seed equation; seed rows are immutable");

        var referencing = await _repository.ListMrsByEquationAsync(equationKey, cancellationToken).ConfigureAwait(false);
        if (referencing.Count > 0)
            return SystemMtManifestEditResult.Fail(
                $"EquationKey '{equationKey}' is referenced by {referencing.Count} MR metadata row(s); remove or re-key those MRs first");

        var deleted = await _repository.DeleteEquationAsync(equationKey, cancellationToken).ConfigureAwait(false);
        return deleted
            ? SystemMtManifestEditResult.Ok()
            : SystemMtManifestEditResult.Fail($"EquationKey '{equationKey}' not found in repository");
    }
}
