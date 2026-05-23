namespace MetBench_BLL.SystemMT.Transformations.Math;

/// <summary>Sum — 数组字段求和，标量结果写回同路径。</summary>
public sealed class Sum : IMRTransformation
{
    public string Name => "Sum";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var list = MathTransformHelper.GetDoubleList(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath,
            list.Sum());
    }
}

/// <summary>Mean — 数组字段算术均值，标量结果写回同路径。</summary>
public sealed class Mean : IMRTransformation
{
    public string Name => "Mean";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var list = MathTransformHelper.GetDoubleList(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath,
            list.Average());
    }
}

/// <summary>Max — 数组字段最大值，标量结果写回同路径。</summary>
public sealed class Max : IMRTransformation
{
    public string Name => "Max";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var list = MathTransformHelper.GetDoubleList(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath,
            list.Max());
    }
}

/// <summary>Min — 数组字段最小值，标量结果写回同路径。</summary>
public sealed class Min : IMRTransformation
{
    public string Name => "Min";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var list = MathTransformHelper.GetDoubleList(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath,
            list.Min());
    }
}
