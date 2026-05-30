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
    public void Catalog_lists_the_three_pinned_pincell_mrs()
    {
        var doc = LoadCatalog();

        Assert.Equal("openmoc", doc.SutName);
        var actualIds = doc.Mrs.Select(m => m.MrId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        // Two Mono MRs (ScaleNuSigmaF / ScaleFuelSigmaA) plus one Conv MR
        // (Bol-Alg-01, angular-discretization convergence via RefineRayTracks —
        // exercised through the error-monotonic launcher pipeline wired by PR-Bol-2A #179).
        var expectedIds = new[]
        {
            "openmoc-pincell-nu-sigma-f",
            "openmoc-pincell-ray-track-convergence",
            "openmoc-pincell-sigma-a",
        };
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public void Ray_track_convergence_uses_atomic_refinement_transform_and_calibrated_phases()
    {
        var doc = LoadCatalog();

        var mr = Assert.Single(doc.Mrs, m => m.MrId == "openmoc-pincell-ray-track-convergence");
        var step = Assert.Single(mr.TransformSteps);
        Assert.Equal("RefineRayTracks", step.TransformationName);
        Assert.Equal("/tracking", step.TargetFieldPath);

        Assert.NotNull(mr.RefinementPhases);
        var phases = mr.RefinementPhases!;
        Assert.Equal(new[] { "coarse", "medium", "reference" }, phases.Select(p => p.Role).ToArray());
        Assert.Equal("1", phases[0].Parameters["factor"]);
        Assert.Equal("3", phases[1].Parameters["factor"]);
        Assert.Equal("8", phases[2].Parameters["factor"]);
    }

    [Fact]
    public void Each_mr_carries_boltzmann_num_semantic_tags_with_pattern_per_role()
    {
        var doc = LoadCatalog();

        var monoIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "openmoc-pincell-nu-sigma-f",
            "openmoc-pincell-sigma-a",
        };
        var convIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "openmoc-pincell-ray-track-convergence",
        };

        foreach (var mr in doc.Mrs)
        {
            Assert.Equal("Boltzmann", mr.Equation);
            Assert.Equal("Num", mr.ProgramType);
            Assert.Equal("openmoc", mr.SutName);

            var expectedPattern = monoIds.Contains(mr.MrId) ? "Mono"
                : convIds.Contains(mr.MrId) ? "Conv"
                : throw new Xunit.Sdk.XunitException(
                    $"Unexpected MR id '{mr.MrId}' — extend the Mono / Conv classification above when adding new OpenMOC MRs.");
            Assert.Equal(expectedPattern, mr.MetaPattern);
        }
    }
}
