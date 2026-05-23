using MetBench_BLL.Equations;
using MetBench_BLL.SystemMT.Transformations;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Equations;

/// <summary>
/// P2 — mr-architecture §5 IEquationFunction + Registry + RecipeBasedEquationFunction
///       + TransformationResolver 决策 B 分级查找。
/// </summary>
public sealed class EquationFunctionP2Tests
{
    // ── helpers ─────────────────────────────────────────────────────────────
    private static Dictionary<string, object?> D(params (string k, object? v)[] pairs)
    {
        var d = new Dictionary<string, object?>();
        foreach (var (k, v) in pairs) d[k] = v;
        return d;
    }

    // ── RecipeBasedEquationFunction — 解析与执行 ──────────────────────────

    [Fact]
    public void Recipe_single_step_ScaleField_works()
    {
        const string json = """
            {"compose":[{"op":"ScaleField","path":"x","params":{"factor":"2"}}]}
            """;
        var fn = new RecipeBasedEquationFunction("bateman", "Double", "transformation", json);
        var result = fn.Execute(D(("x", 3.0)), new Dictionary<string, string>());
        Assert.Equal(6.0, (double)result["x"]!);
    }

    [Fact]
    public void Recipe_two_steps_applied_in_order()
    {
        // Step1: x + 1 → 4.0;  Step2: x * 2 → 8.0
        const string json = """
            {"compose":[
              {"op":"MathAdd","path":"x","params":{"value":"1"}},
              {"op":"MathMul","path":"x","params":{"value":"2"}}
            ]}
            """;
        var fn = new RecipeBasedEquationFunction("eq", "F", "transformation", json);
        var result = fn.Execute(D(("x", 3.0)), new Dictionary<string, string>());
        Assert.Equal(8.0, (double)result["x"]!);
    }

    [Fact]
    public void Recipe_placeholder_substitution_works()
    {
        const string json = """
            {"compose":[{"op":"ScaleField","path":"n","params":{"factor":"{alpha}"}}]}
            """;
        var fn = new RecipeBasedEquationFunction("eq", "Scale", "transformation", json);
        var result = fn.Execute(D(("n", 5.0)),
            new Dictionary<string, string> { ["alpha"] = "3" });
        Assert.Equal(15.0, (double)result["n"]!);
    }

    [Fact]
    public void Recipe_multiple_placeholders_in_one_step()
    {
        // MathAdd with value={delta}; then MathMul with value={gamma}
        const string json = """
            {"compose":[
              {"op":"MathAdd","path":"v","params":{"value":"{delta}"}},
              {"op":"MathMul","path":"v","params":{"value":"{gamma}"}}
            ]}
            """;
        var fn = new RecipeBasedEquationFunction("eq", "F2", "transformation", json);
        var result = fn.Execute(D(("v", 2.0)),
            new Dictionary<string, string> { ["delta"] = "3", ["gamma"] = "4" });
        Assert.Equal(20.0, (double)result["v"]!); // (2+3)*4 = 20
    }

    [Fact]
    public void Recipe_empty_compose_returns_data_unchanged()
    {
        const string json = """{"compose":[]}""";
        var fn = new RecipeBasedEquationFunction("eq", "Noop", "transformation", json);
        var input = D(("x", 1.0));
        var result = fn.Execute(input, new Dictionary<string, string>());
        Assert.Equal(1.0, (double)result["x"]!);
    }

