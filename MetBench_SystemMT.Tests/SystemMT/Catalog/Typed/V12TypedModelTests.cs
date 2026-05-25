using System.IO;
using MetBench_BLL.SystemMT.Catalog.Typed.Serialization;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class V12TypedModelTests
{
    [Fact]
    public void MrSpec_exposes_roles_projections_and_predicates()
    {
        var spec = TypedCatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.TypedMrSample));

        Assert.NotNull(spec.Roles);
        Assert.NotNull(spec.Projections);
        Assert.NotEmpty(spec.Predicates);
        Assert.Equal("Diffusion", spec.FiveDTags.EquationKey);
        Assert.Equal("P2_Mono", spec.FiveDTags.Pattern);
    }

    [Fact]
    public void PropertySpec_exposes_projections_and_assertions()
    {
        var spec = TypedCatalogSerializer.DeserializePropertySpec(File.ReadAllText(TestAssetPaths.TypedPropertySample));

        Assert.NotNull(spec.Projections);
        Assert.NotEmpty(spec.Assertions);
        Assert.Equal("Resonance", spec.FiveDTags.EquationKey);
        Assert.Equal("Deterministic", spec.FiveDTags.ProgramType);
    }

    [Fact]
    public void BinaryComparisonPredicate_roundtrips_core_fields()
    {
        var spec = TypedCatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.TypedMrSample));

        var predicate = Assert.IsType<BinaryComparisonPredicate>(Assert.Single(spec.Predicates));
        Assert.Equal("hotter-k-eff-lower", predicate.PredicateId);
        Assert.Equal("hotter", predicate.LeftRole);
        Assert.Equal("baseline", predicate.RightRole);
        Assert.Equal("k_eff", predicate.Metric);
    }
}
