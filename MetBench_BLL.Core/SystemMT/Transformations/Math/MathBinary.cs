namespace MetBench_BLL.SystemMT.Transformations.Math;

/// <summary>MathAdd — x + value；参数：value。</summary>
public sealed class MathAdd : IMRTransformation
{
    public string Name => "MathAdd";
    public string ParametersSchema => """{"required":["value"],"properties":{"value":{"type":"string"}}}""";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        var v = MathTransformHelper.ParseRequired(parameters, "value");
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, x + v);
    }
}

/// <summary>MathSub — x - value；参数：value。</summary>
public sealed class MathSub : IMRTransformation
{
    public string Name => "MathSub";
    public string ParametersSchema => """{"required":["value"],"properties":{"value":{"type":"string"}}}""";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        var v = MathTransformHelper.ParseRequired(parameters, "value");
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, x - v);
    }
}

/// <summary>MathMul — x * value；参数：value。</summary>
public sealed class MathMul : IMRTransformation
{
    public string Name => "MathMul";
    public string ParametersSchema => """{"required":["value"],"properties":{"value":{"type":"string"}}}""";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        var v = MathTransformHelper.ParseRequired(parameters, "value");
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, x * v);
    }
}

/// <summary>MathDiv — x / value；value = 0 抛 InvalidOperationException；参数：value。</summary>
public sealed class MathDiv : IMRTransformation
{
    public string Name => "MathDiv";
    public string ParametersSchema => """{"required":["value"],"properties":{"value":{"type":"string"}}}""";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        var v = MathTransformHelper.ParseRequired(parameters, "value");
        if (v == 0.0)
            throw new InvalidOperationException("MathDiv: division by zero");
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, x / v);
    }
}
