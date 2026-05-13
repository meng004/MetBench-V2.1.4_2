using MetBench_BLL.SystemMT.ParameterMapping;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// 几何坐标镜像变换 — 把目标字段（标量）符号反转（×-1）。
/// </summary>
/// <remarks>
/// 用于 NOETHER MR02 MirrorX / MR03 MirrorY 几何对称变换：
/// 镜像 fuel_offset_x 或 fuel_offset_y，pin-cell 物理上对称，k_eff 应不变。
///
/// 参数：可选 <c>{"factor": "-1.0"}</c>；不指定则默认 -1.0。
/// </remarks>
public sealed class MirrorAxis : IMRTransformation
{
    private readonly IFieldPathResolver _resolver;

    public MirrorAxis() : this(new JsonPointerResolver()) { }

    public MirrorAxis(IFieldPathResolver resolver)
    {
        _resolver = resolver;
    }

    public string Name => "MirrorAxis";

    public string ParametersSchema => "{}";  // 无强制参数

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var oldValue = _resolver.Get(sourceData, targetFieldPath);
        object newValue = oldValue switch
        {
            null => throw new InvalidOperationException($"Cannot mirror null at '{targetFieldPath}'"),
            double d  => -d,
            float f   => -f,
            int i     => -i,
            long l    => -l,
            decimal m => -m,
            _ => throw new InvalidOperationException(
                $"MirrorAxis does not support type {oldValue.GetType().Name} at '{targetFieldPath}'")
        };
        return _resolver.Set(sourceData, targetFieldPath, newValue);
    }
}
