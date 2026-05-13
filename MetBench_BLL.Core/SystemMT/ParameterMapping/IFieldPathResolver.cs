namespace MetBench_BLL.SystemMT.ParameterMapping;

/// <summary>
/// 字段路径解析器 — 把抽象路径字符串映射到内存 dict 中的具体字段读写操作。
/// </summary>
/// <remarks>
/// 每种 PathSyntax 一个实现：
/// <list type="bullet">
///   <item><c>JsonPointerResolver</c> — RFC 6901 风格："/materials/fuel/temperature_kelvin"</item>
///   <item><c>McnpCardResolver</c> — MCNP 卡式 ASCII："card:m1::tmp"</item>
///   <item><c>NamelistKeyResolver</c> — Fortran NAMELIST："&amp;material/T"</item>
/// </list>
///
/// Resolver 处理路径语义；IMRTransformation 通过 Resolver 操作字段。
/// </remarks>
public interface IFieldPathResolver
{
    /// <summary>路径语法标识符（与 ParameterMapping.PathSyntax 字段对应）。</summary>
    string PathSyntax { get; }

    /// <summary>
    /// 按路径取值。若路径不存在抛 <see cref="FieldPathNotFoundException"/>。
    /// </summary>
    object? Get(IReadOnlyDictionary<string, object?> data, string path);

    /// <summary>
    /// 按路径写值（返回新字典，不 mutate 原字典）。
    /// </summary>
    Dictionary<string, object?> Set(
        IReadOnlyDictionary<string, object?> data,
        string path,
        object? value);

    /// <summary>检查路径是否存在。</summary>
    bool Exists(IReadOnlyDictionary<string, object?> data, string path);
}

/// <summary>路径解析失败异常。</summary>
public sealed class FieldPathNotFoundException : Exception
{
    public FieldPathNotFoundException(string path)
        : base($"Field path not found: '{path}'") { }
}
