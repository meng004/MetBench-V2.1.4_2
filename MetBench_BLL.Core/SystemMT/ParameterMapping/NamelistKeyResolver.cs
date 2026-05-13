namespace MetBench_BLL.SystemMT.ParameterMapping;

/// <summary>
/// Fortran NAMELIST 路径解析器。
/// </summary>
/// <remarks>
/// 路径形如 "&amp;material/T" — namelist 块 "material" 的 T 字段。
/// Fortran SUT runner 把 NAMELIST 预解析成嵌套 dict 后再交给本 resolver。
/// 数据契约：
/// <code>
/// {
///   "namelists": {
///     "material": { "T": "600.0", "rho": "10.4", "U235": "0.04" },
///     "geometry": { ... }
///   }
/// }
/// </code>
/// 路径 "&amp;material/T" 解析为 ["namelists", "material", "T"]。
/// </remarks>
public sealed class NamelistKeyResolver : IFieldPathResolver
{
    public string PathSyntax => "namelist-key";

    public object? Get(IReadOnlyDictionary<string, object?> data, string path)
        => new JsonPointerResolver().Get(data, ToJsonPointer(path));

    public bool Exists(IReadOnlyDictionary<string, object?> data, string path)
        => new JsonPointerResolver().Exists(data, ToJsonPointer(path));

    public Dictionary<string, object?> Set(
        IReadOnlyDictionary<string, object?> data,
        string path,
        object? value)
        => new JsonPointerResolver().Set(data, ToJsonPointer(path), value);

    /// <summary>"&amp;material/T" → "/namelists/material/T"。</summary>
    public static string ToJsonPointer(string namelistPath)
    {
        if (string.IsNullOrWhiteSpace(namelistPath))
            throw new ArgumentException("NAMELIST path cannot be empty");

        if (!namelistPath.StartsWith("&", StringComparison.Ordinal))
            throw new FormatException(
                $"NAMELIST path must start with '&'; got '{namelistPath}'");

        var rest = namelistPath.Substring(1);
        var parts = rest.Split('/', 2);
        if (parts.Length != 2)
            throw new FormatException(
                $"NAMELIST path must be '&<block>/<field>'; got '{namelistPath}'");

        return $"/namelists/{parts[0]}/{parts[1]}";
    }
}
