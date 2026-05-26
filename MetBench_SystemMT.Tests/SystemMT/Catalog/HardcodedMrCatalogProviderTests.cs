#pragma warning disable CS0618 // tests intentionally exercise the obsolete transitional provider
using System.Linq;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog;

public sealed class HardcodedMrCatalogProviderTests
{
    private static LauncherOptions Opts() =>
        new(SutRoot: "/tmp/sut", SystemPython: "python3", OpenMocPython: "python3");

    [Fact]
    public void Load_pins_30_entries()
    {
        var p = new HardcodedMrCatalogProvider(Opts());

        var entries = p.Load();

        // Branch state after PR-A non-JSON I/O adapter synthetic _test-csv SUT (+1 MR / +1 SUT): 30 MR × 16 SUT.
        Assert.Equal(31, entries.Count);
    }

    [Fact]
    public void Load_spans_16_distinct_SUTs()
    {
        var p = new HardcodedMrCatalogProvider(Opts());

        var entries = p.Load();

        var distinctSuts = entries.Select(e => e.Mr.SutName).Distinct().ToList();
        Assert.Equal(16, distinctSuts.Count);
        Assert.Contains("openmoc", distinctSuts);
        Assert.Contains("openmc", distinctSuts);
        Assert.Contains("heat-equation", distinctSuts);
        Assert.Contains("projectile", distinctSuts);
        Assert.Contains("decay-chain", distinctSuts);
        Assert.Contains("damped-oscillator", distinctSuts);
        Assert.Contains("lotka-volterra", distinctSuts);
        Assert.Contains("subchannel-1d", distinctSuts);
        Assert.Contains("diffusion-1d", distinctSuts);
        Assert.Contains("poisson-1d", distinctSuts);
        Assert.Contains("advection-1d", distinctSuts);
        Assert.Contains("wave-1d", distinctSuts);
        Assert.Contains("burgers-1d", distinctSuts);
        Assert.Contains("scipy-ivp-lotka-volterra", distinctSuts);
        Assert.Contains("scipy-bvp-poisson-1d", distinctSuts);
        Assert.Contains("_test-csv", distinctSuts);
    }

    [Fact]
    public void Load_yields_unique_MR_ids_with_non_empty_required_fields()
    {
        var p = new HardcodedMrCatalogProvider(Opts());

        var entries = p.Load();

        var ids = entries.Select(e => e.Mr.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Mr.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.Mr.SutName));
            Assert.False(string.IsNullOrWhiteSpace(entry.AssertionTypeCode));
            Assert.False(string.IsNullOrWhiteSpace(entry.SampleCaseRelativePath));
            Assert.False(string.IsNullOrWhiteSpace(entry.RunnerScriptPath));
            Assert.False(string.IsNullOrWhiteSpace(entry.InputParserScriptPath));
            Assert.False(string.IsNullOrWhiteSpace(entry.OutputParserScriptPath));
            Assert.False(string.IsNullOrWhiteSpace(entry.PrimaryTransformationName));
        }
    }

    [Fact]
    public void Load_carries_EquationKey_for_recipe_based_MRs()
    {
        var p = new HardcodedMrCatalogProvider(Opts());

        var entries = p.Load();

        // bateman recipe: decay-chain-scale-initial uses ScaleInitial recipe in launcher.
        var bateman = entries.Single(e => e.Mr.Id == "decay-chain-scale-initial");
        Assert.Equal("bateman", bateman.EquationKey);

        // diffusion equation-key on diffusion-mesh-richardson (S8-P4).
        var diffusion = entries.Single(e => e.Mr.Id == "diffusion-mesh-richardson");
        Assert.Equal("diffusion", diffusion.EquationKey);
    }

    [Fact]
    public void SourceDescription_identifies_the_hardcoded_origin()
    {
        var p = new HardcodedMrCatalogProvider(Opts());
        Assert.Contains("Hardcoded", p.SourceDescription);
    }

    [Fact]
    public void Load_is_deterministic_across_calls()
    {
        var p = new HardcodedMrCatalogProvider(Opts());

        var first = p.Load();
        var second = p.Load();

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Mr.Id, second[i].Mr.Id);
            Assert.Equal(first[i].AssertionTypeCode, second[i].AssertionTypeCode);
            Assert.Equal(first[i].EquationKey, second[i].EquationKey);
        }
    }

    [Fact]
    public void Load_emits_specific_OpenMOC_pin_cell_MR()
    {
        // Spot-check: this MR id was present pre-S8-P1 and must survive the
        // provider extraction byte-identically.
        var p = new HardcodedMrCatalogProvider(Opts());

        var nuSigmaF = p.Load().Single(e => e.Mr.Id == "openmoc-pincell-nu-sigma-f");

        Assert.Equal("openmoc", nuSigmaF.Mr.SutName);
        // Mr.TransformationName is the UI display label; PrimaryTransformationName is the engine name.
        Assert.Equal("ScaleNuSigmaF", nuSigmaF.Mr.TransformationName);
        Assert.Equal("ScaleField", nuSigmaF.PrimaryTransformationName);
        Assert.Equal("greater", nuSigmaF.AssertionTypeCode);
        Assert.Equal("k_eff", nuSigmaF.Mr.ValueName);
    }

    [Fact]
    public void Constructor_rejects_null_options()
    {
        Assert.Throws<System.ArgumentNullException>(() => new HardcodedMrCatalogProvider(null!));
    }
}
