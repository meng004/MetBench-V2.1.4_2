using MetBench_BLL.SystemMT.Transformations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MetBench_BLL.Equations;

/// <summary>
/// L1 数据驱动方程函数 — 将 Recipe JSON 解析为 L0 算子调用序列并串行执行。
/// 占位参数格式：值为 <c>{paramName}</c>（整体占位），运行时从调用方 parameters 替换。
/// </summary>
public sealed class RecipeBasedEquationFunction : IEquationFunction
{
    private readonly List<ComposeStep> _steps;

    public string EquationKey { get; }
    public string Name { get; }
    public string Kind { get; }

    public RecipeBasedEquationFunction(
        string equationKey, string name, string kind, string recipeJson)
    {
        EquationKey = equationKey;
        Name = name;
        Kind = kind;

        try
        {
            var root = JsonSerializer.Deserialize<RecipeRoot>(recipeJson,
                         JsonOptions) ??
                       throw new ArgumentException("Recipe JSON deserialized to null");
            _steps = root.Compose ?? new List<ComposeStep>();
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid Recipe JSON: {ex.Message}", ex);
        }
    }

    public Dictionary<string, object?> Execute(
        IReadOnlyDictionary<string, object?> sourceData,
        IReadOnlyDictionary<string, string> parameters)
    {
        Dictionary<string, object?> current = new(sourceData);
        foreach (var step in _steps)
        {
            IMRTransformation op;
            try
            {
                op = TransformationRegistry.Get(step.Op);
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException(
                    $"Recipe step references unknown op '{step.Op}'. " +
                    $"Registered: [{string.Join(", ", TransformationRegistry.AvailableNames)}]");
            }

            var resolvedParams = ResolveParams(step.Params ?? new Dictionary<string, string>(), parameters, step.Op);
            current = op.Apply(current, step.Path, resolvedParams);
        }
        return current;
    }

    // ── internals ────────────────────────────────────────────────────────────

    private static readonly Regex PlaceholderRe = new(@"^\{([^}]+)\}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static IReadOnlyDictionary<string, string> ResolveParams(
        Dictionary<string, string> stepParams,
        IReadOnlyDictionary<string, string> callerParams,
        string opName)
    {
        var result = new Dictionary<string, string>(stepParams.Count);
        foreach (var (key, value) in stepParams)
        {
            var m = PlaceholderRe.Match(value);
            if (m.Success)
            {
                var paramName = m.Groups[1].Value;
                if (!callerParams.TryGetValue(paramName, out var resolved))
                    throw new InvalidOperationException(
                        $"Op '{opName}': placeholder '{{{paramName}}}' not supplied by caller. " +
                        $"Available: [{string.Join(", ", callerParams.Keys)}]");
                result[key] = resolved;
            }
            else
            {
                result[key] = value;
            }
        }
        return result;
    }

    // ── private DTOs ────────────────────────────────────────────────────────

    private sealed class RecipeRoot
    {
        [JsonPropertyName("compose")]
        public List<ComposeStep>? Compose { get; set; }
    }

    private sealed class ComposeStep
    {
        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public Dictionary<string, string>? Params { get; set; }
    }
}
