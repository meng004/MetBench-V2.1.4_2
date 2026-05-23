using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_Domain;
using MetBench_SystemMT.Tests.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog;

/// <summary>
/// P3 — Catalog 服务扩展：
///   1) SystemMtCatalogService 新增方程函数 CRUD + 校验链
///   2) MethodMtCatalogService 对称实现（Kind=method-level 强制 + MetaPatternCode 拒绝）
/// </summary>
public sealed class P3CatalogExtensionTests
{
    // ── shared fakes ─────────────────────────────────────────────────────────
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAppRepo _apps = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterMrRepo _mrs = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterBindingRepo _bindings = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAuditRepo _audit = new();
    private readonly FakeMetadataRepo _meta = new();

    private SystemMtCatalogService MakeSvc()
        => new(_apps, _mrs, _bindings, _audit, _meta);

    // ── 1. SystemMtCatalogService: CreateEquationFunction ───────────────────

    [Fact]
    public async Task CreateEquationFunction_persists_recipe()
    {
        var svc = MakeSvc();
        var recipe = ValidRecipe("bateman", "ScaleInitial");

        await svc.CreateEquationFunctionAsync(recipe);

        var back = await svc.GetEquationFunctionAsync("bateman", "ScaleInitial");
        Assert.NotNull(back);
        Assert.Equal("bateman", back.EquationKey);
        Assert.Equal("ScaleInitial", back.FunctionName);
    }

    [Fact]
    public async Task CreateEquationFunction_is_idempotent_upsert()
    {
        var svc = MakeSvc();
        await svc.CreateEquationFunctionAsync(ValidRecipe("bateman", "F1",
            """{"compose":[{"op":"ScaleField","path":"x","params":{"factor":"2"}}]}"""));
        await svc.CreateEquationFunctionAsync(ValidRecipe("bateman", "F1",
            """{"compose":[{"op":"MathAbs","path":"x","params":{}}]}"""));

        var back = await svc.GetEquationFunctionAsync("bateman", "F1");
        Assert.NotNull(back);
        Assert.Contains("MathAbs", back.RecipeJson);
    }

