using System.IO;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class V12CoverageGateTests
{
    [Fact]
    public void Coverage_gate_matches_explicit_inventory_and_tracks_runnable_fixture_execution()
    {
        var report = V12CoverageReport.Build(MigrationRoot(), GoldenRoot());

        Assert.Equal(44, report.MrCount);
        Assert.Equal(4, report.PropertyCount);
        Assert.Equal(0, report.InvalidMigrationMrSpecs);
        Assert.Equal(0, report.InvalidMigrationPropertySpecs);
        Assert.Equal(3, report.RunnableFixtureCount);
        Assert.Equal(3, report.ExecutedFixtureCount);
        Assert.Equal(1, report.GoldenPassedCount);
        Assert.Equal(1, report.GoldenFailedCount);
        Assert.Equal(1, report.GoldenMissingCount);
        Assert.Equal(1, report.GoldenInvalidCount);
    }

    private static string MigrationRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "SystemMT", "Catalog", "Typed", "migration");

    private static string GoldenRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "SystemMT", "Catalog", "Typed", "golden");
}

internal sealed record V12CoverageReport(
    int MrCount,
    int PropertyCount,
    int InvalidMigrationMrSpecs,
    int InvalidMigrationPropertySpecs,
    int RunnableFixtureCount,
    int ExecutedFixtureCount,
    int GoldenPassedCount,
    int GoldenFailedCount,
    int GoldenMissingCount,
    int GoldenInvalidCount)
{
    public static V12CoverageReport Build(string migrationRoot, string goldenRoot)
    {
        var migration = MigrationLoader.LoadAll(migrationRoot);
        var golden = GoldenFixtureVerifier.VerifyAll(goldenRoot);

        return new V12CoverageReport(
            migration.TotalMrSpecs,
            migration.TotalPropertySpecs,
            migration.TotalMrSpecs - migration.ValidMrSpecs,
            migration.TotalPropertySpecs - migration.ValidPropertySpecs,
            golden.RunnableFixtureCount,
            golden.ExecutedFixtureCount,
            golden.PassedCount,
            golden.FailedCount,
            golden.SkippedMissingCount,
            golden.InvalidCount);
    }
}
