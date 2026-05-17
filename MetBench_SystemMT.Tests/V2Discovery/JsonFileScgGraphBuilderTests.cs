using System.Text;
using MetBench_BLL.Discovery;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class JsonFileScgGraphBuilderTests : IDisposable
{
    private readonly string _tempDir;

    public JsonFileScgGraphBuilderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MetBenchScgFileTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* test cleanup best-effort */ }
    }

    [Fact]
    public async Task ParseAsync_handles_minimal_schema()
    {
        var json = """
            {
              "nodes": [{"id":"a", "name":"A", "type":"input"}],
              "edges": []
            }
            """;
        using var s = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var g = await JsonFileScgGraphBuilder.ParseAsync(s);
        Assert.Single(g.Nodes);
        Assert.Empty(g.Edges);
        Assert.Equal("A", g.Nodes[0].Name);
    }

    [Fact]
    public async Task ParseAsync_defaults_missing_optional_fields()
    {
        var json = """
            {
              "nodes": [{"id":"x"}],
              "edges": [{"from":"x", "to":"y"}]
            }
            """;
        using var s = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var g = await JsonFileScgGraphBuilder.ParseAsync(s);
        Assert.Equal("x", g.Nodes[0].Id);
        Assert.Equal("x", g.Nodes[0].Name);          // name defaults to id
        Assert.Equal("intermediate", g.Nodes[0].Type); // type defaults
        Assert.Equal("causes", g.Edges[0].Relation);   // relation defaults
    }

    [Fact]
    public async Task ParseAsync_throws_on_missing_required_id()
    {
        var json = """{"nodes":[{"name":"no-id"}], "edges":[]}""";
        using var s = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await JsonFileScgGraphBuilder.ParseAsync(s));
    }

    [Fact]
    public async Task BuildAsync_loads_per_application_path()
    {
        var file = Path.Combine(_tempDir, "app1.json");
        File.WriteAllText(file, """
            {"nodes":[{"id":"a"}, {"id":"b"}],
             "edges":[{"from":"a","to":"b"}]}
            """);
        var builder = new JsonFileScgGraphBuilder(
            new Dictionary<int, string> { [42] = file });

        var g = await builder.BuildAsync(targetApplicationId: 42);

        Assert.Equal(2, g.NodeCount);
        Assert.Equal(1, g.EdgeCount);
    }

    [Fact]
    public async Task BuildAsync_falls_back_to_default_when_app_unmapped()
    {
        var file = Path.Combine(_tempDir, "default.json");
        File.WriteAllText(file, """{"nodes":[{"id":"d"}], "edges":[]}""");
        var builder = new JsonFileScgGraphBuilder(
            appToPath: new Dictionary<int, string>(),
            defaultPath: file);

        var g = await builder.BuildAsync(targetApplicationId: 99);

        Assert.Equal(1, g.NodeCount);
        Assert.Equal("d", g.Nodes[0].Id);
    }

    [Fact]
    public async Task BuildAsync_throws_when_no_path_resolved()
    {
        var builder = new JsonFileScgGraphBuilder(new Dictionary<int, string>());
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await builder.BuildAsync(targetApplicationId: 1));
    }

    [Fact]
    public async Task BuildAsync_throws_when_mapped_file_missing()
    {
        var builder = new JsonFileScgGraphBuilder(
            new Dictionary<int, string> { [1] = Path.Combine(_tempDir, "does-not-exist.json") });
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await builder.BuildAsync(targetApplicationId: 1));
    }

    // ===== Integration with miner using the real repo SCG file =====

    [Fact]
    public async Task Real_openmoc_scg_yields_expected_pattern_categories()
    {
        var repoRoot = FindRepoRoot();
        var scgPath = Path.Combine(repoRoot, "SUT", "openmoc", "scg.json");
        Assert.True(File.Exists(scgPath), $"OpenMOC SCG file not found at {scgPath}");

        var builder = new JsonFileScgGraphBuilder(
            new Dictionary<int, string> { [1] = scgPath });
        var discoverer = new ScgHeuristicDiscoverer(builder, new RuleBasedScgPatternMiner());

        var outcome = await discoverer.DiscoverAsync(targetApplicationId: 1);

        Assert.Equal("ok", outcome.Status);
        Assert.NotEmpty(outcome.Proposals);
        // OpenMOC SCG should produce all 3 pattern kinds
        Assert.Contains(outcome.Proposals, p => p.ProposedCode.StartsWith("SCG-direct-cause-"));
        Assert.Contains(outcome.Proposals, p => p.ProposedCode.StartsWith("SCG-mediator-"));
        Assert.Contains(outcome.Proposals, p => p.ProposedCode.StartsWith("SCG-confounder-"));
    }

    [Fact]
    public async Task InMemory_builder_round_trips_graph_unchanged()
    {
        var g = new ScgGraph(
            new[] { new ScgNode("a", "A", "input"), new ScgNode("b", "B", "output") },
            new[] { new ScgEdge("a", "b", "causes") });
        var builder = new InMemoryScgGraphBuilder(g);

        var result = await builder.BuildAsync(targetApplicationId: null);

        Assert.Same(g, result);
    }

    [Fact]
    public async Task InMemory_builder_respects_cancellation()
    {
        var g = new ScgGraph(Array.Empty<ScgNode>(), Array.Empty<ScgEdge>());
        var builder = new InMemoryScgGraphBuilder(g);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            builder.BuildAsync(null, cts.Token));
    }

    private static string FindRepoRoot()
    {
        // Walk up from current AppContext.BaseDirectory until we hit MetBench.sln
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "MetBench.sln"))) return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        throw new InvalidOperationException("Could not locate repo root (MetBench.sln).");
    }
}
