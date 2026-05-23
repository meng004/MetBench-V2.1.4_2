using System.Globalization;

namespace MetBench_BLL.SystemMT.Transformations.Math;

/// <summary>
/// Linspace — 生成等距序列 [start, …, stop]，count 个点，写入 targetFieldPath。
/// 参数：start / stop / count（均为字符串）。
/// count = 1 时返回 [start]；count ≥ 2 时两端点等距。
/// </summary>
public sealed class Linspace : IMRTransformation
{
    public string Name => "Linspace";
    public string ParametersSchema =>
        """{"required":["start","stop","count"],"properties":{"start":{"type":"string"},"stop":{"type":"string"},"count":{"type":"string"}}}""";

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var start = MathTransformHelper.ParseRequired(parameters, "start");
        var stop  = MathTransformHelper.ParseRequired(parameters, "stop");

        if (!parameters.TryGetValue("count", out var countStr))
            throw new ArgumentException("Linspace requires 'count' parameter");
        if (!int.TryParse(countStr, out var count) || count < 1)
            throw new ArgumentException($"Linspace 'count' must be a positive integer; got '{countStr}'");

        var result = new List<double>(count);
        if (count == 1)
        {
            result.Add(start);
        }
        else
        {
            var step = (stop - start) / (count - 1);
            for (var i = 0; i < count; i++)
                result.Add(start + i * step);
        }

        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, result);
    }
}
