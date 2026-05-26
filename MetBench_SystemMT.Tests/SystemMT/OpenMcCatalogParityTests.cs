using System.Text.Json;
using MetBench_BLL.SystemMT.Catalog;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// File-level parity test for <c>SUT/openmc/catalog.json</c>. Mirror of
/// <see cref="OpenMocCatalogParityTests"/>; pins the exact set of OpenMC single-program
/// Boltzmann MR ids and their semantic fields. Asserts <c>ProgramType==MC</c> (Monte
/// Carlo counterpart of OpenMOC's <c>Num</c>). No external Python required.
/// </summary>
public sealed class OpenMcCatalogParityTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static SystemMtCatalogDocument LoadCatalog()
    {
        var path = Path.Combine(TestAssetPaths.AssetRoot(), "openmc", "catalog.json");
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<SystemMtCatalogDocument>(json, JsonOpts);
        Assert.NotNull(doc);
        return doc!;
    }

    [Fact]
    public void Catalog_lists_exactly_the_two_single_program_boltzmann_mrs()
    {
        var doc = LoadCatalog();

        Assert.Equal("openmc", doc.SutName);
        var actualIds = doc.Mrs.Select(m => m.MrId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var expectedIds = new[] { "openmc-pincell-nu-sigma-f", "openmc-pincell-sigma-a" };
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public void Each_mr_carries_boltzmann_mono_mc_semantic_tags()
    {
        var doc = LoadCatalog();

        foreach (var mr in doc.Mrs)
        {
            Assert.Equal("Boltzmann", mr.Equation);
            Assert.Equal("Mono", mr.MetaPattern);
            Assert.Equal("MC", mr.ProgramType);
            Assert.Equal("openmc", mr.SutName);
        }
    }
}
