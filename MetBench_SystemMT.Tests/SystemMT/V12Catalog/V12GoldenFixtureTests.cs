using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12GoldenFixtureTests
{
    [Fact]
    public void Golden_fixtures_cover_pass_fail_missing_and_invalid()
    {
        var fixtures = GoldenFixtureInventory.Load(GoldenRoot());

        Assert.Contains("pass", fixtures.Buckets);
        Assert.Contains("fail", fixtures.Buckets);
        Assert.Contains("missing", fixtures.Buckets);
        Assert.Contains("invalid", fixtures.Buckets);
        Assert.All(fixtures.Buckets.Values, files => Assert.NotEmpty(files));
    }

    private static string GoldenRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "V12Catalog", "golden");
}

internal sealed record GoldenFixtureInventory(IReadOnlyDictionary<string, IReadOnlyList<string>> Buckets)
{
    public static GoldenFixtureInventory Load(string root)
    {
        var buckets = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var bucket in new[] { "pass", "fail", "missing", "invalid" })
        {
            var path = Path.Combine(root, bucket);
            buckets[bucket] = Directory.Exists(path)
                ? Directory.GetFiles(path, "*.yaml", SearchOption.TopDirectoryOnly)
                : new List<string>();
        }

        return new GoldenFixtureInventory(buckets);
    }
}
