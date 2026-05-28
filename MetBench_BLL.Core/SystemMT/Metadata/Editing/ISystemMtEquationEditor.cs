using MetBench_BLL.SystemMT.Catalog.Editing;

namespace MetBench_BLL.SystemMT.Metadata.Editing;

/// <summary>
/// CRUD surface for user-authored equations layered on top of the static
/// <see cref="SystemMtMetadataCatalog.Equations"/> seed. The seed list is
/// read-only; the editor exposes Create / Update / Delete only for rows
/// persisted in <see cref="ISystemMtMetadataRepository"/>. <see cref="ListEquations"/>
/// returns both lists merged, with a <c>Source</c> column so the UI can gate
/// controls.
/// </summary>
public interface ISystemMtEquationEditor
{
    /// <summary>
    /// Built-in seed equations (always present, immutable) merged with
    /// user-defined equations from the LiteDB repository. Sorted by
    /// <see cref="EquationListItem.EquationKey"/> ascending.
    /// </summary>
    Task<IReadOnlyList<EquationListItem>> ListEquationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the user-authored equation with this key, or <c>null</c> if the
    /// key matches a Built-in row (which the editor refuses to surface as an
    /// editable draft).
    /// </summary>
    Task<EquationMetadataDraft?> LoadAsync(string equationKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a draft without persisting. Checks non-blank <c>EquationKey</c>
    /// / <c>Name</c> and rejects keys that collide with any Built-in row using
    /// <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    SystemMtManifestEditResult ValidateDraft(EquationMetadataDraft draft);

    /// <summary>
    /// Persist the draft via <see cref="ISystemMtMetadataRepository.UpsertEquationAsync"/>.
    /// Returns <see cref="SystemMtManifestEditResult.Fail"/> when validation fails.
    /// </summary>
    Task<SystemMtManifestEditResult> SaveDraftAsync(EquationMetadataDraft draft, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user-authored equation. Fail-closed when:
    /// - the key matches a Built-in row (seed equations are immutable);
    /// - any <see cref="MrMetadata"/> still references the key.
    /// Returns <see cref="SystemMtManifestEditResult.Ok"/> when actually deleted,
    /// <see cref="SystemMtManifestEditResult.Fail"/> otherwise.
    /// </summary>
    Task<SystemMtManifestEditResult> DeleteAsync(string equationKey, CancellationToken cancellationToken = default);
}
