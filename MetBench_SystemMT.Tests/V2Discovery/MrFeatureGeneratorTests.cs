using MetBench_BLL.Discovery;
using MetBench_DAL.V2;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

/// <summary>
/// TDD for T-A scaffold_mr generator —— 从持久化 MetaPattern 一次生成结构正确的 .feature。
/// </summary>
public sealed class MrFeatureGeneratorTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbMetaPatternRepository _repo;
    private readonly MrFeatureGenerator _gen;

    public MrFeatureGeneratorTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(),
            "MetBenchMrGen", Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _repo = new LiteDbMetaPatternRepository(_dbPath);
        MetaPatternSeed.EnsureSeeded(_repo);
        _gen = new MrFeatureGenerator(_repo);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    [Fact]
    public void Generate_m_mono_picks_default_assertion_from_metapattern_db()
    {
        var spec = new MrFeatureSpec(
            Code: "MR-X",
            MetaPatternCode: "m_mono",
            Suts: new[] { "openmoc" },
            ParameterAbstract: "physics.fuel.nu_sigma_f");

        var content = _gen.Generate(spec);

        Assert.Contains("@metapattern:m_mono", content);
        Assert.Contains("@assertion:greater", content);              // m_mono 默认 = greater
        Assert.Contains("@value:k_eff", content);
        Assert.Contains("Feature: MR-X", content);
        Assert.Contains("physics.fuel.nu_sigma_f", content);
        Assert.Contains("Apply MR-X to <sut>", content);
    }

    [Fact]
    public void Generate_m_inv_picks_approx_invariant_default()
    {
        var content = _gen.Generate(new MrFeatureSpec(
            Code: "MR-Y", MetaPatternCode: "m_inv",
            Suts: new[] { "openmoc", "openmc" },
            ParameterAbstract: "geometry.rotation_deg"));

        Assert.Contains("@assertion:approx-invariant", content);    // m_inv 默认
        Assert.Contains("@tolerance_rel:0.0001", content);
    }

    [Fact]
    public void Generate_m_cmp_inherits_bound_and_noise_aware()
    {
        var content = _gen.Generate(new MrFeatureSpec(
            Code: "MR16-cmp", MetaPatternCode: "m_cmp",
            Suts: new[] { "openmoc", "openmc" },
            ParameterAbstract: "solver.OpenMOC_vs_OpenMC"));

        Assert.Contains("@assertion:bound", content);
        Assert.Contains("@noise_aware:true", content);
        Assert.Contains("@tolerance_rel:0.005", content);
    }

    [Fact]
    public void Generate_explicit_overrides_beat_metapattern_defaults()
    {
        var content = _gen.Generate(new MrFeatureSpec(
            Code: "MR-Z", MetaPatternCode: "m_mono",
            Suts: new[] { "openmc" },
            ParameterAbstract: "physics.fuel.sigma_a",
            Assertion: "less",            // override default 'greater'
            ToleranceRel: 0.002,
            NoiseAware: true,
            ValueName: "flux"));

        Assert.Contains("@assertion:less", content);
        Assert.Contains("@noise_aware:true", content);
        Assert.Contains("@tolerance_rel:0.002", content);
        Assert.Contains("@value:flux", content);
    }

    [Fact]
    public void Generate_uses_default_factors_when_user_supplies_none()
    {
        var content = _gen.Generate(new MrFeatureSpec(
            Code: "MR-W", MetaPatternCode: "m_mono",
            Suts: new[] { "openmoc" },
            ParameterAbstract: "physics.fuel.nu_sigma_f"));

        // 3 行 Examples（默认 factors = 0.5 / 1.0 / 1.5）
        var lines = content.Split('\n').Count(l => l.Contains("openmoc | SUT/openmoc"));
        Assert.Equal(3, lines);
    }

    [Fact]
    public void Generate_unknown_metapattern_throws_with_hint()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _gen.Generate(new MrFeatureSpec(
            Code: "MR-Bad", MetaPatternCode: "m_bogus",
            Suts: new[] { "openmoc" }, ParameterAbstract: "x.y")));
        Assert.Contains("m_bogus", ex.Message);
        Assert.Contains("Active:", ex.Message);
        Assert.Contains("m_inv", ex.Message);  // hint 应该列已 active 的
    }

    [Fact]
    public void Generate_out_of_scope_metapattern_refuses()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _gen.Generate(new MrFeatureSpec(
            Code: "MR-Adj", MetaPatternCode: "m_adj",   // seeded as out-of-scope
            Suts: new[] { "openmoc" }, ParameterAbstract: "solver.adjoint_mode")));
        Assert.Contains("out-of-scope", ex.Message);
    }

    [Fact]
    public void Generate_empty_code_throws()
    {
        Assert.Throws<InvalidOperationException>(() => _gen.Generate(new MrFeatureSpec(
            Code: "", MetaPatternCode: "m_mono",
            Suts: new[] { "openmoc" }, ParameterAbstract: "x")));
    }

    [Fact]
    public void Generate_empty_suts_throws()
    {
        Assert.Throws<InvalidOperationException>(() => _gen.Generate(new MrFeatureSpec(
            Code: "MR-X", MetaPatternCode: "m_mono",
            Suts: Array.Empty<string>(), ParameterAbstract: "x")));
    }

    [Fact]
    public void Generate_empty_parameter_abstract_throws()
    {
        Assert.Throws<InvalidOperationException>(() => _gen.Generate(new MrFeatureSpec(
            Code: "MR-X", MetaPatternCode: "m_mono",
            Suts: new[] { "openmoc" }, ParameterAbstract: "")));
    }

    [Fact]
    public void TargetRelativePath_uses_metapattern_subdir()
    {
        var spec = new MrFeatureSpec("MR-X", "m_mono", new[] { "openmoc" }, "physics.fuel.nu_sigma_f");
        Assert.Equal("metbench/catalog/features/m_mono/MR-X.feature", _gen.TargetRelativePath(spec));
    }

    [Fact]
    public void Generate_includes_hypothesis_from_metapattern()
    {
        var content = _gen.Generate(new MrFeatureSpec(
            Code: "MR-X", MetaPatternCode: "m_mono",
            Suts: new[] { "openmoc" }, ParameterAbstract: "physics.fuel.nu_sigma_f"));
        // m_mono.HypothesisTemplate 含 "monotone"
        Assert.Contains("monotone", content);
    }
}
