using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MetBench_BLL.SystemMT.Catalog.Typed.Lint;
using MetBench_BLL.SystemMT.Catalog.Typed.Schema;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using YamlDotNet.RepresentationModel;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Serialization;

public static class TypedCatalogSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static MrSpec DeserializeMrSpec(string yaml)
    {
        var node = ParseYaml(yaml);
        TypedCatalogAntiLegacyLinter.Validate(node);
        TypedCatalogStructuralSchema.ValidateMr(node);
        return node.Deserialize<MrSpec>(JsonOptions)
            ?? throw new TypedCatalogSchemaException("MR YAML deserialized to null");
    }

    public static PropertySpec DeserializePropertySpec(string yaml)
    {
        var node = ParseYaml(yaml);
        TypedCatalogAntiLegacyLinter.Validate(node);
        TypedCatalogStructuralSchema.ValidateProperty(node);
        return node.Deserialize<PropertySpec>(JsonOptions)
            ?? throw new TypedCatalogSchemaException("Property YAML deserialized to null");
    }

    private static JsonNode ParseYaml(string yaml)
    {
        using var reader = new StringReader(yaml);
        var stream = new YamlStream();
        stream.Load(reader);

        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is null)
            throw new TypedCatalogSchemaException("YAML document is empty");

        return ConvertYamlNode(stream.Documents[0].RootNode);
    }

    private static JsonNode ConvertYamlNode(YamlNode node) =>
        node switch
        {
            YamlScalarNode scalar => ConvertScalar(scalar),
            YamlSequenceNode sequence => ConvertSequence(sequence),
            YamlMappingNode mapping => ConvertMapping(mapping),
            _ => throw new TypedCatalogSchemaException($"Unsupported YAML node type: {node.NodeType}")
        };

    private static JsonNode ConvertScalar(YamlScalarNode scalar)
    {
        if (scalar.Value is null)
            return null!;
        if (bool.TryParse(scalar.Value, out var b))
            return JsonValue.Create(b)!;
        if (double.TryParse(scalar.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return JsonValue.Create(d)!;
        return JsonValue.Create(scalar.Value)!;
    }

    private static JsonNode ConvertSequence(YamlSequenceNode sequence)
    {
        var array = new JsonArray();
        foreach (var child in sequence.Children)
            array.Add(ConvertYamlNode(child));
        return array;
    }

    private static JsonNode ConvertMapping(YamlMappingNode mapping)
    {
        var obj = new JsonObject();
        foreach (var entry in mapping.Children)
        {
            var key = ((YamlScalarNode)entry.Key).Value
                ?? throw new TypedCatalogSchemaException("YAML mapping key cannot be null");
            obj[key] = ConvertYamlNode(entry.Value);
        }

        return obj;
    }
}
