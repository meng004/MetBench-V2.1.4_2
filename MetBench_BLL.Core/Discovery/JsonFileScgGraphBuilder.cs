using System.Text.Json;

namespace MetBench_BLL.Discovery;

/// <summary>
/// W11.1 续 — 从 JSON 文件加载 <see cref="ScgGraph"/> 的生产实现。
/// </summary>
/// <remarks>
/// 设计选择：用 JSON 文件而非源码挖掘 / NetworkX sidecar，原因：
/// <list type="bullet">
///   <item>领域专家直接手写 + git diff 可审；不需要 Python toolchain</item>
///   <item>每个 SUT 在 <c>SUT/&lt;code&gt;/scg.json</c> 放自己的图；统一 schema</item>
///   <item>未来要换 NetworkX / 源码挖掘 → 实现新 builder，不影响 ScgHeuristicDiscoverer</item>
/// </list>
///
/// JSON schema：
/// <code>
/// {
///   "nodes": [{"id":"...", "name":"...", "type":"input|param|intermediate|output"}, ...],
///   "edges": [{"from":"...", "to":"...", "relation":"causes|mediates|confounds"}, ...]
/// }
/// </code>
///
/// 应用映射：构造期注入 <c>appId → 文件路径</c> 的字典，
/// <c>BuildAsync(appId)</c> 找文件即读。<c>appId == null</c> 走 default 路径（也可不配置）。
/// 找不到对应 SUT 的 JSON → 抛 <see cref="FileNotFoundException"/>，由 Discoverer 包成 error outcome。
/// </remarks>
public sealed class JsonFileScgGraphBuilder : IScgGraphBuilder
{
    private readonly IReadOnlyDictionary<int, string> _appToPath;
    private readonly string? _defaultPath;

    public JsonFileScgGraphBuilder(
        IReadOnlyDictionary<int, string> appToPath,
        string? defaultPath = null)
    {
        _appToPath = appToPath;
        _defaultPath = defaultPath;
    }

    public async Task<ScgGraph> BuildAsync(int? targetApplicationId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        string? path = null;
        if (targetApplicationId.HasValue && _appToPath.TryGetValue(targetApplicationId.Value, out var mapped))
            path = mapped;
        else
            path = _defaultPath;
        if (path is null)
            throw new FileNotFoundException(
                $"No SCG file mapped for application id {targetApplicationId?.ToString() ?? "(null)"} and no default path configured.");
        if (!File.Exists(path))
            throw new FileNotFoundException($"SCG file not found at: {path}", path);

        await using var fs = File.OpenRead(path);
        return await ParseAsync(fs, ct).ConfigureAwait(false);
    }

    /// <summary>解析 SCG JSON 流到 <see cref="ScgGraph"/>。Internal 暴露便于单测直接喂流。</summary>
    internal static async Task<ScgGraph> ParseAsync(Stream json, CancellationToken ct = default)
    {
        using var doc = await JsonDocument.ParseAsync(json, cancellationToken: ct).ConfigureAwait(false);
        var nodes = new List<ScgNode>();
        var edges = new List<ScgEdge>();

        if (doc.RootElement.TryGetProperty("nodes", out var nodesEl) && nodesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var n in nodesEl.EnumerateArray())
            {
                var id = n.GetProperty("id").GetString() ?? throw new InvalidDataException("scg node missing 'id'");
                var name = n.TryGetProperty("name", out var nm) ? (nm.GetString() ?? id) : id;
                var type = n.TryGetProperty("type", out var tp) ? (tp.GetString() ?? "intermediate") : "intermediate";
                nodes.Add(new ScgNode(id, name, type));
            }
        }
        if (doc.RootElement.TryGetProperty("edges", out var edgesEl) && edgesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in edgesEl.EnumerateArray())
            {
                var from = e.GetProperty("from").GetString() ?? throw new InvalidDataException("scg edge missing 'from'");
                var to = e.GetProperty("to").GetString() ?? throw new InvalidDataException("scg edge missing 'to'");
                var rel = e.TryGetProperty("relation", out var rl) ? (rl.GetString() ?? "causes") : "causes";
                edges.Add(new ScgEdge(from, to, rel));
            }
        }
        return new ScgGraph(nodes, edges);
    }
}

/// <summary>
/// 测试 / 探索用：直接喂 <see cref="ScgGraph"/> 实例。
/// 生产代码请用 <see cref="JsonFileScgGraphBuilder"/>。
/// </summary>
public sealed class InMemoryScgGraphBuilder : IScgGraphBuilder
{
    private readonly ScgGraph _graph;
    public InMemoryScgGraphBuilder(ScgGraph graph) { _graph = graph; }
    public Task<ScgGraph> BuildAsync(int? targetApplicationId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_graph);
    }
}
