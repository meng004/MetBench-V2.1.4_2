#pragma warning disable CS0618 // parity test intentionally compares against the obsolete hardcoded provider
using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog;

/// <summary>
/// Pin the equivalence between the legacy hardcoded catalog and the manifest-backed catalog
/// for every MR currently in the launcher (25 entries / 13 SUTs after T3 Poisson 1D + Advection 1D + Wave 1D + Burgers 1D).
/// Any drift in either side fails CI loudly.
/// </summary>
public sealed class CatalogParityTests
{
    private static LauncherOptions Opts() => new(
        SutRoot: TestAssetPaths.AssetRoot(),
        SystemPython: "python3",
        OpenMocPython: "python3");

    private static List<MrCatalogEntry> SortById(IEnumerable<MrCatalogEntry> entries) =>
        entries.OrderBy(e => e.Mr.Id, System.StringComparer.Ordinal).ToList();

    [Fact]
    public void Hardcoded_and_manifest_providers_emit_same_entry_count_and_set_of_MR_ids()
    {
        var hardcoded = new HardcodedMrCatalogProvider(Opts()).Load();
        var manifest = new ManifestMrCatalogProvider(Opts()).Load();

        Assert.Equal(hardcoded.Count, manifest.Count);
        Assert.Equal(25, hardcoded.Count);

        var hSet = hardcoded.Select(e => e.Mr.Id).OrderBy(s => s).ToList();
        var mSet = manifest.Select(e => e.Mr.Id).OrderBy(s => s).ToList();
        Assert.Equal(hSet, mSet);
    }

    [Fact]
    public void Hardcoded_and_manifest_providers_agree_field_by_field()
    {
        var hardcoded = SortById(new HardcodedMrCatalogProvider(Opts()).Load());
        var manifest = SortById(new ManifestMrCatalogProvider(Opts()).Load());

        Assert.Equal(hardcoded.Count, manifest.Count);

        for (var i = 0; i < hardcoded.Count; i++)
        {
            var h = hardcoded[i];
            var m = manifest[i];

            // MR summary
            Assert.Equal(h.Mr.Id, m.Mr.Id);
            Assert.Equal(h.Mr.SutName, m.Mr.SutName);
            Assert.Equal(h.Mr.DisplayName, m.Mr.DisplayName);
            Assert.Equal(h.Mr.Description, m.Mr.Description);
            Assert.Equal(h.Mr.TransformationName, m.Mr.TransformationName);
            Assert.Equal(h.Mr.AssertionName, m.Mr.AssertionName);
            Assert.Equal(h.Mr.ValueName, m.Mr.ValueName);
            Assert.Equal(h.Mr.MrFamily, m.Mr.MrFamily);
            Assert.Equal(
                h.Mr.DefaultParameters.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"),
                m.Mr.DefaultParameters.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));

            // Catalog entry fields (the 8-field export shape)
            Assert.Equal(h.SampleCaseRelativePath, m.SampleCaseRelativePath);
            Assert.Equal(h.RunnerScriptPath, m.RunnerScriptPath);
            Assert.Equal(h.InputParserScriptPath, m.InputParserScriptPath);
            Assert.Equal(h.OutputParserScriptPath, m.OutputParserScriptPath);
            Assert.Equal(h.AssertionTypeCode, m.AssertionTypeCode);
            Assert.Equal(h.PrimaryTransformationName, m.PrimaryTransformationName);
            Assert.Equal(h.EquationKey, m.EquationKey);
        }
    }
}
