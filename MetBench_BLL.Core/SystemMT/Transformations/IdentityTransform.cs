using MetBench_BLL.SystemMT.ParameterMapping;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// 身份变换 — 不改变数据。
/// </summary>
/// <remarks>
/// 用作 Mut00（identity）的语义对应：MR 是否正确实例化的"假阳性控制"。
/// 任何 SUT 上跑 IdentityTransform 应该让 MR 始终通过（assertion 不触发）；
/// 若反之，则 MR 自身或断言有 bug。
/// </remarks>
public sealed class IdentityTransform : IMRTransformation
{
    public string Name => "Identity";
    public string ParametersSchema => "{}";  // 无参

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        // 返回新字典副本，确保调用方拿到的是独立对象。
        return new Dictionary<string, object?>(sourceData);
    }
}
