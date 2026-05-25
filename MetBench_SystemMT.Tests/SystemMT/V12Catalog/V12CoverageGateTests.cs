using System.IO;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12CoverageGateTests
{
    [Fact]
    public void Coverage_gate_matches_explicit_inventory_and_has_no_invalid_migration_specs()
    {
        var report = V12CoverageReport.Build(MigrationRoot(), GoldenRoot());

        Assert.Equal(44, report.MrCount);
        Assert.Equal(4, report.PropertyCount);
        Assert.Equal(0, report.InvalidMigrationMrSpecs);
        Assert.Equal(0, report.InvalidMigrationPropertySpecs);
        Assert.Contains("pass", report.GoldenBuckets);
        Assert.Contains("fail", report.GoldenBuckets);
        Assert.Contains("missing", report.GoldenBuckets);
        Assert.Contains("invalid", report.GoldenBuckets);
    }

    private static string MigrationRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "V12Catalog", "migration");

    private static string GoldenRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "V12Catalog", "golden");
}

internal sealed record V12CoverageReport(
    int MrCount,
    int PropertyCount,
    int InvalidMigrationMrSpecs,
    int InvalidMigrationPropertySpecs,
    string[] GoldenBuckets)
{
    public static V12CoverageReport Build(string migrationRoot, string goldenRoot)
    {
        var migration = MigrationLoader.LoadAll(migrationRoot);
        var golden = GoldenFixtureInventory.Load(goldenRoot);

        return new V12CoverageReport(
            migration.TotalMrSpecs,
            migration.TotalPropertySpecs,
            migration.TotalMrSpecs - migration.ValidMrSpecs,
            migration.TotalPropertySpecs - migration.ValidPropertySpecs,
            golden.Buckets.Keys.OrderBy(x => x).ToArray());
    }
}