    [Fact]
    public void Recipe_unknown_op_throws_InvalidOperation()
    {
        const string json = """
            {"compose":[{"op":"NoSuchOp","path":"x","params":{}}]}
            """;
        var fn = new RecipeBasedEquationFunction("eq", "Bad", "transformation", json);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            fn.Execute(D(("x", 1.0)), new Dictionary<string, string>()));
        Assert.Contains("NoSuchOp", ex.Message);
    }

    [Fact]
    public void Recipe_unresolved_placeholder_throws_InvalidOperation()
    {
        const string json = """
            {"compose":[{"op":"ScaleField","path":"x","params":{"factor":"{missing}"}}]}
            """;
        var fn = new RecipeBasedEquationFunction("eq", "Bad2", "transformation", json);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            fn.Execute(D(("x", 1.0)), new Dictionary<string, string>()));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void Recipe_invalid_json_throws_on_construction()
        => Assert.Throws<ArgumentException>(() =>
            new RecipeBasedEquationFunction("eq", "Bad3", "transformation", "not-json"));

    [Fact]
    public void Recipe_exposes_metadata()
    {
        var fn = new RecipeBasedEquationFunction("bateman", "ScaleInitial",
            "transformation", """{"compose":[]}""");
        Assert.Equal("bateman", fn.EquationKey);
        Assert.Equal("ScaleInitial", fn.Name);
        Assert.Equal("transformation", fn.Kind);
    }

    // ── EquationFunctionRegistry ──────────────────────────────────────────

    [Fact]
    public void Registry_register_and_retrieve()
    {
        var reg = new EquationFunctionRegistry();
        var fn = new RecipeBasedEquationFunction("bateman", "F1", "transformation",
            """{"compose":[]}""");
        reg.Register(fn);
        Assert.True(reg.TryGet("bateman", "F1", out var found));
        Assert.Same(fn, found);
    }

    [Fact]
    public void Registry_TryGet_unknown_returns_false()
    {
        var reg = new EquationFunctionRegistry();
        Assert.False(reg.TryGet("bateman", "Unknown", out var f));
        Assert.Null(f);
    }

    [Fact]
    public void Registry_List_filters_by_equation_key()
    {
        var reg = new EquationFunctionRegistry();
        reg.Register(new RecipeBasedEquationFunction("bateman", "F1", "transformation", """{"compose":[]}"""));
        reg.Register(new RecipeBasedEquationFunction("bateman", "F2", "transformation", """{"compose":[]}"""));
        reg.Register(new RecipeBasedEquationFunction("heat",    "F3", "transformation", """{"compose":[]}"""));

        var list = reg.List("bateman");
        Assert.Equal(2, list.Count);
        Assert.All(list, f => Assert.Equal("bateman", f.EquationKey));
    }

    // ── TransformationResolver 决策 B 分级查找 ──────────────────────────

    private static TransformationResolver MakeResolver(EquationFunctionRegistry? reg = null)
        => new(reg ?? new EquationFunctionRegistry());

    [Fact]
    public void Resolver_finds_global_transformation()
    {
        var resolver = MakeResolver();
        // "ScaleField" is in global TransformationRegistry
        var t = resolver.Resolve("ScaleField", equationKey: "bateman");
        Assert.NotNull(t);
    }

    [Fact]
    public void Resolver_finds_equation_function_when_not_in_global()
    {
        var reg = new EquationFunctionRegistry();
        reg.Register(new RecipeBasedEquationFunction("bateman", "CustomFn", "transformation",
            """{"compose":[{"op":"MathAbs","path":"x","params":{}}]}"""));
        var resolver = new TransformationResolver(reg);

        var t = resolver.Resolve("CustomFn", equationKey: "bateman");
        Assert.NotNull(t);
        // verify it executes
        var result = t.Apply(new Dictionary<string, object?> { ["x"] = -2.0 }, "x",
            new Dictionary<string, string>());
        Assert.Equal(2.0, (double)result["x"]!);
    }

    [Fact]
    public void Resolver_global_wins_over_equation_namespace_when_name_conflicts()
    {
        // Register "ScaleField" also as an equation fn — global should win
        var reg = new EquationFunctionRegistry();
        reg.Register(new RecipeBasedEquationFunction("bateman", "ScaleField", "transformation",
            """{"compose":[]}"""));
        var resolver = new TransformationResolver(reg);

        var t = resolver.Resolve("ScaleField", equationKey: "bateman");
        // global ScaleField returns ScaleField instance, not the recipe noop
        Assert.Equal("ScaleField", t.Name);
        // applying it with factor should actually scale (not be a noop)
        var result = t.Apply(new Dictionary<string, object?> { ["x"] = 3.0 }, "x",
            new Dictionary<string, string> { ["factor"] = "2" });
        Assert.Equal(6.0, (double)result["x"]!);
    }

    [Fact]
    public void Resolver_throws_UnknownTransformation_when_both_miss()
    {
        var resolver = MakeResolver();
        var ex = Assert.Throws<UnknownTransformationException>(() =>
            resolver.Resolve("NoSuchOp", equationKey: "bateman"));
        Assert.Contains("NoSuchOp", ex.Message);
        Assert.Contains("bateman", ex.Message);
    }

    [Fact]
    public void Resolver_throws_when_equation_key_empty_and_global_misses()
    {
        var resolver = MakeResolver();
        Assert.Throws<UnknownTransformationException>(() =>
            resolver.Resolve("FancyOp", equationKey: ""));
    }

    [Fact]
    public void Resolver_equation_fn_lookup_skipped_when_equationKey_empty()
    {
        // Even if registry has a fn, empty equationKey skips equation lookup
        var reg = new EquationFunctionRegistry();
        reg.Register(new RecipeBasedEquationFunction("bateman", "EqOnly", "transformation",
            """{"compose":[]}"""));
        var resolver = new TransformationResolver(reg);
        Assert.Throws<UnknownTransformationException>(() =>
            resolver.Resolve("EqOnly", equationKey: ""));
    }
}
