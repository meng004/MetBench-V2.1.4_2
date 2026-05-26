namespace MetBench_BLL.SystemMT.Catalog.Editing;

public interface ISystemMtManifestCatalogEditor
{
    IReadOnlyList<SystemMtManifestDescriptor> ListManifests();

    SystemMtCatalogDocument Load(string sutId);

    SystemMtManifestEditResult ValidateDraft(string sutId, SystemMtMrBindingDraft draft);

    SystemMtManifestEditResult SaveDraft(string sutId, SystemMtMrBindingDraft draft);
}
