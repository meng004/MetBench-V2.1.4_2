using MetBench_BLL.SystemMT.Transformations;

namespace MetBench_BLL.Equations;

/// <summary>
/// 决策 B 分级查找：MR.TransformationName → IMRTransformation。
/// <list type="number">
///   <item>先查全局 <see cref="TransformationRegistry"/>（按 Name）。</item>
///   <item>若未命中，查 <see cref="EquationFunctionRegistry"/>[(equationKey, Name)]，
///         结果包装为 IMRTransformation。</item>
///   <item>两者均未命中 → 抛 <see cref="UnknownTransformationException"/>。</item>
/// </list>
/// 全局优先：同名条目以全局注册为准（方程命名空间不得夺走通用算子名）。
/// </summary>
public sealed class TransformationResolver
{
    private readonly EquationFunctionRegistry _eqRegistry;

    public TransformationResolver(EquationFunctionRegistry eqRegistry)
    {
        _eqRegistry = eqRegistry ?? throw new ArgumentNullException(nameof(eqRegistry));
    }

    /// <summary>
    /// 按决策 B 解析 <paramref name="transformationName"/>。
    /// </summary>
    /// <exception cref="UnknownTransformationException">两级均未命中。</exception>
    public IMRTransformation Resolve(string transformationName, string equationKey)
    {
        // 1. 全局优先
        try
        {
            return TransformationRegistry.Get(transformationName);
        }
        catch (KeyNotFoundException) { /* 继续方程命名空间查找 */ }

        // 2. 方程命名空间（空 equationKey 跳过）
        if (!string.IsNullOrEmpty(equationKey) &&
            _eqRegistry.TryGet(equationKey, transformationName, out var fn) && fn is not null)
        {
            return new EquationFunctionAdapter(fn);
        }

        // 3. 两级均未命中
        throw new UnknownTransformationException(transformationName, equationKey);
    }

    // ── 内部适配器：将 IEquationFunction 包装为 IMRTransformation ─────────────
    // targetFieldPath 被忽略；recipe 内部已含路径信息。

    private sealed class EquationFunctionAdapter : IMRTransformation
    {
        private readonly IEquationFunction _fn;
        public EquationFunctionAdapter(IEquationFunction fn) => _fn = fn;

        public string Name => _fn.Name;
        public string ParametersSchema => "{}";

        public Dictionary<string, object?> Apply(
            IReadOnlyDictionary<string, object?> sourceData,
            string targetFieldPath,
            IReadOnlyDictionary<string, string> parameters)
            => _fn.Execute(sourceData, parameters);
    }
}
