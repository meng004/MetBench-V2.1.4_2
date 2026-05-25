using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using MetBench_BLL.SystemMT.V12Catalog.Validation;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12GoldenFixtureTests
{
    [Fact]
    public void Golden_fixtures_cover_pass_fail_missing_and_invalid()
    {
        var report = GoldenFixtureVerifier.VerifyAll(GoldenRoot());

        Assert.Equal(1, report.PassedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(1, report.SkippedMissingCount);
        Assert.Equal(1, report.InvalidCount);
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

internal sealed record GoldenFixtureVerificationReport(
    int RunnableFixtureCount,
    int ExecutedFixtureCount,
    int PassedCount,
    int FailedCount,
    int SkippedMissingCount,
    int InvalidCount)
{
    public int BucketCount => 4;
}

internal static class GoldenFixtureVerifier
{
    public static GoldenFixtureVerificationReport VerifyAll(string root)
    {
        var inventory = GoldenFixtureInventory.Load(root);
        var passed = 0;
        var failed = 0;
        var skippedMissing = 0;
        var invalid = 0;
        var executed = 0;
        var runnable = 0;

        foreach (var file in inventory.Buckets["pass"])
        {
            runnable++;
            executed++;
            var result = ExecuteMrFixture(file);
            Assert.Equal(VerifyStatus.Passed, result.Status);
            passed++;
        }

        foreach (var file in inventory.Buckets["fail"])
        {
            runnable++;
            executed++;
            var result = ExecuteMrFixture(file);
            Assert.Equal(VerifyStatus.Failed, result.Status);
            failed++;
        }

        foreach (var file in inventory.Buckets["missing"])
        {
            runnable++;
            executed++;
            var result = ExecuteMrFixture(file);
            Assert.Equal(VerifyStatus.SkippedMissingObservable, result.Status);
            skippedMissing++;
        }

        foreach (var file in inventory.Buckets["invalid"])
        {
            Assert.ThrowsAny<System.Exception>(() => V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(file)));
            invalid++;
        }

        return new GoldenFixtureVerificationReport(runnable, executed, passed, failed, skippedMissing, invalid);
    }

    private static VerificationResult ExecuteMrFixture(string file)
    {
        var yaml = File.ReadAllText(file);
        var spec = V12CatalogSerializer.DeserializeMrSpec(yaml);
        Assert.True(spec.Validate().IsValid);
        var context = BuildContext(Path.GetFileName(file), spec);
        return new PredicateDispatcher().Dispatch(spec.Predicates.Single(), context);
    }

    private static VerificationContext BuildContext(string fileName, MrSpec spec) =>
        fileName switch
        {
            "dif-phy-01-pass.yaml" => new VerificationContext(
                spec,
                new Dictionary<string, RoleOutput>
                {
                    ["low_boron"] = ScalarRole("low_boron", "k_eff", 1.0100),
                    ["high_boron"] = ScalarRole("high_boron", "k_eff", 0.9950),
                }),
            "cpl-app-06-fail.yaml" => new VerificationContext(
                spec,
                new Dictionary<string, RoleOutput>
                {
                    ["low_power"] = ScalarRole("low_power", "k_eff", 1.0000),
                    ["high_power"] = ScalarRole("high_power", "k_eff", 1.0020),
                }),
            "dif-phy-09-missing.yaml" => new VerificationContext(
                spec,
                new Dictionary<string, RoleOutput>
                {
                    ["h0"] = ScalarRole("h0", "diff_worth", 1.0),
                    ["h1"] = ScalarRole("h1", "diff_worth", 2.0),
                    ["h2"] = new("h2", new Dictionary<string, double>()),
                }),
            _ => throw new InvalidOperationException($"Unsupported golden fixture '{fileName}'.")
        };

    private static RoleOutput ScalarRole(string role, string metric, double value) =>
        new(role, new Dictionary<string, double> { [metric] = value });
}
