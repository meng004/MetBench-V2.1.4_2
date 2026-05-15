using MetBench_BLL.SystemMT.ParameterMapping;
using System.Globalization;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// 标量加法变换：target field + offset。
/// </summary>
/// <remarks>
/// 参数：<c>{"offset": "0.5"}</c>。
///
/// 用于：
/// <list type="bullet">
///   <item>几何坐标平移 (MR02 mirror-x / MR03 mirror-y 的 offset 修正)</item>
///   <item>温度加偏移测试线性响应</item>
/// </list>
/// </remarks>
public sealed class TranslateField : IMRTransformation
{
    private readonly IFieldPathResolver _resolver;

    public TranslateField() : this(new JsonPointerResolver()) { }

    public TranslateField(IFieldPathResolver resolver)
    {
        _resolver = resolver;
    }

    public string Name => "TranslateField";

    public string ParametersSchema => """
        {
          "type": "object",
          "required": ["offset"],
          "properties": { "offset": { "type": "string" } }
        }
        """;

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("offset", out var offsetStr))
            throw new ArgumentException("TranslateField requires 'offset' parameter");
        if (!double.TryParse(offsetStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var offset))
            throw new ArgumentException($"TranslateField 'offset' must be a number; got '{offsetStr}'");

        var oldValue = _resolver.Get(sourceData, targetFieldPath);
        var newValue = TranslateAny(oldValue, offset, targetFieldPath);
        return _resolver.Set(sourceData, targetFieldPath, newValue);
    }

    private static object? TranslateAny(object? value, double offset, string path)
    {
        return value switch
        {
            null => throw new InvalidOperationException($"Cannot translate null at '{path}'"),
            double d   => d + offset,
            float f    => f + offset,
            int i      => i + offset,
            long l     => l + offset,
            decimal m  => (double)m + offset,
            System.Collections.IList list => TranslateList(list, offset, path),
            _ => throw new InvalidOperationException(
                $"TranslateField does not support type {value.GetType().Name} at '{path}'")
        };
    }

    private static List<object?> TranslateList(System.Collections.IList list, double offset, string path)
    {
        var result = new List<object?>(list.Count);
        foreach (var item in list)
        {
            result.Add(TranslateAny(item, offset, path));
        }
        return result;
    }
}
