namespace MetBench_BLL.SystemMT.Metadata;

/// <summary>
/// 方程函数库的目录条目 —— 声明本方程提供了哪个函数 + 它是 L1(数据驱动 Recipe)
/// 还是 L2(C# 实现) + 给 UI/报表的展示信息。嵌在 <see cref="EquationMetadata.Functions"/>。
/// </summary>
/// <remarks>
/// 设计见 docs/design/mr-architecture.md §4.2 与 §5。
/// 二选一:
///   • <see cref="ImplKey"/> 非空 → L2 路径,引用 C# <c>IEquationFunction</c> 注册名(P2 落地)
///   • <see cref="ImplKey"/> 空    → L1 路径,Recipe 存 <see cref="EquationFunctionRecipe"/> 表
/// </remarks>
public sealed class EquationFunctionDescriptor
{
    /// <summary>方程命名空间内唯一,如 "ScaleInitial"。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>用途分类:<c>transformation</c> / <c>solution</c> / <c>invariant</c>。</summary>
    public string Kind { get; set; } = "transformation";

    /// <summary>人类可读签名(输入/输出形态)。</summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>给 UI / 报表用的 LaTeX 字面公式。**仅展示,不驱动执行**。</summary>
    public string DisplayLatex { get; set; } = string.Empty;

    /// <summary>
    /// L2 实现注册名(如 <c>bateman.AnalyticSolution</c>),C# <c>IEquationFunction</c> 在
    /// <c>EquationFunctionRegistry</c> 启动时按此注册。
    /// 留空表示 L1 路径(Recipe 在 <see cref="EquationFunctionRecipe"/> 表中,
    /// 按 <c>(EquationKey, FunctionName)</c> 查找)。
    /// </summary>
    public string ImplKey { get; set; } = string.Empty;
}
