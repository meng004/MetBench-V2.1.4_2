using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_DAL;
using MetBench_SystemMT.Tests.SystemMT.Catalog;
using MetBench_SystemMT.Tests.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Metadata;

/// <summary>
/// G-10: 元信息层 Delete + Recipe Update/Delete 补齐。
/// </summary>
public sealed class MetadataDeleteG10Tests : IDisposable
{
    private readonly string _dbPath;

    public MetadataDeleteG10Tests()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchG10", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _dbPath = Path.Combine(dir, "metadata.db");
    }

    public void Dispose()
    {
        foreach (var p in new[] { _dbPath, _dbPath + "-log" })
            if (File.Exists(p)) File.Delete(p);
    }

    // ── ISystemMtMetadataRepository Delete (LiteDB 实现) ─────────────────────

    [Fact]
    public async Task DeleteEquationAsync_removes_row_and_returns_true()
    {
        using var repo = new LiteDbSystemMtMetadataRepository(_dbPath);
        await repo.UpsertEquationAsync(new EquationMetadata
        {
            EquationKey = "test-eq", Name = "T", CanonicalForm = "F", SymbolSystem = "S",
        });

        var removed = await repo.DeleteEquationAsync("test-eq");

        Assert.True(removed);
        Assert.Null(await repo.GetEquationAsync("test-eq"));
    }

    [Fact]
    public async Task DeleteEquationAsync_returns_false_for_unknown_key()
    {
        using var repo = new LiteDbSystemMtMetadataRepository(_dbPath);

        Assert.False(await repo.DeleteEquationAsync("does-not-exist"));
        Assert.False(await repo.DeleteEquationAsync(""));
    }

    [Fact]
    public async Task DeleteMrAsync_removes_row_and_returns_true()
    {
        using var repo = new LiteDbSystemMtMetadataRepository(_dbPath);
        await repo.UpsertMrAsync(new MrMetadata
        {
            MrId = "test-mr", EquationKey = "test-eq",
            PhysicalMeaning = "x", InputTransformation = "y", OutputRelation = "z",
        });

        var removed = await repo.DeleteMrAsync("test-mr");

        Assert.True(removed);
        Assert.Null(await repo.GetMrAsync("test-mr"));
    }

    [Fact]
    public async Task DeleteRecipeAsync_removes_row_and_returns_true()
    {
        using var repo = new LiteDbSystemMtMetadataRepository(_dbPath);
        var recipe = new EquationFunctionRecipe
        {
            EquationKey = "test-eq",
            FunctionName = "test-fn",
            RecipeJson = """{"compose":[{"op":"MathAbs","path":"x","params":{}}]}""",
        };
        await repo.UpsertRecipeAsync(recipe);

        var removed = await repo.DeleteRecipeAsync("test-eq", "test-fn");

        Assert.True(removed);
        Assert.Null(await repo.GetRecipeAsync("test-eq", "test-fn"));
    }

    [Fact]
    public async Task DeleteRecipeAsync_returns_false_for_unknown_key()
    {
        using var repo = new LiteDbSystemMtMetadataRepository(_dbPath);

        Assert.False(await repo.DeleteRecipeAsync("nope", "nope"));
        Assert.False(await repo.DeleteRecipeAsync("", "fn"));
        Assert.False(await repo.DeleteRecipeAsync("eq", ""));
    }

    // ── SystemMtCatalogService: Recipe Update + Delete ───────────────────────

    [Fact]
    public async Task UpdateEquationFunctionAsync_replaces_existing_RecipeJson()
    {
        var apps = new LauncherCatalogV2ImporterTests.FakeImporterAppRepo();
        var mrs = new LauncherCatalogV2ImporterTests.FakeImporterMrRepo();
        var bindings = new LauncherCatalogV2ImporterTests.FakeImporterBindingRepo();
        var audit = new LauncherCatalogV2ImporterTests.FakeImporterAuditRepo();
        var meta = new P3CatalogExtensionTests.FakeMetadataRepo();
        var svc = new SystemMtCatalogService(apps, mrs, bindings, audit, meta);

        var original = new EquationFunctionRecipe
        {
            EquationKey = "bateman", FunctionName = "Fx",
            RecipeJson = """{"compose":[{"op":"ScaleField","path":"/x","params":{"factor":"2"}}]}""",
        };
        await svc.CreateEquationFunctionAsync(original);

        var updated = new EquationFunctionRecipe
        {
            EquationKey = "bateman", FunctionName = "Fx",
            RecipeJson = """{"compose":[{"op":"MathAbs","path":"/x","params":{}}]}""",
        };
        await svc.UpdateEquationFunctionAsync(updated);

        var back = await svc.GetEquationFunctionAsync("bateman", "Fx");
        Assert.NotNull(back);
        Assert.Contains("MathAbs", back!.RecipeJson);
    }

    [Fact]
    public async Task UpdateEquationFunctionAsync_throws_when_not_found()
    {
        var apps = new LauncherCatalogV2ImporterTests.FakeImporterAppRepo();
        var mrs = new LauncherCatalogV2ImporterTests.FakeImporterMrRepo();
        var bindings = new LauncherCatalogV2ImporterTests.FakeImporterBindingRepo();
        var audit = new LauncherCatalogV2ImporterTests.FakeImporterAuditRepo();
        var meta = new P3CatalogExtensionTests.FakeMetadataRepo();
        var svc = new SystemMtCatalogService(apps, mrs, bindings, audit, meta);

        var recipe = new EquationFunctionRecipe
        {
            EquationKey = "ghost", FunctionName = "Fx",
            RecipeJson = """{"compose":[{"op":"MathAbs","path":"/x","params":{}}]}""",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateEquationFunctionAsync(recipe));
    }

    [Fact]
    public async Task DeleteEquationFunctionAsync_removes_recipe_and_returns_true()
    {
        var apps = new LauncherCatalogV2ImporterTests.FakeImporterAppRepo();
        var mrs = new LauncherCatalogV2ImporterTests.FakeImporterMrRepo();
        var bindings = new LauncherCatalogV2ImporterTests.FakeImporterBindingRepo();
        var audit = new LauncherCatalogV2ImporterTests.FakeImporterAuditRepo();
        var meta = new P3CatalogExtensionTests.FakeMetadataRepo();
        var svc = new SystemMtCatalogService(apps, mrs, bindings, audit, meta);

        var recipe = new EquationFunctionRecipe
        {
            EquationKey = "bateman", FunctionName = "Fx",
            RecipeJson = """{"compose":[{"op":"MathAbs","path":"/x","params":{}}]}""",
        };
        await svc.CreateEquationFunctionAsync(recipe);

        var ok = await svc.DeleteEquationFunctionAsync("bateman", "Fx");

        Assert.True(ok);
        Assert.Null(await svc.GetEquationFunctionAsync("bateman", "Fx"));
    }

    [Fact]
    public async Task DeleteEquationFunctionAsync_returns_false_for_missing()
    {
        var apps = new LauncherCatalogV2ImporterTests.FakeImporterAppRepo();
        var mrs = new LauncherCatalogV2ImporterTests.FakeImporterMrRepo();
        var bindings = new LauncherCatalogV2ImporterTests.FakeImporterBindingRepo();
        var audit = new LauncherCatalogV2ImporterTests.FakeImporterAuditRepo();
        var meta = new P3CatalogExtensionTests.FakeMetadataRepo();
        var svc = new SystemMtCatalogService(apps, mrs, bindings, audit, meta);

        Assert.False(await svc.DeleteEquationFunctionAsync("nope", "nope"));
        Assert.False(await svc.DeleteEquationFunctionAsync("", "fn"));
    }
}
