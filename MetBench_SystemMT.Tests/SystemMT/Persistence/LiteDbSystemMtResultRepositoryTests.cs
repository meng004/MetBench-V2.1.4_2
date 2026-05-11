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

    private async Task SeedAsync(LiteDbSystemMtResultRepository repo, int count, string scenarioPrefix = "Scenario")
    {
        for (var i = 0; i < count; i++)
        {
            await repo.SaveAsync(
                $"{scenarioPrefix}-{i % 3}",
                MakeResult(sourceValue: i, followUpValue: i + 1));
            // small delay so RunAt timestamps differ deterministically
            await Task.Delay(2);
        }
    }

    [Fact]
    public async Task ListPagedAsync_rejects_null_request()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.ListPagedAsync(null!));
    }

    [Fact]
    public async Task ListPagedAsync_rejects_invalid_request()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(-1, 10)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(0, 0)));
    }

    [Fact]
    public async Task ListPagedAsync_empty_repo_returns_empty_page_total_zero()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        var page = await repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(0, 10));

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.PageIndex);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasPrevious);
        Assert.False(page.HasNext);
    }

    [Fact]
    public async Task ListPagedAsync_first_page_returns_items_and_correct_metadata()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await SeedAsync(repo, 25);

        var page = await repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(0, 10));

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.False(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public async Task ListPagedAsync_middle_page_returns_correct_slice()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await SeedAsync(repo, 25);

        var page = await repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(1, 10));

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public async Task ListPagedAsync_last_page_returns_partial_slice()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await SeedAsync(repo, 25);

        var page = await repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(2, 10));

        Assert.Equal(5, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.True(page.HasPrevious);
        Assert.False(page.HasNext);
    }

    [Fact]
    public async Task ListPagedAsync_page_beyond_range_returns_empty_items_with_total_preserved()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await SeedAsync(repo, 25);

        var page = await repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(99, 10));

        Assert.Empty(page.Items);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task ListPagedAsync_orders_items_descending_by_run_at()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await SeedAsync(repo, 5);

        var page = await repo.ListPagedAsync(new MetBench_BLL.Paging.PageRequest(0, 5));

        Assert.Equal(5, page.Items.Count);
        for (var i = 1; i < page.Items.Count; i++)
        {
            Assert.True(page.Items[i - 1].RunAt >= page.Items[i].RunAt,
                $"items not descending at index {i}: {page.Items[i - 1].RunAt} vs {page.Items[i].RunAt}");
        }
    }

    [Fact]
    public async Task ListPagedByScenarioAsync_filters_count_and_items_to_one_scenario()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        // 9 records total: 3 in each of Scenario-0, Scenario-1, Scenario-2
        await SeedAsync(repo, 9);

        var page = await repo.ListPagedByScenarioAsync(
            "Scenario-1",
            new MetBench_BLL.Paging.PageRequest(0, 10));

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
        Assert.All(page.Items, r => Assert.Equal("Scenario-1", r.ScenarioName));
    }

    [Fact]
    public async Task ListPagedByScenarioAsync_rejects_blank_scenario_name()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.ListPagedByScenarioAsync("", new MetBench_BLL.Paging.PageRequest(0, 10)));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.ListPagedByScenarioAsync("   ", new MetBench_BLL.Paging.PageRequest(0, 10)));
    }

    [Fact]
    public async Task ListPagedByScenarioAsync_rejects_null_request()
    {
        using var repo = new LiteDbSystemMtResultRepository(_dbPath);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repo.ListPagedByScenarioAsync("Scenario-0", null!));
    }
}

