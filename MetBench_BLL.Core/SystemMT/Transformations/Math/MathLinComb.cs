using System.Globalization;

namespace MetBench_BLL.SystemMT.Transformations.Math;

/// <summary>
/// MathLinComb — 线性组合：result = Σ coeffs[i] × fields[i]，写入 targetFieldPath。
/// 参数：fields（逗号分隔字段路径）、coeffs（逗号分隔浮点数，与 fields 等长）。
/// 示例：fields="a,b" coeffs="1.5,2.0" → result = 1.5*a + 2.0*b。
/// </summary>
public sealed class MathLinComb : IMRTransformation
{
    public string Name => "MathLinComb";
    public string ParametersSchema =>
        """{"required":["fields","coeffs"],"properties":{"fields":{"type":"string"},"coeffs":{"type":"string"}}}""";

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("fields", out var fieldsStr))
            throw new ArgumentException("MathLinComb requires 'fields' parameter");
        if (!parameters.TryGetValue("coeffs", out var coeffsStr))
            throw new ArgumentException("MathLinComb requires 'coeffs' parameter");

        var fields = fieldsStr.Split(',', StringSplitOptions.TrimEntries);
        var coeffParts = coeffsStr.Split(',', StringSplitOptions.TrimEntries);

        if (fields.Length != coeffParts.Length)
            throw new ArgumentException(
                $"MathLinComb: 'fields' count ({fields.Length}) != 'coeffs' count ({coeffParts.Length})");

        var coeffs = new double[coeffParts.Length];
        for (var i = 0; i < coeffParts.Length; i++)
        {
            if (!double.TryParse(coeffParts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out coeffs[i]))
                throw new ArgumentException($"MathLinComb: invalid coeff '{coeffParts[i]}'");
        }

        var sum = 0.0;
        for (var i = 0; i < fields.Length; i++)
            sum += coeffs[i] * MathTransformHelper.GetDouble(sourceData, fields[i]);

        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, sum);
    }
}
