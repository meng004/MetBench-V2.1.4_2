using MetBench_BLL.Equations;
using MetBench_BLL.SystemMT.Transformations;

namespace MetBench_BLL.MethodMT.Transformations;

/// <summary>
/// 方法级 MT 变换注册表 — 与系统级 TransformationResolver 同决策 B 查找顺序：
///   1. 全局 <see cref="TransformationRegistry"/>（按 Name）优先
///   2. 方程命名空间 <see cref="EquationFunctionRegistry"/>（按 equationKey+Name）
///   3. 两者未命中 → 抛 <see cref="UnknownTransformationException"/>
///
/// 设计见 docs/design/mr-architecture.md §6（method 侧执行栈）。
/// </summary>
public sealed class MethodTransformationRegistry
{
    private readonly TransformationResolver _resolver;

    public MethodTransformationRegistry(EquationFunctionRegistry equationRegistry)
    {
        _resolver = new TransformationResolver(
            equationRegistry ?? throw new ArgumentNullException(nameof(equationRegistry)));
    }

    /// <summary>按决策 B 解析变换名；未命中抛 <see cref="UnknownTransformationException"/>。</summary>
    public IMRTransformation Resolve(string transformationName, string equationKey)
        => _resolver.Resolve(transformationName, equationKey);
}
