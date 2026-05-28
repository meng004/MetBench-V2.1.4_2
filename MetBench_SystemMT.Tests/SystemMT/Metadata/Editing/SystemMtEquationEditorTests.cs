using MetBench_BLL.SystemMT.Metadata;
using MetBench_BLL.SystemMT.Metadata.Editing;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Metadata.Editing;

public sealed class SystemMtEquationEditorTests
{
    private static readonly IReadOnlyList<EquationMetadata> SeedFixture = new[]
    {
        new EquationMetadata
        {
            EquationKey = "fourier",
            Name = "Fourier Heat",
            CanonicalForm = "u_t = a * u_xx",
            SymbolSystem = "u, a, x, t",
        },
        new EquationMetadata
        {
            EquationKey = "boltzmann",
            Name = "Boltzmann Transport",
            CanonicalForm = "Ω·∇ψ + Σ_t·ψ = ...",
            SymbolSystem = "ψ, Σ, k_eff",
        },
    };

    [Fact]
    public async Task ListEquationsAsync_returns_seed_merged_with_user_rows_sorted_by_key()
    {
        var repo = new FakeMetadataRepository();
        await repo.UpsertEquationAsync(new EquationMetadata
        {
            EquationKey = "user-eq",
            Name = "User Equation",
            CanonicalForm = "f(x) = 0",
        });
        var editor = new SystemMtEquationEditor(repo, SeedFixture);

        var items = await editor.ListEquationsAsync();

        Assert.Equal(new[] { "boltzmann", "fourier", "user-eq" }, items.Select(i => i.EquationKey).ToArray());
        Assert.Equal(EquationSourceKinds.BuiltIn, items[0].Source);
        Assert.Equal(EquationSourceKinds.BuiltIn, items[1].Source);
        Assert.Equal(EquationSourceKinds.User, items[2].Source);
    }

