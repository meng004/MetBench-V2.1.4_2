using System;
using System.Text.Json.Nodes;

namespace MetBench_BLL.SystemMT.V12Catalog.Schema;

public static class V12CatalogStructuralSchema
{
    public static void ValidateMr(JsonNode node)
    {
        var obj = RequireObject(node, "$");
        RequireKind(obj, "$", "MrSpec");
        RequireKindsFromDictionary(obj, "parameters", "$.parameters");
        RequireKindsFromDictionary(obj, "roles", "$.roles");
        RequireKindsFromDictionary(obj, "projections", "$.projections");
        RequireKindIfPresent(obj, "default_tolerance", "$.default_tolerance");
        RequireKindsFromArray(obj, "predicates", "$.predicates");
    }

    public static void ValidateProperty(JsonNode node)
    {
        var obj = RequireObject(node, "$");
        RequireKind(obj, "$", "PropertySpec");
        RequireKindIfPresent(obj, "case", "$.case");
        RequireKindsFromDictionary(obj, "projections", "$.projections");
        RequireKindIfPresent(obj, "default_tolerance", "$.default_tolerance");
        RequireKindsFromArray(obj, "assertions", "$.assertions");
    }

    private static JsonObject RequireObject(JsonNode? node, string path) =>
        node as JsonObject ?? throw new V12CatalogSchemaException($"{path} must be an object");

    private static void RequireKind(JsonObject obj, string path, string expectedKind)
    {
        if (!TryGetString(obj, "kind", out var kind))
            throw new V12CatalogSchemaException($"{path}.kind is required");
        if (!string.Equals(kind, expectedKind, StringComparison.Ordinal))
            throw new V12CatalogSchemaException($"{path}.kind must be '{expectedKind}', got '{kind}'");
    }

    private static void RequireKindIfPresent(JsonObject obj, string propertyName, string path)
    {
        if (obj[propertyName] is null)
            return;
        var child = RequireObject(obj[propertyName], path);
        if (!TryGetString(child, "kind", out _))
            throw new V12CatalogSchemaException($"{path}.kind is required");
    }

    private static void RequireKindsFromDictionary(JsonObject obj, string propertyName, string path)
    {
        if (obj[propertyName] is not JsonObject dict)
            return;

        foreach (var item in dict)
        {
            var child = RequireObject(item.Value, $"{path}.{item.Key}");
            if (!TryGetString(child, "kind", out _))
                throw new V12CatalogSchemaException($"{path}.{item.Key}.kind is required");
        }
    }

    private static void RequireKindsFromArray(JsonObject obj, string propertyName, string path)
    {
        if (obj[propertyName] is not JsonArray arr)
            return;

        for (var i = 0; i < arr.Count; i++)
        {
            var child = RequireObject(arr[i], $"{path}[{i}]");
            if (!TryGetString(child, "kind", out _))
                throw new V12CatalogSchemaException($"{path}[{i}].kind is required");
        }
    }

    private static bool TryGetString(JsonObject obj, string propertyName, out string? value)
    {
        value = obj[propertyName]?.GetValue<string>();
        return !string.IsNullOrWhiteSpace(value);
    }
}
