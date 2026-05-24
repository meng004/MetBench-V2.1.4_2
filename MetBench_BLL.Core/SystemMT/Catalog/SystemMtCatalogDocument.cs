using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog;

public sealed class SystemMtCatalogDocument
{
    public string SutName { get; set; } = string.Empty;
    public ProgramDefinition? Program { get; set; }
    public List<MrBindingDefinition> Mrs { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SutName))
            throw new CatalogValidationException("SystemMtCatalogDocument.SutName is required");
        Program?.Validate();

        // Null-guard mutable collection (public setter allows null assignment).
        if (Mrs is null)
            throw new CatalogValidationException(
                $"SystemMtCatalogDocument '{SutName}' Mrs must not be null");

        // Empty catalog == whole SUT silently absent from launcher (CLAUDE.md §6 no-silent-skips).
        if (Mrs.Count == 0)
            throw new CatalogValidationException(
                $"SystemMtCatalogDocument '{SutName}' must declare at least one MR binding");

        var ids = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var mr in Mrs)
        {
            if (mr is null)
                throw new CatalogValidationException(
                    $"SystemMtCatalogDocument '{SutName}' contains a null MR binding");
            mr.Validate();
            if (!string.Equals(mr.SutName, SutName, System.StringComparison.Ordinal))
                throw new CatalogValidationException(
                    $"MR '{mr.MrId}' SutName '{mr.SutName}' does not match document SutName '{SutName}'");
            if (!ids.Add(mr.MrId))
                throw new CatalogValidationException(
                    $"SystemMtCatalogDocument has duplicate MR id: '{mr.MrId}'");
        }
    }
}
