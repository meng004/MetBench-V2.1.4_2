using SysMath = System.Math;

namespace MetBench_BLL.SystemMT.Transformations.Math;

/// <summary>MathExp — e^x，无参数。</summary>
public sealed class MathExp : IMRTransformation
{
    public string Name => "MathExp";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, SysMath.Exp(x));
    }
}

/// <summary>MathLog — 自然对数 ln(x)；x ≤ 0 抛异常。</summary>
public sealed class MathLog : IMRTransformation
{
    public string Name => "MathLog";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        if (x <= 0)
            throw new InvalidOperationException($"MathLog requires positive value; got {x} at '{targetFieldPath}'");
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, SysMath.Log(x));
    }
}

/// <summary>MathSin — sin(x) 弧度，无参数。</summary>
public sealed class MathSin : IMRTransformation
{
    public string Name => "MathSin";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, SysMath.Sin(x));
    }
}

/// <summary>MathCos — cos(x) 弧度，无参数。</summary>
public sealed class MathCos : IMRTransformation
{
    public string Name => "MathCos";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, SysMath.Cos(x));
    }
}

/// <summary>MathSqrt — √x；x &lt; 0 抛异常。</summary>
public sealed class MathSqrt : IMRTransformation
{
    public string Name => "MathSqrt";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        if (x < 0)
            throw new InvalidOperationException($"MathSqrt requires non-negative value; got {x} at '{targetFieldPath}'");
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, SysMath.Sqrt(x));
    }
}

/// <summary>MathAbs — |x|，无参数。</summary>
public sealed class MathAbs : IMRTransformation
{
    public string Name => "MathAbs";
    public string ParametersSchema => "{}";
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        var x = MathTransformHelper.GetDouble(sourceData, targetFieldPath);
        return MathTransformHelper.Resolver.Set(sourceData, targetFieldPath, SysMath.Abs(x));
    }
}
