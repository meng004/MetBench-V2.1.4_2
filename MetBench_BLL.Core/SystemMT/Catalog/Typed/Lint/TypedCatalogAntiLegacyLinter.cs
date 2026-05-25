using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Lint;

public static class TypedCatalogAntiLegacyLinter
{
    private static readonly HashSet<string> ForbiddenFields = new(StringComparer.Ordinal)
    {
        "kernel_code",
        "role_bindings",
        "projection_bindings",
        "assertion_name",
    };

    public static void Validate(JsonNode node)
    {
        Visit(node, "$");
    }

    private static void Visit(JsonNode? node, string path)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var kvp in obj)
                {
                    if (ForbiddenFields.Contains(kvp.Key))
                        throw new TypedCatalogLintException($"{path}.{kvp.Key} is forbidden in v1.2 typed catalog");
                    Visit(kvp.Value, $"{path}.{kvp.Key}");
                }
                break;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                    Visit(arr[i], $"{path}[{i}]");
                break;
        }
    }
}
