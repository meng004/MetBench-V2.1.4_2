using LiteDB;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_DAL;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Metadata;

/// <summary>
/// P0 — mr-architecture.md §8 第 1 步:schema 入位(无行为变化)。
/// 1) <see cref="MetamorphicRelation"/> 加 <c>EquationKey</c> + <c>ValueShape</c>(默认 "scalar")
/// 2) <see cref="EquationMetadata"/> 加 <c>Functions: List&lt;EquationFunctionDescriptor&gt;</c>
/// 3) 新建 <see cref="EquationFunctionRecipe"/> 实体 + 仓储方法(扩 ISystemMtMetadataRepository)
/// 计划见 docs/superpowers/plans/2026-05-23-mr-architecture-implementation-plan.md。
/// </summary>
public sealed class MrArchitectureSchemaP0Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbSystemMtMetadataRepository _repo;

    public MrArchitectureSchemaP0Tests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), "MetBenchSchemaP0",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _repo = new LiteDbSystemMtMetadataRepository(_dbPath);
    }

    public void Dispose()
    {
        _repo.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    // ===================== 1. MetamorphicRelation 新字段 =====================

    [Fact]
    public void MetamorphicRelation_default_EquationKey_is_empty_and_ValueShape_is_scalar()
    {
        var mr = new MetamorphicRelation();
        Assert.Equal(string.Empty, mr.EquationKey);
        Assert.Equal("scalar", mr.ValueShape);
    }

    [Fact]
    public void MetamorphicRelation_roundtrips_new_fields_via_LiteDB()
    {
        var dbFile = _dbPath + "-mr.db";
        using (var db = new LiteDatabase($"Filename={dbFile}"))
        {
            var col = db.GetCollection<MetamorphicRelation>("Mrs");
            col.EnsureIndex(x => x.IdMR, unique: true);

            var mr = new MetamorphicRelation
            {
                Code = "MR-TEST-P0",
                Kind = "system-level",
                EquationKey = "bateman",
                ValueShape = "vector",
            };
            col.Insert(mr);

            var back = col.FindOne(x => x.Code == "MR-TEST-P0");
            Assert.NotNull(back);
            Assert.Equal("bateman", back.EquationKey);
            Assert.Equal("vector", back.ValueShape);
        }

        File.Delete(dbFile);
        var l = dbFile + "-log";
        if (File.Exists(l)) File.Delete(l);
    }

    [Fact]
    public void MetamorphicRelation_reading_old_row_without_new_fields_uses_defaults()
    {
        var path = _dbPath + "-old.db";
        // 写一个不含新字段的"老"文档,模拟既有 v2 数据
        using (var db = new LiteDatabase($"Filename={path}"))
        {
            var raw = db.GetCollection("Mrs");
            raw.Insert(new BsonDocument
            {
                ["_id"] = 7,
                ["Code"] = "MR-LEGACY",
                ["Kind"] = "system-level",
                ["TransformationName"] = "ScaleField",
                // 故意不写 EquationKey / ValueShape
            });
        }
        // 用强类型读
        using (var db = new LiteDatabase($"Filename={path}"))
        {
            var col = db.GetCollection<MetamorphicRelation>("Mrs");
            var mr = col.FindById(7);
            Assert.NotNull(mr);
            Assert.Equal("MR-LEGACY", mr.Code);
            Assert.Equal(string.Empty, mr.EquationKey);   // 默认
            Assert.Equal("scalar", mr.ValueShape);         // 默认
        }
        File.Delete(path);
        var l = path + "-log";
        if (File.Exists(l)) File.Delete(l);
    }

    // ===================== 2. EquationMetadata.Functions =====================

    [Fact]
    public void EquationMetadata_default_Functions_list_is_empty()
    {
        var eq = new EquationMetadata { EquationKey = "x" };
        Assert.NotNull(eq.Functions);
        Assert.Empty(eq.Functions);
    }

    [Fact]
    public async Task EquationMetadata_with_Functions_round_trips_via_repo()
    {
        var eq = new EquationMetadata
        {
            EquationKey = "bateman",
            Name = "Bateman decay chain",
            CanonicalForm = "dN/dt = -λN + ...",
            Functions = new List<EquationFunctionDescriptor>
            {
                new() { Name = "ScaleInitial", Kind = "transformation",
                    Signature = "(initial, factor) → initial",
                    DisplayLatex = @"\hat N_i = \alpha \cdot N_i", ImplKey = "" },
                new() { Name = "AnalyticSolution", Kind = "solution",
                    Signature = "(initial, t) → N(t)",
                    DisplayLatex = @"N_A(t) = N_{A0} e^{-\lambda_A t}",
                    ImplKey = "bateman.AnalyticSolution" },
            },
        };

        await _repo.UpsertEquationAsync(eq);

        var back = await _repo.GetEquationAsync("bateman");
        Assert.NotNull(back);
        Assert.Equal(2, back.Functions.Count);
        var scale = back.Functions.Single(f => f.Name == "ScaleInitial");
        Assert.Equal("transformation", scale.Kind);
        // L1 路径 = ImplKey 空(LiteDB 反序列化 "" 为 null,语义等价 — 见设计 §5.1)
        Assert.True(string.IsNullOrEmpty(scale.ImplKey));
        var solve = back.Functions.Single(f => f.Name == "AnalyticSolution");
        Assert.Equal("bateman.AnalyticSolution", solve.ImplKey);  // L2:非空 ImplKey
    }

    // ===================== 3. EquationFunctionRecipe 仓储 =====================

    [Fact]
    public async Task UpsertRecipeAsync_inserts_new_row_and_assigns_Id()
    {
        var recipe = new EquationFunctionRecipe
        {
            EquationKey = "bateman",
            FunctionName = "ScaleInitial",
            RecipeJson = """{"compose":[{"op":"ScaleField","path":"/initial/N_A","params":{"factor":"{factor}"}}]}""",
            ParameterSchemaJson = """[{"name":"factor","type":"real","required":true}]""",
            CreatedBy = "test",
        };

        await _repo.UpsertRecipeAsync(recipe);

        Assert.NotEqual(Guid.Empty, recipe.Id);

        var back = await _repo.GetRecipeAsync("bateman", "ScaleInitial");
        Assert.NotNull(back);
        Assert.Equal("bateman", back.EquationKey);
        Assert.Equal("ScaleInitial", back.FunctionName);
        Assert.Contains("ScaleField", back.RecipeJson);
        Assert.Contains("factor", back.ParameterSchemaJson);
    }

    [Fact]
    public async Task UpsertRecipeAsync_with_same_key_pair_updates_in_place()
    {
        var r1 = new EquationFunctionRecipe
        {
            EquationKey = "bateman", FunctionName = "ScaleInitial",
            RecipeJson = "{\"v\":1}", CreatedBy = "alice",
        };
        await _repo.UpsertRecipeAsync(r1);
        var firstId = r1.Id;

        var r2 = new EquationFunctionRecipe
        {
            EquationKey = "bateman", FunctionName = "ScaleInitial",
            RecipeJson = "{\"v\":2}", CreatedBy = "bob",
        };
        await _repo.UpsertRecipeAsync(r2);

        var back = await _repo.GetRecipeAsync("bateman", "ScaleInitial");
        Assert.NotNull(back);
        Assert.Equal(firstId, back.Id);  // id 复用,行不增
        Assert.Contains("\"v\":2", back.RecipeJson);
        Assert.Equal("bob", back.CreatedBy);

        var all = await _repo.ListRecipesAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task GetRecipeAsync_returns_null_for_unknown_pair()
    {
        Assert.Null(await _repo.GetRecipeAsync("bateman", "NotThere"));
        Assert.Null(await _repo.GetRecipeAsync("nope", "Anything"));
        Assert.Null(await _repo.GetRecipeAsync("", ""));
    }

    [Fact]
    public async Task ListRecipesByEquationAsync_filters_by_equation_key()
    {
        await _repo.UpsertRecipeAsync(new EquationFunctionRecipe
            { EquationKey = "bateman", FunctionName = "A", RecipeJson = "{}" });
        await _repo.UpsertRecipeAsync(new EquationFunctionRecipe
            { EquationKey = "bateman", FunctionName = "B", RecipeJson = "{}" });
        await _repo.UpsertRecipeAsync(new EquationFunctionRecipe
            { EquationKey = "heat-equation", FunctionName = "C", RecipeJson = "{}" });

        var bateman = await _repo.ListRecipesByEquationAsync("bateman");
        Assert.Equal(2, bateman.Count);
        Assert.All(bateman, r => Assert.Equal("bateman", r.EquationKey));

        var heat = await _repo.ListRecipesByEquationAsync("heat-equation");
        Assert.Single(heat);
        Assert.Equal("C", heat[0].FunctionName);

        var nothing = await _repo.ListRecipesByEquationAsync("unknown");
        Assert.Empty(nothing);
    }

    [Fact]
    public async Task ListRecipesAsync_returns_all_ordered_by_equation_then_name()
    {
        await _repo.UpsertRecipeAsync(new EquationFunctionRecipe
            { EquationKey = "heat-equation", FunctionName = "ScaleAmplitude", RecipeJson = "{}" });
        await _repo.UpsertRecipeAsync(new EquationFunctionRecipe
            { EquationKey = "bateman", FunctionName = "ScaleInitial", RecipeJson = "{}" });
        await _repo.UpsertRecipeAsync(new EquationFunctionRecipe
            { EquationKey = "bateman", FunctionName = "AnalyticSolution", RecipeJson = "{}" });

        var all = await _repo.ListRecipesAsync();
        Assert.Equal(3, all.Count);
        // bateman.AnalyticSolution, bateman.ScaleInitial, heat-equation.ScaleAmplitude
        Assert.Equal(("bateman", "AnalyticSolution"),
            (all[0].EquationKey, all[0].FunctionName));
        Assert.Equal(("bateman", "ScaleInitial"),
            (all[1].EquationKey, all[1].FunctionName));
        Assert.Equal(("heat-equation", "ScaleAmplitude"),
            (all[2].EquationKey, all[2].FunctionName));
    }

    [Fact]
    public async Task UpsertRecipeAsync_rejects_null()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repo.UpsertRecipeAsync(null!));
    }

    [Fact]
    public async Task UpsertRecipeAsync_rejects_blank_equation_key()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repo.UpsertRecipeAsync(new EquationFunctionRecipe
                { EquationKey = "", FunctionName = "X", RecipeJson = "{}" }));
    }

    [Fact]
    public async Task UpsertRecipeAsync_rejects_blank_function_name()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repo.UpsertRecipeAsync(new EquationFunctionRecipe
                { EquationKey = "bateman", FunctionName = "  ", RecipeJson = "{}" }));
    }
}
