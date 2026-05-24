using System.IO;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12TypedModelTests
{
    [Fact]
    public void MrSpec_exposes_roles_projections_and_predicates()
    {
        var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));

        Assert.NotNull(spec.Roles);
        Assert.NotNull(spec.Projections);
        Assert.NotEmpty(spec.Predicates);
        Assert.Equal("Diffusion", spec.FiveDTags.EquationKey);
        Assert.Equal("P2_Mono", spec.FiveDTags.Pattern);
    }

    [Fact]
    public void PropertySpec_exposes_projections_and_assertions()
    {
        var spec = V12CatalogSerializer.DeserializePropertySpec(File.ReadAllText(TestAssetPaths.V12PropertySample));

        Assert.NotNull(spec.Projections);
        Assert.NotEmpty(spec.Assertions);
        Assert.Equal("Resonance", spec.FiveDTags.EquationKey);
        Assert.Equal("Deterministic", spec.FiveDTags.ProgramType);
    }

    [Fact]
    public void BinaryComparisonPredicate_roundtrips_core_fields()
    {
        var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));

        var predicate = Assert.IsType<BinaryComparisonPredicate>(Assert.Single(spec.Predicates));
        Assert.Equal("hotter-k-eff-lower", predicate.PredicateId);
        Assert.Equal("hotter", predicate.LeftRole);
        Assert.Equal("baseline", predicate.RightRole);
        Assert.Equal("k_eff", predicate.Metric);
    }
}
