using MetBench_BLL.SystemMT.ParameterMapping;
using System.Globalization;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// 标量乘法变换：target field × factor。
/// </summary>
/// <remarks>
/// 参数：<c>{"factor": "1.5"}</c>（字符串以保持与 MRInstance.ParameterOverrides 风格一致；
/// 转 double 在 Apply 内部完成）。
///
/// 支持的字段类型：
/// <list type="bullet">
///   <item><c>double</c> / <c>int</c> / <c>long</c> / <c>float</c> 等数值标量 → 乘 factor</item>
///   <item>数值数组（<c>IList&lt;double&gt;</c> / <c>IList&lt;object&gt;</c> 含数字） → 逐元素乘 factor</item>
/// </list>
///
/// 用于 NOETHER m_mono 类 MR：ScaleNuSigmaF / ScaleFuelSigmaA / ScaleFuelSigmaT /
/// ScaleFuelSigmaS / ScaleModeratorSigmaA / ScaleFuelRadius / RaiseFuelTemperature 等。
/// </remarks>
public sealed class ScaleField : IMRTransformation
{
    private readonly IFieldPathResolver _resolver;

    public ScaleField() : this(new JsonPointerResolver()) { }

    public ScaleField(IFieldPathResolver resolver)
    {
        _resolver = resolver;
    }

    public string Name => "ScaleField";

    public string ParametersSchema => """
        {
          "type": "object",
          "required": ["factor"],
          "properties": { "factor": { "type": "string", "pattern": "^[+]?[0-9]*\\.?[0-9]+([eE][-+]?[0-9]+)?$" } }
        }
        """;

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("factor", out var factorStr))
            throw new ArgumentException("ScaleField requires 'factor' parameter");
        if (!double.TryParse(factorStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            throw new ArgumentException($"ScaleField 'factor' must be a number; got '{factorStr}'");

        var oldValue = _resolver.Get(sourceData, targetFieldPath);
        var newValue = ScaleAny(oldValue, factor, targetFieldPath);
        return _resolver.Set(sourceData, targetFieldPath, newValue);
    }

    private static object? ScaleAny(object? value, double factor, string path)
    {
        return value switch
        {
            null => throw new InvalidOperationException($"Cannot scale null at '{path}'"),
            double d   => d * factor,
            float f    => f * factor,
            int i      => i * factor,
            long l     => l * factor,
            decimal m  => (double)m * factor,
            System.Collections.IList list => ScaleList(list, factor, path),
            _ => throw new InvalidOperationException(
                $"ScaleField does not support type {value.GetType().Name} at '{path}'")
        };
    }

    private static List<object?> ScaleList(System.Collections.IList list, double factor, string path)
    {
        var result = new List<object?>(list.Count);
        foreach (var item in list)
        {
            result.Add(ScaleAny(item, factor, path));
        }
        return result;
    }
}
