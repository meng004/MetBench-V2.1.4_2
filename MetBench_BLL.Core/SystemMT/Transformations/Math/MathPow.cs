using SysMath = System.Math;

namespace MetBench_BLL.SystemMT.Transformations.Math;

/// <summary>MathPow — x^exponent；参数：exponent (string→double)。</summary>
public sealed class MathPow : IMRTransformation
{
    public string Name => "MathPow";
    public string ParametersSchema => """{"required":["exponent"],"properties":{"exponent":{"type":"string"}}}""";

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        var e = MathTransformHelper.ParseRequired(parameters, "exponent");
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, SysMath.Pow(x, e));
    }
}
