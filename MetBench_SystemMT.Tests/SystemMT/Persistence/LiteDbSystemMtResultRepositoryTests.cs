using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

public sealed class LiteDbSystemMtResultRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public LiteDbSystemMtResultRepositoryTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchLiteDbTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile))
        {
            File.Delete(logFile);
        }
    }

    private static SystemMtResult MakeResult(
        bool passed = true,
        string assertionName = "GreaterThan",
        string valueName = "k_eff",
        double sourceValue = 1.13,
        double followUpValue = 1.51,
        InputGenerationResult? inputGeneration = null)
    {
        var sourceRun = new CliRunResult(
            "source", 0, "stdout-source", string.Empty,
            TimeSpan.FromSeconds(2.5), "/tmp/source/output.json", true, string.Empty);
        var followUpRun = new CliRunResult(
            "follow-up", 0, "stdout-followup", string.Empty,
            TimeSpan.FromSeconds(2.7), "/tmp/followup/output.json", true, string.Empty);
        var sourceOutput = new ParsedOutput(
            new Dictionary<string, double> { ["k_eff"] = sourceValue, ["iterations"] = 553, ["converged"] = 1.0 },
            new Dictionary<string, string> { ["program"] = "openmoc" });
        var followUpOutput = new ParsedOutput(
            new Dictionary<string, double> { ["k_eff"] = followUpValue, ["iterations"] = 464, ["converged"] = 1.0 },
            new Dictionary<string, string> { ["program"] = "openmoc" });
        var assertion = new SystemMtAssertionResult(
            assertionName, valueName, sourceValue, followUpValue, passed,
            passed ? string.Empty : "MR violated");
        return new SystemMtResult(
            sourceRun, followUpRun, sourceOutput, followUpOutput, assertion,
            passed, passed ? string.Empty : "MR violated", inputGeneration);
    }

    [Fact]
    public async Task SaveAsync_assigns_id_and_timestamp_persists_summary_fields()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        var before = DateTimeOffset.UtcNow;

        var id = await repo.SaveAsync("OpenMocPinCellNuSigmaF", MakeResult());

        var after = DateTimeOffset.UtcNow;
        Assert.False(string.IsNullOrEmpty(id));

        var record = await repo.GetAsync(id);
        Assert.NotNull(record);
        Assert.Equal(id, record!.Id);
        Assert.InRange(record.RunAt, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Equal("OpenMocPinCellNuSigmaF", record.ScenarioName);
        Assert.Equal("GreaterThan", record.AssertionName);
        Assert.Equal("k_eff", record.ValueName);
        Assert.Equal(1.13, record.SourceValue);
        Assert.Equal(1.51, record.FollowUpValue);
        Assert.True(record.Passed);
        Assert.Equal("source", record.SourceCaseName);
        Assert.Equal("follow-up", record.FollowUpCaseName);
        Assert.Equal(0, record.SourceExitCode);
        Assert.Equal(553, record.SourceMetrics["iterations"]);
        Assert.Equal(464, record.FollowUpMetrics["iterations"]);
    }

    [Fact]
    public async Task SaveAsync_persists_input_generation_when_present()
    {
        var transformation = new MrTransformation(
            "ScaleFuelSigmaA",
            new Dictionary<string, string> { ["factor"] = "1.5" });
        var generation = new InputGenerationResult(
            "/src.json", "/follow.json", transformation, true, "Scaled by 1.5", string.Empty);
        var result = MakeResult(
            passed: true,
            assertionName: "LessThan",
            valueName: "k_eff",
            sourceValue: 1.13,
            followUpValue: 0.81,
            inputGeneration: generation);

        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        var id = await repo.SaveAsync("OpenMocPinCellSigmaA", result);

        var record = await repo.GetAsync(id);
        Assert.NotNull(record);
        Assert.Equal("ScaleFuelSigmaA", record!.TransformationName);
        Assert.Equal("1.5", record.TransformationParameters!["factor"]);
        Assert.True(record.InputGenerationSucceeded);
        Assert.Equal("Scaled by 1.5", record.InputGenerationLog);
    }

    [Fact]
    public async Task SaveAsync_omits_input_generation_when_absent()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        var id = await repo.SaveAsync("Stage1Scenario", MakeResult());

        var record = await repo.GetAsync(id);
        Assert.NotNull(record);
        Assert.Null(record!.TransformationName);
        Assert.Null(record.TransformationParameters);
        Assert.Null(record.InputGenerationSucceeded);
        Assert.Null(record.InputGenerationLog);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);

        var record = await repo.GetAsync("507f1f77bcf86cd799439011");

        Assert.Null(record);
    }

    [Fact]
    public async Task ListRecentAsync_returns_records_in_descending_run_at_order()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        var firstId = await repo.SaveAsync("ScenarioA", MakeResult(sourceValue: 1.0, followUpValue: 1.5));
        await Task.Delay(20);
        var secondId = await repo.SaveAsync("ScenarioB", MakeResult(sourceValue: 2.0, followUpValue: 3.0));
        await Task.Delay(20);
        var thirdId = await repo.SaveAsync("ScenarioC", MakeResult(sourceValue: 3.0, followUpValue: 4.5));

        var recent = await repo.ListRecentAsync();

        Assert.Equal(3, recent.Count);
        Assert.Equal(thirdId, recent[0].Id);
        Assert.Equal(secondId, recent[1].Id);
        Assert.Equal(firstId, recent[2].Id);
    }

    [Fact]
    public async Task ListRecentAsync_respects_limit()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        for (var i = 0; i < 5; i++)
        {
            await repo.SaveAsync($"S{i}", MakeResult(sourceValue: i, followUpValue: i + 1));
        }

        var recent = await repo.ListRecentAsync(limit: 2);

        Assert.Equal(2, recent.Count);
    }

    [Fact]
    public async Task ListByScenarioAsync_filters_to_one_scenario()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await repo.SaveAsync("ScenarioA", MakeResult());
        await repo.SaveAsync("ScenarioB", MakeResult());
        await repo.SaveAsync("ScenarioA", MakeResult(sourceValue: 9, followUpValue: 10));

        var aResults = await repo.ListByScenarioAsync("ScenarioA");
        var bResults = await repo.ListByScenarioAsync("ScenarioB");

        Assert.Equal(2, aResults.Count);
        Assert.All(aResults, r => Assert.Equal("ScenarioA", r.ScenarioName));
        Assert.Single(bResults);
        Assert.Equal("ScenarioB", bResults[0].ScenarioName);
    }

    [Fact]
    public async Task ListByScenarioAsync_rejects_blank_scenario_name()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await Assert.ThrowsAsync<ArgumentException>(() => repo.ListByScenarioAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.ListByScenarioAsync("   "));
    }

    [Fact]
    public async Task ListRecentAsync_rejects_non_positive_limit()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.ListRecentAsync(0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.ListRecentAsync(-1));
    }

    [Fact]
    public async Task SaveAsync_rejects_blank_scenario_name()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await Assert.ThrowsAsync<ArgumentException>(() => repo.SaveAsync("", MakeResult()));
    }

    [Fact]
    public async Task SaveAsync_then_reopen_database_returns_same_record()
    {
        string id;
        using (var repo = new LiteDbSystemMtResultRepository(_dbPath))
        {
            id = await repo.SaveAsync("PersistedScenario", MakeResult());
        }

        using var reopened = new LiteDbSystemMtResultRepository(_dbPath);
        var record = await reopened.GetAsync(id);

        Assert.NotNull(record);
        Assert.Equal("PersistedScenario", record!.ScenarioName);
    }
}
