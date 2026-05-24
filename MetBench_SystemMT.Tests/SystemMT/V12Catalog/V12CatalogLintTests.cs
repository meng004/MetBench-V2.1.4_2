using System;
using System.IO;
using MetBench_BLL.SystemMT.V12Catalog.Lint;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12CatalogLintTests
{
    private static string Asset(string relativePath) =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "V12Catalog", relativePath);

    [Theory]
    [InlineData("legacy-kernel-code.yaml", "kernel_code")]
    [InlineData("legacy-role-bindings.yaml", "role_bindings")]
    [InlineData("legacy-projection-bindings.yaml", "projection_bindings")]
    [InlineData("legacy-assertion-name.yaml", "assertion_name")]
    public void Anti_legacy_lint_rejects_legacy_fields(string fileName, string field)
    {
        var yaml = File.ReadAllText(Asset(Path.Combine("invalid", fileName)));

        var ex = Assert.Throws<V12CatalogLintException>(() => V12CatalogSerializer.DeserializeMrSpec(yaml));

        Assert.Contains(field, ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
