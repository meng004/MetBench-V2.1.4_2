namespace MetBench_BLL.Equations;

/// <summary>
/// TransformationResolver 的决策 B 查找未命中时抛出：
/// 既不在全局 TransformationRegistry 也不在方程命名空间 EquationFunctionRegistry 中。
/// </summary>
public sealed class UnknownTransformationException : Exception
{
    public string TransformationName { get; }
    public string EquationKey { get; }

    public UnknownTransformationException(string transformationName, string equationKey)
        : base($"Unknown transformation '{transformationName}' for equation '{equationKey}'. " +
               "Not found in global TransformationRegistry or equation-namespaced EquationFunctionRegistry.")
    {
        TransformationName = transformationName;
        EquationKey = equationKey;
    }
}
