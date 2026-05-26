namespace MetBench_BLL.SystemMT.Catalog.Editing;

public sealed record SystemMtManifestEditResult(bool Success, IReadOnlyList<string> Errors)
{
    public static SystemMtManifestEditResult Ok() => new(true, Array.Empty<string>());

    public static SystemMtManifestEditResult Fail(params string[] errors) => new(false, errors);
}
