using MetBench_BLL.SystemMT.Metadata;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Metadata;

public sealed class LiteDbSystemMtMetadataRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public LiteDbSystemMtMetadataRepositoryTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchMetadataTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile))
        {
            File.Delete(logFile);
        }
    }

    private LiteDbSystemMtMetadataRepository OpenRepo() => new(_dbPath);

    private static EquationMetadata SampleEquation() => new()
    {
        EquationKey = "bateman",
        Name = "Bateman 衰变链方程",
        CanonicalForm = "dN_i/dt = λ_{i-1}·N_{i-1} − λ_i·N_i",
        SymbolSystem = "N_i 核素 i 的数量；λ_i 核素 i 的衰变常数。",
        Parameters = new List<EquationParameter>
        {
            new() { Symbol = "N_i", Description = "核素 i 的初始原子数", Unit = "atoms" },
            new() { Symbol = "λ_i", Description = "核素 i 的衰变常数", Unit = "1/s" },
        },
    };

    [Fact]
    public async Task UpsertEquation_then_GetEquation_roundtrips_all_fields()
    {
        using (var repo = OpenRepo())
        {
            await repo.UpsertEquationAsync(SampleEquation());
        }

        using (var repo = OpenRepo())
        {
            var loaded = await repo.GetEquationAsync("bateman");

            Assert.NotNull(loaded);
            Assert.Equal("Bateman 衰变链方程", loaded!.Name);
            Assert.Equal("dN_i/dt = λ_{i-1}·N_{i-1} − λ_i·N_i", loaded.CanonicalForm);
            Assert.Equal("N_i 核素 i 的数量；λ_i 核素 i 的衰变常数。", loaded.SymbolSystem);
            Assert.Equal(2, loaded.Parameters.Count);
            Assert.Equal("λ_i", loaded.Parameters[1].Symbol);
            Assert.Equal("核素 i 的衰变常数", loaded.Parameters[1].Description);
            Assert.Equal("1/s", loaded.Parameters[1].Unit);
        }
    }

    [Fact]
    public async Task GetEquation_returns_null_for_unknown_key()
    {
        using var repo = OpenRepo();
        Assert.Null(await repo.GetEquationAsync("does-not-exist"));
    }

    [Fact]
    public async Task UpsertEquation_with_existing_key_updates_in_place()
    {
        using var repo = OpenRepo();
        await repo.UpsertEquationAsync(SampleEquation());

        var revised = SampleEquation();
        revised.Name = "Bateman 方程（修订）";
        await repo.UpsertEquationAsync(revised);

        var all = await repo.ListEquationsAsync();
        Assert.Single(all);
        Assert.Equal("Bateman 方程（修订）", all[0].Name);
    }

    [Fact]
    public async Task UpsertEquation_rejects_blank_key()
    {
        using var repo = OpenRepo();
        var bad = SampleEquation();
        bad.EquationKey = "  ";
        await Assert.ThrowsAsync<ArgumentException>(() => repo.UpsertEquationAsync(bad));
    }

    private static MrMetadata SampleMr(string mrId = "lotka-volterra-scale-gamma") => new()
    {
        MrId = mrId,
        EquationKey = "lotka-volterra",
        PhysicalMeaning =
            "由时均恒等式 ⟨prey⟩ = γ/δ，放大捕食者死亡率必然抬高时均猎物数。",
        InputTransformation = "γ → factor·γ（factor > 1）",
        OutputRelation = "mean_prey(flw) > mean_prey(src)",
        ComparisonType = MrComparisonType.Relative,
        MetaPatternRationale = "Linearity MR: scaling the source should scale the solution.",
        TransformationSemantics = "Scale source input field by factor.",
        ObservableSummary = "Compare selected scalar or field residual after source/follow-up runs.",
        PredicateSummary = "Binary comparison with configured tolerance.",
        ToleranceSummary = "Relative tolerance explains accepted numerical deviation.",
        Applicability = "Only valid when the SUT exposes the named metric.",
        FailureMeaning = "MR violation indicates inconsistent response to the declared transformation.",
        Parameters = new List<MrParameter>
        {
            new() { Symbol = "γ", PhysicalMeaning = "捕食者死亡率", ValueRange = "γ > 0" },
            new() { Symbol = "δ", PhysicalMeaning = "捕食效率系数", ValueRange = "δ > 0" },
        },
    };


    [Fact]
    public void MrMetadata_default_explanation_profile_is_empty()
    {
        var mr = new MrMetadata();

        Assert.Equal(string.Empty, mr.MetaPatternRationale);
        Assert.Equal(string.Empty, mr.TransformationSemantics);
        Assert.Equal(string.Empty, mr.ObservableSummary);
        Assert.Equal(string.Empty, mr.PredicateSummary);
        Assert.Equal(string.Empty, mr.ToleranceSummary);
        Assert.Equal(string.Empty, mr.Applicability);
        Assert.Equal(string.Empty, mr.FailureMeaning);
    }

    [Fact]
    public async Task UpsertMr_then_GetMr_roundtrips_all_fields()
    {
        using (var repo = OpenRepo())
        {
            await repo.UpsertMrAsync(SampleMr());
        }

        using (var repo = OpenRepo())
        {
            var loaded = await repo.GetMrAsync("lotka-volterra-scale-gamma");

            Assert.NotNull(loaded);
            Assert.Equal("lotka-volterra", loaded!.EquationKey);
            Assert.Equal(
                "由时均恒等式 ⟨prey⟩ = γ/δ，放大捕食者死亡率必然抬高时均猎物数。",
                loaded.PhysicalMeaning);
            Assert.Equal("γ → factor·γ（factor > 1）", loaded.InputTransformation);
            Assert.Equal("mean_prey(flw) > mean_prey(src)", loaded.OutputRelation);
            Assert.Equal(MrComparisonType.Relative, loaded.ComparisonType);
            Assert.Equal("Linearity MR: scaling the source should scale the solution.", loaded.MetaPatternRationale);
            Assert.Equal("Scale source input field by factor.", loaded.TransformationSemantics);
            Assert.Equal("Compare selected scalar or field residual after source/follow-up runs.", loaded.ObservableSummary);
            Assert.Equal("Binary comparison with configured tolerance.", loaded.PredicateSummary);
            Assert.Equal("Relative tolerance explains accepted numerical deviation.", loaded.ToleranceSummary);
            Assert.Equal("Only valid when the SUT exposes the named metric.", loaded.Applicability);
            Assert.Equal("MR violation indicates inconsistent response to the declared transformation.", loaded.FailureMeaning);
            Assert.Equal(2, loaded.Parameters.Count);
            Assert.Equal("δ", loaded.Parameters[1].Symbol);
            Assert.Equal("捕食效率系数", loaded.Parameters[1].PhysicalMeaning);
            Assert.Equal("δ > 0", loaded.Parameters[1].ValueRange);
        }
    }

    [Fact]
    public async Task GetMr_returns_null_for_unknown_id()
    {
        using var repo = OpenRepo();
        Assert.Null(await repo.GetMrAsync("does-not-exist"));
    }

    [Fact]
    public async Task UpsertMr_with_existing_id_updates_in_place()
    {
        using var repo = OpenRepo();
        await repo.UpsertMrAsync(SampleMr());

        var revised = SampleMr();
        revised.ComparisonType = MrComparisonType.Absolute;
        await repo.UpsertMrAsync(revised);

        var all = await repo.ListMrsAsync();
        Assert.Single(all);
        Assert.Equal(MrComparisonType.Absolute, all[0].ComparisonType);
    }

    [Fact]
    public async Task ListMrsByEquation_filters_to_the_requested_equation()
    {
        using var repo = OpenRepo();
        await repo.UpsertMrAsync(SampleMr("lotka-volterra-scale-gamma"));

        var foreign = SampleMr("decay-chain-scale-initial");
        foreign.EquationKey = "bateman";
        await repo.UpsertMrAsync(foreign);

        var lv = await repo.ListMrsByEquationAsync("lotka-volterra");
        Assert.Single(lv);
        Assert.Equal("lotka-volterra-scale-gamma", lv[0].MrId);

        var all = await repo.ListMrsAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpsertMr_rejects_blank_id()
    {
        using var repo = OpenRepo();
        var bad = SampleMr();
        bad.MrId = "  ";
        await Assert.ThrowsAsync<ArgumentException>(() => repo.UpsertMrAsync(bad));
    }
}
