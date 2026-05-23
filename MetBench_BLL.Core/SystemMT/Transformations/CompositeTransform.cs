namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// 组合变换 — 把多个 IMRTransformation 串行组合成一个新的变换。
/// </summary>
/// <remarks>
/// 用于：
/// <list type="bullet">
///   <item>Rotate90 = MirrorAxis(x) ∘ MirrorAxis(y) 两步组合</item>
///   <item>ScaleAndTranslate 在同字段先 scale 再 translate</item>
/// </list>
///
/// 构造时按顺序传入 (Transformation, TargetFieldPath, Parameters) 三元组。
/// 与单一 IMRTransformation 不同，本类的 Apply 用 ctor 注入的 path/params 而非
/// Apply 参数中的（因为多步可能针对不同字段）。
/// </remarks>
public sealed class CompositeTransform : IMRTransformation
{
    private readonly IReadOnlyList<Step> _steps;
    private readonly string _name;

    public CompositeTransform(string name, IReadOnlyList<Step> steps)
    {
        _name = name;
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        if (steps.Count == 0)
            throw new ArgumentException("CompositeTransform requires at least 1 step");
    }

    public string Name => _name;
    public string ParametersSchema => "{}";  // 参数固化在 Steps 里

    /// <summary>
    /// 应用全部 Step；step.Parameters 非空则按构造时固化的参数（用于
    /// "scale 1.5 再 translate 50" 这类异参数 step）；step.Parameters 为空则
    /// 用 Apply 调用方传入的 <paramref name="parameters"/>（用于多步共享同一
    /// runtime 参数的场景，如 launcher 对 decay-chain 多字段共用 factor）。
    /// </summary>
    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        IReadOnlyDictionary<string, object?> current = sourceData;
        foreach (var step in _steps)
        {
            var effective = step.Parameters.Count > 0 ? step.Parameters : parameters;
            current = step.Transformation.Apply(current, step.TargetFieldPath, effective);
        }
        return new Dictionary<string, object?>(current);
    }

    /// <summary>组合变换的单步。</summary>
    public sealed record Step(
        IMRTransformation Transformation,
        string TargetFieldPath,
        IReadOnlyDictionary<string, string> Parameters);
}
