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
    public void Catalog_lists_the_three_pinned_pincell_mrs()
    {
        var doc = LoadCatalog();

        Assert.Equal("openmc", doc.SutName);
        var actualIds = doc.Mrs.Select(m => m.MrId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        // Two cross-program Mono MRs (counterparts of the OpenMOC pair) plus one
        // OpenMC-specific Conv MR (Bol-Alg-02, particle-count convergence — no
        // deterministic OpenMOC counterpart, since OpenMOC has no statistical
        // sampling to refine).
        var expectedIds = new[]
        {
            "openmc-pincell-nu-sigma-f",
            "openmc-pincell-particle-count-convergence",
            "openmc-pincell-sigma-a",
        };
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public void Each_mr_carries_boltzmann_mc_semantic_tags_with_pattern_per_role()
    {
        var doc = LoadCatalog();

        var monoIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "openmc-pincell-nu-sigma-f",
            "openmc-pincell-sigma-a",
        };
        var convIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "openmc-pincell-particle-count-convergence",
        };

        foreach (var mr in doc.Mrs)
        {
            Assert.Equal("Boltzmann", mr.Equation);
            Assert.Equal("MC", mr.ProgramType);
            Assert.Equal("openmc", mr.SutName);

            var expectedPattern = monoIds.Contains(mr.MrId) ? "Mono"
                : convIds.Contains(mr.MrId) ? "Conv"
                : throw new Xunit.Sdk.XunitException(
                    $"Unexpected MR id '{mr.MrId}' — extend the Mono / Conv classification above when adding new OpenMC MRs.");
            Assert.Equal(expectedPattern, mr.MetaPattern);
        }
    }
}
