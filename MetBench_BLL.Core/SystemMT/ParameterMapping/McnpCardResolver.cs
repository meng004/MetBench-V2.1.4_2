namespace MetBench_BLL.SystemMT.ParameterMapping;

/// <summary>
/// MCNP 卡式 ASCII 路径解析器（占位实现）。
/// </summary>
/// <remarks>
/// 路径形如 "card:m1::tmp" — card 名 m1 的 tmp 子字段。
/// MCNP runner 把卡式输入预先解析成嵌套 dict 后再交给本 resolver。
/// 数据契约（MCNP Input Parser 输出）：
/// <code>
/// {
///   "cards": {
///     "m1": { "name": "m1", "type": "material", "tmp": "600.0", "nuclides": [...] },
///     ...
///   },
///   "surfaces": { ... },
///   "cells": { ... }
/// }
/// </code>
/// 路径 "card:m1::tmp" 解析为 ["cards", "m1", "tmp"]。
///
/// 当前实现：MCNP SUT 未接入，留占位（throw NotImplementedException）以便
/// P3.5 测试覆盖到 Factory 分派逻辑。
/// </remarks>
public sealed class McnpCardResolver : IFieldPathResolver
{
    public string PathSyntax => "mcnp-card";

    public object? Get(IReadOnlyDictionary<string, object?> data, string path)
    {
        var jp = ToJsonPointer(path);
        return new JsonPointerResolver().Get(data, jp);
    }

    public bool Exists(IReadOnlyDictionary<string, object?> data, string path)
    {
        var jp = ToJsonPointer(path);
        return new JsonPointerResolver().Exists(data, jp);
    }

    public Dictionary<string, object?> Set(
        IReadOnlyDictionary<string, object?> data,
        string path,
        object? value)
    {
        var jp = ToJsonPointer(path);
        return new JsonPointerResolver().Set(data, jp, value);
    }

    /// <summary>
    /// "card:m1::tmp" → "/cards/m1/tmp"。
    /// 支持的语法：<c>card:&lt;name&gt;::&lt;field&gt;</c>
    /// </summary>
    public static string ToJsonPointer(string mcnpPath)
    {
        if (string.IsNullOrWhiteSpace(mcnpPath))
            throw new ArgumentException("MCNP path cannot be empty");

        if (!mcnpPath.StartsWith("card:", StringComparison.OrdinalIgnoreCase))
            throw new FormatException(
                $"MCNP path must start with 'card:'; got '{mcnpPath}'");

        var rest = mcnpPath.Substring(5);
        var parts = rest.Split("::", 2);
        if (parts.Length != 2)
            throw new FormatException(
                $"MCNP path must contain '::'; got '{mcnpPath}'");

        return $"/cards/{parts[0]}/{parts[1]}";
    }
}
