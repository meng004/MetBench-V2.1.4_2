namespace MetBench_BLL.Equations;

/// <summary>
/// 方程级函数抽象 — 比 IMRTransformation 更高层；负责「以参数为输入、对整个数据字典应用
/// 一系列操作」。路径信息由实现内部管理（L1 Recipe 的 compose 步骤已含 path）。
/// </summary>
/// <remarks>
/// 设计见 docs/design/mr-architecture.md §5.1(L1) 与 §5.2(L2 可实现本接口)。
/// </remarks>
public interface IEquationFunction
{
    /// <summary>所属方程的稳定业务键（对应 EquationMetadata.EquationKey）。</summary>
    string EquationKey { get; }

    /// <summary>方程命名空间内的函数名（对应 EquationFunctionDescriptor.Name）。</summary>
    string Name { get; }

    /// <summary>函数种类：<c>transformation</c> / <c>solution</c> / <c>oracle</c>。</summary>
    string Kind { get; }

    /// <summary>
    /// 执行函数：以 <paramref name="sourceData"/> 为输入，
    /// <paramref name="parameters"/> 为调用参数（字符串形式），返回变换后的新字典。
    /// </summary>
    Dictionary<string, object?> Execute(
        IReadOnlyDictionary<string, object?> sourceData,
        IReadOnlyDictionary<string, string> parameters);
}
