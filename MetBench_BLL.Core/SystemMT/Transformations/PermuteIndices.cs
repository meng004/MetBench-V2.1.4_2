using MetBench_BLL.SystemMT.ParameterMapping;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// 数组索引置换变换 — 把目标字段（必须是数组）按给定 permutation 重排。
/// </summary>
/// <remarks>
/// 参数：<c>{"permutation": "1,0"}</c>（逗号分隔索引列表）。
/// 例：原数组 [a, b]，permutation="1,0" → [b, a]。
///
/// 用于 NOETHER MR04 PermuteEnergyGroups：把多群截面数组的群顺序对调，
/// k_eff 应保持不变（m_inv 对称性）。
/// </remarks>
public sealed class PermuteIndices : IMRTransformation
{
    private readonly IFieldPathResolver _resolver;

    public PermuteIndices() : this(new JsonPointerResolver()) { }

    public PermuteIndices(IFieldPathResolver resolver)
    {
        _resolver = resolver;
    }

    public string Name => "PermuteIndices";

    public string ParametersSchema => """
        {
          "type": "object",
          "required": ["permutation"],
          "properties": { "permutation": { "type": "string", "pattern": "^[0-9]+(,[0-9]+)*$" } }
        }
        """;

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("permutation", out var permStr))
            throw new ArgumentException("PermuteIndices requires 'permutation' parameter");

        var indices = permStr.Split(',').Select(int.Parse).ToArray();
        var oldValue = _resolver.Get(sourceData, targetFieldPath);

        if (oldValue is not System.Collections.IList list)
            throw new InvalidOperationException(
                $"PermuteIndices target must be a list at '{targetFieldPath}', " +
                $"got {oldValue?.GetType().Name ?? "null"}");

        if (indices.Length != list.Count)
            throw new ArgumentException(
                $"Permutation length {indices.Length} != array length {list.Count} at '{targetFieldPath}'");

        // 验证 permutation 是合法 — 是 [0, n) 的一个排列
        var sorted = indices.OrderBy(i => i).ToArray();
        for (int i = 0; i < sorted.Length; i++)
        {
            if (sorted[i] != i)
                throw new ArgumentException(
                    $"Permutation must be a valid bijection of [0, {list.Count})");
        }

        var newList = new List<object?>(list.Count);
        for (int i = 0; i < indices.Length; i++)
        {
            newList.Add(list[indices[i]]);
        }
        return _resolver.Set(sourceData, targetFieldPath, newList);
    }
}