    [Fact]
    public async Task ListEquationFunctions_filters_by_equation()
    {
        var svc = MakeSvc();
        await svc.CreateEquationFunctionAsync(ValidRecipe("bateman", "F1"));
        await svc.CreateEquationFunctionAsync(ValidRecipe("bateman", "F2"));
        await svc.CreateEquationFunctionAsync(ValidRecipe("heat",    "F3"));

        var list = await svc.ListEquationFunctionsAsync("bateman");
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.Equal("bateman", r.EquationKey));
    }

    [Fact]
    public async Task GetEquationFunction_returns_null_for_unknown()
    {
        var svc = MakeSvc();
        Assert.Null(await svc.GetEquationFunctionAsync("bateman", "Nope"));
    }

    // ── validation: reject invalid recipes ───────────────────────────────────

    [Fact]
    public async Task CreateEquationFunction_rejects_unknown_op()
    {
        var svc = MakeSvc();
        var recipe = ValidRecipe("eq", "Bad",
            """{"compose":[{"op":"NoSuchOp","path":"x","params":{}}]}""");
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateEquationFunctionAsync(recipe));
        Assert.Contains("NoSuchOp", ex.Message);
    }

    [Fact]
    public async Task CreateEquationFunction_rejects_invalid_json()
    {
        var svc = MakeSvc();
        var recipe = new EquationFunctionRecipe
            { EquationKey = "eq", FunctionName = "Bad", RecipeJson = "not-json" };
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateEquationFunctionAsync(recipe));
    }

    [Fact]
    public async Task CreateEquationFunction_rejects_blank_equation_key()
    {
        var svc = MakeSvc();
        var recipe = ValidRecipe("", "F1");
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateEquationFunctionAsync(recipe));
    }

    [Fact]
    public async Task CreateEquationFunction_rejects_blank_function_name()
    {
        var svc = MakeSvc();
        var recipe = ValidRecipe("bateman", "");
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateEquationFunctionAsync(recipe));
    }

    // ── 2. MethodMtCatalogService ─────────────────────────────────────────────

    private MethodMtCatalogService MakeMethodSvc()
        => new(_apps, _mrs, _bindings, _audit);

    [Fact]
    public void MethodSvc_CreateMr_assigns_method_level_kind()
    {
        var svc = MakeMethodSvc();
        var mr = new MetamorphicRelation { Code = "MR-METHOD-01" };
        svc.CreateMr(mr);

        var found = svc.FindMrByCode("MR-METHOD-01");
        Assert.NotNull(found);
        Assert.Equal("method-level", found.Kind);
    }

    [Fact]
    public void MethodSvc_CreateMr_rejects_MetaPatternCode_non_empty()
    {
        var svc = MakeMethodSvc();
        var mr = new MetamorphicRelation
            { Code = "MR-M", MetaPatternCode = "m_inv" };
        var ex = Assert.Throws<ArgumentException>(() => svc.CreateMr(mr));
        Assert.Contains("MetaPatternCode", ex.Message);
    }

    [Fact]
    public void MethodSvc_ListMrs_filters_method_level_only()
    {
        // seed a system-level MR directly in the shared _mrs fake
        _mrs.Data.Add(new MetamorphicRelation
            { Code = "SYS-01", Kind = "system-level" });
        var svc = MakeMethodSvc();
        svc.CreateMr(new MetamorphicRelation { Code = "MR-M-01" });

        var list = svc.ListMrs();
        Assert.Single(list);
        Assert.Equal("MR-M-01", list[0].Code);
    }

    // ── helper factories ────────────────────────────────────────────────────
    private static EquationFunctionRecipe ValidRecipe(
        string eq, string fn,
        string? json = null)
        => new()
        {
            EquationKey  = eq,
            FunctionName = fn,
            RecipeJson   = json ?? """{"compose":[{"op":"ScaleField","path":"x","params":{"factor":"2"}}]}""",
        };

    // ── in-memory fake ISystemMtMetadataRepository ──────────────────────────
    internal sealed class FakeMetadataRepo : ISystemMtMetadataRepository
    {
        private readonly Dictionary<string, EquationMetadata> _eqs = new();
        private readonly Dictionary<string, MrMetadata>       _mrsM = new();
        private readonly List<EquationFunctionRecipe>         _recipes = new();

        public Task UpsertEquationAsync(EquationMetadata e, CancellationToken ct = default)
        { _eqs[e.EquationKey] = e; return Task.CompletedTask; }

        public Task<EquationMetadata?> GetEquationAsync(string k, CancellationToken ct = default)
            => Task.FromResult(_eqs.TryGetValue(k, out var v) ? v : null);

        public Task<IReadOnlyList<EquationMetadata>> ListEquationsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EquationMetadata>>(_eqs.Values.OrderBy(e => e.EquationKey).ToList());

        public Task UpsertMrAsync(MrMetadata mr, CancellationToken ct = default)
        { _mrsM[mr.MrId] = mr; return Task.CompletedTask; }

        public Task<MrMetadata?> GetMrAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_mrsM.TryGetValue(id, out var v) ? v : null);

        public Task<IReadOnlyList<MrMetadata>> ListMrsByEquationAsync(string eq, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrMetadata>>(
                _mrsM.Values.Where(m => m.EquationKey == eq).OrderBy(m => m.MrId).ToList());

        public Task<IReadOnlyList<MrMetadata>> ListMrsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrMetadata>>(_mrsM.Values.OrderBy(m => m.MrId).ToList());

        public Task UpsertRecipeAsync(EquationFunctionRecipe r, CancellationToken ct = default)
        {
            var idx = _recipes.FindIndex(x =>
                x.EquationKey == r.EquationKey && x.FunctionName == r.FunctionName);
            if (idx < 0)
            {
                if (r.Id == Guid.Empty) r.Id = Guid.NewGuid();
                _recipes.Add(r);
            }
            else
            {
                var existing = _recipes[idx];
                r.Id = existing.Id;
                _recipes[idx] = r;
            }
            return Task.CompletedTask;
        }

        public Task<EquationFunctionRecipe?> GetRecipeAsync(string eq, string fn, CancellationToken ct = default)
            => Task.FromResult(_recipes.FirstOrDefault(r =>
                r.EquationKey == eq && r.FunctionName == fn));

        public Task<IReadOnlyList<EquationFunctionRecipe>> ListRecipesByEquationAsync(
            string eq, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EquationFunctionRecipe>>(
                _recipes.Where(r => r.EquationKey == eq).OrderBy(r => r.FunctionName).ToList());

        public Task<IReadOnlyList<EquationFunctionRecipe>> ListRecipesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EquationFunctionRecipe>>(
                _recipes.OrderBy(r => r.EquationKey).ThenBy(r => r.FunctionName).ToList());

        public Task<bool> DeleteEquationAsync(string eq, CancellationToken ct = default)
            => Task.FromResult(_eqs.Remove(eq));

        public Task<bool> DeleteMrAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_mrsM.Remove(id));

        public Task<bool> DeleteRecipeAsync(string eq, string fn, CancellationToken ct = default)
        {
            var removed = _recipes.RemoveAll(r => r.EquationKey == eq && r.FunctionName == fn);
            return Task.FromResult(removed > 0);
        }
    }
}
