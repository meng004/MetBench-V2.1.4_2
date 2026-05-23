using MetBench_BLL.SystemMT.ParameterMapping;
using System.Globalization;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// 中子物理一致性变换 —— scale fuel.sigma_a 同时调整 fuel.sigma_t,
/// 维持 <c>sigma_t ≡ sigma_a + Σ sigma_s</c>(transport 方程要求)。
/// </summary>
/// <remarks>
/// 参数:<c>{"factor": "1.5"}</c>。targetFieldPath 指向 fuel container(如
/// <c>/materials/fuel</c>),容器需含 <c>sigma_a</c> 与 <c>sigma_t</c> 两个等长数组。
///
/// 物理:scale 吸收截面后,total 截面也要相应调整。设
/// <list type="bullet">
///   <item><c>new_sigma_a_g = factor × old_sigma_a_g</c></item>
///   <item><c>delta_t_g     = (factor − 1) × old_sigma_a_g</c></item>
///   <item><c>new_sigma_t_g = old_sigma_t_g + delta_t_g</c></item>
/// </list>
/// 等价于把 <c>sigma_t = sigma_a + Σ sigma_s</c> 中的 sigma_a 部分跟着 scale,
/// 散射部分保持不变。
///
/// 替代 World 1 的 <c>SUT/openm{oc,c}/*_input_adapter_sigma_a.py</c>:
/// 同样的物理,搬到 C# 通用 transformation 后,OpenMOC 与 OpenMC 共用同一份(因二者
/// 输入 JSON shape 一致),不再需要 per-SUT 的 Python adapter 维护此跨字段更新。
/// </remarks>
public sealed class ScaleFuelAbsorption : IMRTransformation
{
    private readonly IFieldPathResolver _resolver;

    public ScaleFuelAbsorption() : this(new JsonPointerResolver()) { }

    public ScaleFuelAbsorption(IFieldPathResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public string Name => "ScaleFuelAbsorption";

    public string ParametersSchema => """
        {
          "type": "object",
          "required": ["factor"],
          "properties": { "factor": { "type": "string", "pattern": "^[+]?[0-9]*\\.?[0-9]+([eE][-+]?[0-9]+)?$" } }
        }
        """;

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("factor", out var factorStr))
            throw new ArgumentException("ScaleFuelAbsorption requires 'factor' parameter");
        if (!double.TryParse(factorStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            throw new ArgumentException(
                $"ScaleFuelAbsorption 'factor' must be a number; got '{factorStr}'");

        var raw = _resolver.Get(sourceData, targetFieldPath);
        if (raw is not IReadOnlyDictionary<string, object?> container)
        {
            // Set 流程同样会处理 IDictionary,但我们需要按 key 读 sigma_a/sigma_t
            if (raw is not IDictionary<string, object?> mutable)
            {
                throw new InvalidOperationException(
                    $"ScaleFuelAbsorption requires a dict container at '{targetFieldPath}'; "
                    + $"got {raw?.GetType().Name ?? "null"}");
            }
            container = new Dictionary<string, object?>(mutable);
        }

        var oldSigmaA = RequireArray(container, "sigma_a", targetFieldPath);
        var oldSigmaT = RequireArray(container, "sigma_t", targetFieldPath);
        if (oldSigmaA.Count != oldSigmaT.Count)
        {
            throw new InvalidOperationException(
                $"ScaleFuelAbsorption: sigma_a length {oldSigmaA.Count} != sigma_t length {oldSigmaT.Count} at '{targetFieldPath}'");
        }

        var newSigmaA = new List<object?>(oldSigmaA.Count);
        var newSigmaT = new List<object?>(oldSigmaT.Count);
        for (var g = 0; g < oldSigmaA.Count; g++)
        {
            var oldA = ToDouble(oldSigmaA[g], targetFieldPath, "sigma_a", g);
            var oldT = ToDouble(oldSigmaT[g], targetFieldPath, "sigma_t", g);
            newSigmaA.Add(oldA * factor);
            newSigmaT.Add(oldT + (factor - 1.0) * oldA);
        }

        // 复制 container, 覆盖两个数组(其余字段保持引用)。
        var newContainer = new Dictionary<string, object?>(container)
        {
            ["sigma_a"] = newSigmaA,
            ["sigma_t"] = newSigmaT,
        };
        return _resolver.Set(sourceData, targetFieldPath, newContainer);
    }

    private static System.Collections.IList RequireArray(
        IReadOnlyDictionary<string, object?> container, string key, string path)
    {
        if (!container.TryGetValue(key, out var v) || v is not System.Collections.IList list)
        {
            throw new InvalidOperationException(
                $"ScaleFuelAbsorption: container at '{path}' missing '{key}' array");
        }
        return list;
    }

    private static double ToDouble(object? value, string path, string field, int index) => value switch
    {
        double d  => d,
        float f   => f,
        int i     => i,
        long l    => l,
        decimal m => (double)m,
        _ => throw new InvalidOperationException(
            $"ScaleFuelAbsorption: non-numeric value in {field}[{index}] at '{path}'"),
    };
}
