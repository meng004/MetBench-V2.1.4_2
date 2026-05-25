using System.IO;
using Xunit;

namespace MetBench_SystemMT.Tests.Architecture;

public sealed class SemanticCatalogNamingBoundaryTests
{
    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Cannot find solution root from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Typed_semantic_catalog_uses_permanent_catalog_typed_path()
    {
        var root = FindSolutionRoot();
        Assert.True(
            Directory.Exists(Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Catalog", "Typed")),
            "Typed semantic catalog must live under SystemMT/Catalog/Typed.");
        Assert.False(
            Directory.Exists(Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "V12Catalog")),
            "SystemMT/V12Catalog is a phase name and must not remain a production path.");
    }

    [Fact]
    public void Production_and_tests_do_not_use_v12_catalog_namespace()
    {
        var root = FindSolutionRoot();
        var searchRoots = new[]
        {
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT"),
            Path.Combine(root, "MetBench_SystemMT.Tests", "SystemMT"),
        };
        var violators = searchRoots
            .Where(Directory.Exists)
            .SelectMany(path => Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(file => File.ReadAllText(file).Contains("V12Catalog", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file))
            .ToList();

        Assert.True(
            violators.Count == 0,
            "V12Catalog references remain in production/test semantic catalog code:\n" + string.Join("\n", violators));
    }
}
