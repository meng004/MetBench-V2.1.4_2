using System.Text.Json;
using MetBench_BLL.SystemMT.Catalog;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// Guards the contract that cross-program neutron-transport BDD scenarios in
/// <c>Features/CrossProgramNeutronTransportMrs.feature</c> reuse the SAME transformation
/// names declared by the single-program OpenMOC and OpenMC catalogs, rather than inventing
/// new MR semantics. Cross-program agreement is a verification dimension on top of the four
/// existing single-program MRs (openmoc-pincell-{nu-sigma-f|sigma-a},
/// openmc-pincell-{nu-sigma-f|sigma-a}); it is not a fifth or sixth MR id.
///
/// No external Python required.
/// </summary>
public sealed class CrossProgramScenarioIdReuseTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static SystemMtCatalogDocument LoadCatalog(string sutDir)
    {
        var path = Path.Combine(TestAssetPaths.AssetRoot(), sutDir, "catalog.json");
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<SystemMtCatalogDocument>(json, JsonOpts);
        Assert.NotNull(doc);
        return doc!;
    }

    private static string FeatureSource()
    {
        // The .feature file is included in the test project; locate it relative to AppContext
        // by walking up to the project root (TestAssets is under bin/Debug/net8.0/; Features
        // is at the project root). Using the source-file path is brittle across CI worktrees,
        // so prefer AppContext.BaseDirectory + relative project navigation.
        var probe = AppContext.BaseDirectory;
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(probe, "Features", "CrossProgramNeutronTransportMrs.feature");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            probe = Path.GetDirectoryName(probe)!;
            if (string.IsNullOrEmpty(probe))
                break;
        }
        throw new FileNotFoundException(
            "CrossProgramNeutronTransportMrs.feature not found from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Feature_transformations_match_single_program_catalog_transformations()
    {
        // Cross-program parity contract: OpenMOC and OpenMC must declare *the same*
        // TransformationName for the *cross-program MRs* (nu-sigma-f, sigma-a). Each catalog
        // may additionally declare program-specific MRs (e.g. OpenMC's PR-N2 / Bol-Alg-02
        // particle-count-convergence MR has no OpenMOC counterpart because OpenMOC is
        // deterministic). Compare the cross-program subset, not the whole catalog.
        var openmoc = LoadCatalog("openmoc");
        var openmc = LoadCatalog("openmc");

        // Cross-program MRs are identified by ID stem (everything after the SUT-name prefix).
        // Both catalogs must contain the same set of stems for the cross-program subset; the
        // intersection is what the cross-program BDD feature exercises.
        var openmocStems = openmoc.Mrs.Select(m => StripSutPrefix(m.MrId, "openmoc")).ToHashSet(StringComparer.Ordinal);
        var openmcStems = openmc.Mrs.Select(m => StripSutPrefix(m.MrId, "openmc")).ToHashSet(StringComparer.Ordinal);
        var crossProgramStems = openmocStems.Intersect(openmcStems, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        var openmocCrossTransformations = openmoc.Mrs
            .Where(m => crossProgramStems.Contains(StripSutPrefix(m.MrId, "openmoc")))
            .Select(m => m.TransformationName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        var openmcCrossTransformations = openmc.Mrs
            .Where(m => crossProgramStems.Contains(StripSutPrefix(m.MrId, "openmc")))
            .Select(m => m.TransformationName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(openmocCrossTransformations, openmcCrossTransformations);
        Assert.Equal(new[] { "ScaleFuelSigmaA", "ScaleNuSigmaF" }, openmocCrossTransformations);

        var feature = FeatureSource();
        Assert.Contains("\"ScaleNuSigmaF\"", feature);
        Assert.Contains("\"ScaleFuelSigmaA\"", feature);
    }

    private static string StripSutPrefix(string mrId, string sutName)
    {
        var prefix = sutName + "-";
        return mrId.StartsWith(prefix, StringComparison.Ordinal) ? mrId.Substring(prefix.Length) : mrId;
    }

    [Fact]
    public void Feature_examples_table_covers_both_openmoc_and_openmc_solvers()
    {
        // Sanity guard against silent removal of one solver row from the Examples table.
        var feature = FeatureSource();
        Assert.Contains("| openmoc | openmoc |", feature);
        Assert.Contains("| openmc  | openmc  |", feature);
    }
}
