namespace MetBench_BLL.SystemMT.Catalog.Editing;

/// <summary>
/// CRU surface for SUT-level catalog metadata (the <c>program</c> section of
/// <c>SUT/&lt;sut&gt;/catalog.json</c>). Delete is intentionally omitted: removing
/// a SUT orphans its MRs, sample cases, and execution history — that deletion
/// path runs through git revert, not the UI.
/// </summary>
public interface ISystemMtSutEditor
{
    IReadOnlyList<SystemMtSutDescriptor> ListSuts();

    SystemMtSutProgramDraft Load(string sutId);

    SystemMtManifestEditResult ValidateDraft(string sutId, SystemMtSutProgramDraft draft);

    /// <summary>
    /// Persist the program section. When <paramref name="sutId"/> already has a
    /// <c>catalog.json</c>, the existing <c>mrs</c> array is preserved verbatim.
    /// When the SUT directory does not exist, <see cref="SaveDraft"/> creates
    /// it and writes a fresh catalog with an empty <c>mrs</c> array (callers
    /// then author MR bindings through <see cref="ISystemMtManifestCatalogEditor"/>;
    /// the resulting empty catalog will not parse through the launcher until at
    /// least one binding is added — that is intentional fail-closed behaviour).
    /// </summary>
    SystemMtManifestEditResult SaveDraft(string sutId, SystemMtSutProgramDraft draft);
}