    [Fact]
    public void ValidateDraft_rejects_blank_equation_key_and_blank_name()
    {
        var editor = new SystemMtEquationEditor(new FakeMetadataRepository(), SeedFixture);

        var result = editor.ValidateDraft(new EquationMetadataDraft { EquationKey = "", Name = "" });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("EquationKey", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => e.Contains("Name", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("fourier")]
    [InlineData("Fourier")]
    [InlineData("FOURIER")]
    [InlineData("BoLtZmAnN")]
    public void ValidateDraft_rejects_keys_that_collide_with_seed_case_insensitive(string conflictingKey)
    {
        var editor = new SystemMtEquationEditor(new FakeMetadataRepository(), SeedFixture);

        var result = editor.ValidateDraft(new EquationMetadataDraft
        {
            EquationKey = conflictingKey,
            Name = "Override Attempt",
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Built-in", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LoadAsync_returns_null_for_built_in_key_even_when_user_row_exists_with_same_key()
    {
        // Built-in shadows user: editor never exposes a Built-in key as editable, even if a
        // colliding LiteDB row managed to land (defensive guard mirroring the validator).
        var repo = new FakeMetadataRepository();
        await repo.UpsertEquationAsync(new EquationMetadata
        {
            EquationKey = "fourier",
            Name = "User-injected Fourier (should be invisible)",
        });
        var editor = new SystemMtEquationEditor(repo, SeedFixture);

        var loaded = await editor.LoadAsync("fourier");

        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveDraftAsync_upserts_user_equation_when_validation_passes()
    {
        var repo = new FakeMetadataRepository();
        var editor = new SystemMtEquationEditor(repo, SeedFixture);

        var result = await editor.SaveDraftAsync(new EquationMetadataDraft
        {
            EquationKey = "new-user-equation",
            Name = "New User Equation",
            CanonicalForm = "x = 0",
        });

        Assert.True(result.Success, string.Join(" | ", result.Errors));
        var fetched = await repo.GetEquationAsync("new-user-equation");
        Assert.NotNull(fetched);
        Assert.Equal("New User Equation", fetched!.Name);
    }

    [Fact]
    public async Task DeleteAsync_blocks_when_mr_metadata_references_the_equation()
    {
        var repo = new FakeMetadataRepository();
        await repo.UpsertEquationAsync(new EquationMetadata { EquationKey = "user-eq", Name = "User Eq" });
        await repo.UpsertMrAsync(new MrMetadata { MrId = "mr-1", EquationKey = "user-eq" });
        var editor = new SystemMtEquationEditor(repo, SeedFixture);

        var result = await editor.DeleteAsync("user-eq");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("referenced", StringComparison.OrdinalIgnoreCase));
        // Row not deleted
        Assert.NotNull(await repo.GetEquationAsync("user-eq"));
    }

    [Fact]
    public async Task DeleteAsync_rejects_built_in_equation_key()
    {
        var editor = new SystemMtEquationEditor(new FakeMetadataRepository(), SeedFixture);

        var result = await editor.DeleteAsync("fourier");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Built-in", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteAsync_returns_failure_when_equation_key_not_in_repository()
    {
        var editor = new SystemMtEquationEditor(new FakeMetadataRepository(), SeedFixture);

        var result = await editor.DeleteAsync("ghost-equation");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeMetadataRepository : ISystemMtMetadataRepository
    {
        private readonly Dictionary<string, EquationMetadata> _equations = new(StringComparer.Ordinal);
        private readonly Dictionary<string, MrMetadata> _mrs = new(StringComparer.Ordinal);
        private readonly Dictionary<(string, string), EquationFunctionRecipe> _recipes = new();

        public Task UpsertEquationAsync(EquationMetadata equation, CancellationToken cancellationToken = default)
        {
            _equations[equation.EquationKey] = equation;
            return Task.CompletedTask;
        }

        public Task<EquationMetadata?> GetEquationAsync(string equationKey, CancellationToken cancellationToken = default)
        {
            _equations.TryGetValue(equationKey, out var found);
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<EquationMetadata>> ListEquationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EquationMetadata>>(_equations.Values.ToList());

        public Task UpsertMrAsync(MrMetadata mr, CancellationToken cancellationToken = default)
        {
            _mrs[mr.MrId] = mr;
            return Task.CompletedTask;
        }

        public Task<MrMetadata?> GetMrAsync(string mrId, CancellationToken cancellationToken = default)
        {
            _mrs.TryGetValue(mrId, out var found);
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<MrMetadata>> ListMrsByEquationAsync(string equationKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MrMetadata>>(
                _mrs.Values.Where(m => m.EquationKey == equationKey).ToList());

        public Task<IReadOnlyList<MrMetadata>> ListMrsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MrMetadata>>(_mrs.Values.ToList());

        public Task UpsertRecipeAsync(EquationFunctionRecipe recipe, CancellationToken cancellationToken = default)
        {
            _recipes[(recipe.EquationKey, recipe.FunctionName)] = recipe;
            return Task.CompletedTask;
        }

        public Task<EquationFunctionRecipe?> GetRecipeAsync(string equationKey, string functionName, CancellationToken cancellationToken = default)
        {
            _recipes.TryGetValue((equationKey, functionName), out var found);
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<EquationFunctionRecipe>> ListRecipesByEquationAsync(string equationKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EquationFunctionRecipe>>(
                _recipes.Where(kv => kv.Key.Item1 == equationKey).Select(kv => kv.Value).ToList());

        public Task<IReadOnlyList<EquationFunctionRecipe>> ListRecipesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EquationFunctionRecipe>>(_recipes.Values.ToList());

        public Task<bool> DeleteEquationAsync(string equationKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_equations.Remove(equationKey));

        public Task<bool> DeleteMrAsync(string mrId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_mrs.Remove(mrId));

        public Task<bool> DeleteRecipeAsync(string equationKey, string functionName, CancellationToken cancellationToken = default) =>
            Task.FromResult(_recipes.Remove((equationKey, functionName)));
    }
}
