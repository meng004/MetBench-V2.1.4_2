namespace MetBench_BLL.SystemMT.ParameterMapping;

/// <summary>
/// 按 PathSyntax 字符串分派到具体 <see cref="IFieldPathResolver"/> 实现。
/// </summary>
public static class FieldPathResolverFactory
{
    /// <summary>取得指定 PathSyntax 对应的 resolver 实例。</summary>
    /// <exception cref="NotSupportedException">未知语法。</exception>
    public static IFieldPathResolver For(string pathSyntax) => pathSyntax switch
    {
        "json-pointer" => new JsonPointerResolver(),
        "mcnp-card"    => new McnpCardResolver(),
        "namelist-key" => new NamelistKeyResolver(),
        _ => throw new NotSupportedException(
            $"Unknown PathSyntax '{pathSyntax}'. " +
            $"Supported: json-pointer / mcnp-card / namelist-key.")
    };

    /// <summary>已注册的全部 PathSyntax 列表（UI 下拉框用）。</summary>
    public static IReadOnlyList<string> SupportedSyntaxes { get; } =
        new[] { "json-pointer", "mcnp-card", "namelist-key" };
}
