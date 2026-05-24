using System.IO;
using System.Text.Json.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12CatalogSemanticValidationTests
{
    [Fact]
    public void Validate_rejects_missing_role()
    {
        var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));
        var broken = spec with
        {
            Predicates = new PredicateSpec[]
            {
                new BinaryComparisonPredicate("p1", "ghost", "baseline", "k_eff", "Less")
            }
        };

        var result = broken.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("left_role"));
    }

    [Fact]
    public void Validate_rejects_missing_metric()
    {
        var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));
        var broken = spec with
        {
            Predicates = new PredicateSpec[]
            {
                new BinaryComparisonPredicate("p1", "hotter", "baseline", "ghost_metric", "Less")
            }
        };

        var result = broken.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("metric"));
    }

    [Fact]
    public void Validate_passes_for_sample_catalog_entries()
    {
        var mr = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));
        var property = V12CatalogSerializer.DeserializePropertySpec(File.ReadAllText(TestAssetPaths.V12PropertySample));
        var extraMr = mr with
        {
            MrId = "dif-phy-doppler-negative-copy",
            Predicates = new PredicateSpec[]
            {
                new BinaryComparisonPredicate("hotter-k-eff-equal", "hotter", "baseline", "k_eff", "Equal")
            }
        };

        var mrResult = mr.Validate();
        var propertyResult = property.Validate();
        var extraMrResult = extraMr.Validate();

        Assert.True(mrResult.IsValid);
        Assert.True(propertyResult.IsValid);
        Assert.True(extraMrResult.IsValid);
    }

    [Fact]
    public void Validate_rejects_unknown_mr_parameter_reference()
    {
        var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));
        var broken = spec with
        {
            Parameters = new Dictionary<string, ParameterExpression>(spec.Parameters!)
            {
                ["delta_t"] = new MrParameterRefExpression("missing_alpha")
            }
        };

        var result = broken.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("parameters.delta_t"));
    }

    [Fact]
    public void Validate_rejects_unsupported_tolerance_kind()
    {
        var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));
        var broken = spec with
        {
            DefaultTolerance = new FieldNormToleranceSpec("L2", 1e-6, 1e-4)
        };

        var result = broken.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.Contains("default_tolerance"));
    }
}
