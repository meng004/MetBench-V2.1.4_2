using System.Text.Json;
using MetBench_BLL.SystemMT.Catalog;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// File-level parity test for <c>SUT/openmoc/catalog.json</c>. Pins the exact set of
/// OpenMOC single-program Boltzmann MR ids and their semantic fields
/// (Equation / MetaPattern / ProgramType). No external Python required — loads JSON only,
/// so this runs in every CI environment regardless of whether the OpenMOC venv is present.
///
/// Complements (does not replace) <see cref="Launcher.SystemMtLauncherTests"/>'s positional
/// id-order test: this guards categorical catalog content; the launcher test guards ordering.
/// </summary>
public sealed class OpenMocCatalogParityTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static SystemMtCatalogDocument LoadCatalog()
    {
        var path = Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "catalog.json");
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<SystemMtCatalogDocument>(json, JsonOpts);
        Assert.NotNull(doc);
        return doc!;
    }

    [Fact]
    public void Catalog_lists_exactly_the_two_single_program_boltzmann_mrs()
    {
        var doc = LoadCatalog();

        Assert.Equal("openmoc", doc.SutName);
        var actualIds = doc.Mrs.Select(m => m.MrId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var expectedIds = new[] { "openmoc-pincell-nu-sigma-f", "openmoc-pincell-sigma-a" };
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public void Each_mr_carries_boltzmann_mono_num_semantic_tags()
    {
        var doc = LoadCatalog();

        foreach (var mr in doc.Mrs)
        {
            Assert.Equal("Boltzmann", mr.Equation);
            Assert.Equal("Mono", mr.MetaPattern);
            Assert.Equal("Num", mr.ProgramType);
            Assert.Equal("openmoc", mr.SutName);
        }
    }
}
