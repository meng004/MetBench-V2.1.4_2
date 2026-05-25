using System.IO;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12CatalogMigrationTests
{
    [Fact]
    public void All_explicit_44_mr_and_4_property_entries_deserialize_and_validate()
    {
        var report = MigrationLoader.LoadAll(MigrationRoot());

        Assert.Equal(44, report.ValidMrSpecs);
        Assert.Equal(4, report.ValidPropertySpecs);
    }

    private static string MigrationRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "V12Catalog", "migration");
}

internal static class MigrationLoader
{
    public static MigrationLoadReport LoadAll(string root)
    {
        var validMrSpecs = 0;
        var validPropertySpecs = 0;
        var totalMrSpecs = 0;
        var totalPropertySpecs = 0;

        if (!Directory.Exists(root))
        {
            return new MigrationLoadReport(validMrSpecs, validPropertySpecs, totalMrSpecs, totalPropertySpecs);
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.yaml", SearchOption.AllDirectories))
        {
            var yaml = File.ReadAllText(file);
            foreach (var document in SplitDocuments(yaml))
            {
                if (document.Contains("kind: MrSpec", System.StringComparison.Ordinal))
                {
                    totalMrSpecs++;
                    var spec = V12CatalogSerializer.DeserializeMrSpec(document);
                    if (spec.Validate().IsValid)
                    {
                        validMrSpecs++;
                    }
                }
                else if (document.Contains("kind: PropertySpec", System.StringComparison.Ordinal))
                {
                    totalPropertySpecs++;
                    var spec = V12CatalogSerializer.DeserializePropertySpec(document);
                    if (spec.Validate().IsValid)
                    {
                        validPropertySpecs++;
                    }
                }
            }
        }

        return new MigrationLoadReport(validMrSpecs, validPropertySpecs, totalMrSpecs, totalPropertySpecs);
    }

    private static System.Collections.Generic.IEnumerable<string> SplitDocuments(string yaml)
    {
        var normalized = yaml.Replace("\r\n", "\n");
        foreach (var chunk in normalized.Split("\n---\n", System.StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = chunk.Trim();
            if (trimmed.Length > 0)
            {
                yield return trimmed;
            }
        }
    }
}

internal sealed record MigrationLoadReport(
    int ValidMrSpecs,
    int ValidPropertySpecs,
    int TotalMrSpecs,
    int TotalPropertySpecs);
