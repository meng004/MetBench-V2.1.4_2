using System.IO;
using System.Text;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using Xunit;
using YamlDotNet.RepresentationModel;

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
            foreach (var document in EnumerateDocuments(yaml))
            {
                var kind = ReadRootKind(document);
                if (string.Equals(kind, "MrSpec", System.StringComparison.Ordinal))
                {
                    totalMrSpecs++;
                    var spec = V12CatalogSerializer.DeserializeMrSpec(document);
                    if (spec.Validate().IsValid)
                    {
                        validMrSpecs++;
                    }
                }
                else if (string.Equals(kind, "PropertySpec", System.StringComparison.Ordinal))
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

    private static System.Collections.Generic.IEnumerable<string> EnumerateDocuments(string yaml)
    {
        using var reader = new StringReader(yaml);
        var stream = new YamlStream();
        stream.Load(reader);

        foreach (var document in stream.Documents)
        {
            var writer = new StringWriter(new StringBuilder());
            var single = new YamlStream(document);
            single.Save(writer, false);
            var serialized = writer.ToString().Trim();
            if (serialized.Length > 0)
                yield return serialized;
        }
    }

    private static string? ReadRootKind(string yaml)
    {
        using var reader = new StringReader(yaml);
        var stream = new YamlStream();
        stream.Load(reader);

        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
            return null;

        foreach (var entry in mapping.Children)
        {
            if (entry.Key is YamlScalarNode key &&
                string.Equals(key.Value, "kind", System.StringComparison.Ordinal) &&
                entry.Value is YamlScalarNode value)
            {
                return value.Value;
            }
        }

        return null;
    }
}

internal sealed record MigrationLoadReport(
    int ValidMrSpecs,
    int ValidPropertySpecs,
    int TotalMrSpecs,
    int TotalPropertySpecs);
