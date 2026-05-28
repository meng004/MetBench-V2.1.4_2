namespace MetBench_BLL.SystemMT.Catalog.Editing;

/// <summary>
/// CRUD surface for sample-case JSON files under
/// <c>SUT/&lt;sutId&gt;/sample/&lt;name&gt;.json</c>. Each sample file is the
/// source input for an MR run; the launcher reads it via
/// <c>MrBindingDefinition.SampleCaseRelativePath</c>.
/// </summary>
public interface ISystemMtSampleCaseEditor
{
    /// <summary>
    /// List sample-case files for one SUT, sorted ordinal by file name.
    /// Empty array when the <c>sample</c> directory does not exist.
    /// </summary>
    IReadOnlyList<SystemMtSampleCaseDescriptor> ListSamples(string sutId);

    /// <summary>Read the raw JSON body of one sample file as a string.</summary>
    string LoadSample(string sutId, string fileName);

    /// <summary>
    /// Validate filename and JSON body without writing. Filename must match
    /// <c>^[a-z0-9][a-z0-9_-]*\.json$</c>; body must parse via
    /// <see cref="System.Text.Json.JsonDocument.Parse(string)"/>.
    /// </summary>
    SystemMtManifestEditResult ValidateDraft(string sutId, string fileName, string jsonText);

    /// <summary>
    /// Atomically write the sample. Creates the SUT's <c>sample/</c> directory
    /// when absent. Uses tmp + <see cref="File.Replace(string, string, string?)"/>
    /// on update (matches <see cref="SystemMtManifestCatalogEditor"/> safety
    /// guarantees).
    /// </summary>
    SystemMtManifestEditResult SaveDraft(string sutId, string fileName, string jsonText);

    /// <summary>
    /// Delete a sample. Fail-closed when any MR binding in
    /// <c>SUT/&lt;sutId&gt;/catalog.json</c> references
    /// <c>sample/&lt;fileName&gt;</c> via its <c>sample_case_relative_path</c>.
    /// </summary>
    SystemMtManifestEditResult Delete(string sutId, string fileName);
}
