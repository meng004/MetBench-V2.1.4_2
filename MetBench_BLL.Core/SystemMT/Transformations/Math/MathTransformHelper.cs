using MetBench_BLL.SystemMT.ParameterMapping;
using System.Globalization;

namespace MetBench_BLL.SystemMT.Transformations.Math;

/// <summary>
/// 内部共享工具：类型转换 + 参数解析 + 字段读写，供 L0 数学基元算子复用。
/// </summary>
internal static class MathTransformHelper
{
    internal static readonly JsonPointerResolver Resolver = new();

    internal static double GetDouble(IReadOnlyDictionary<string, object?> data, string path)
    {
        var v = Resolver.Get(data, path);
        return v switch
        {
            double d  => d,
            float f   => (double)f,
            int i     => (double)i,
            long l    => (double)l,
            decimal m => (double)m,
            null      => throw new InvalidOperationException($"Field '{path}' is null; expected numeric"),
            _         => throw new InvalidOperationException(
                             $"Field '{path}' is {v.GetType().Name}; expected numeric"),
        };
    }

    internal static double ParseRequired(IReadOnlyDictionary<string, string> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var s))
            throw new ArgumentException($"Missing required parameter '{key}'");
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            throw new ArgumentException($"Parameter '{key}' must be a number; got '{s}'");
        return v;
    }

    internal static List<double> GetDoubleList(IReadOnlyDictionary<string, object?> data, string path)
    {
        var v = Resolver.Get(data, path);
        if (v is not System.Collections.IList list)
            throw new InvalidOperationException($"Field '{path}' is not a list");
        if (list.Count == 0)
            throw new InvalidOperationException($"Field '{path}' is empty");
        var result = new List<double>(list.Count);
        foreach (var item in list)
        {
            result.Add(item switch
            {
                double d  => d,
                float f   => (double)f,
                int i     => (double)i,
                long l    => (double)l,
                decimal m => (double)m,
                _         => throw new InvalidOperationException(
                                 $"Non-numeric element in list at '{path}': {item?.GetType().Name ?? "null"}"),
            });
        }
        return result;
    }
}
