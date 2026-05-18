using MetBench_BLL.SystemMT.Pipeline;
using MetBench_DAL;
using MetBench_DAL.V2;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pagination;

/// <summary>
/// F10 — LiteDB keyset pagination (O(pageSize) vs Skip()'s O(offset+pageSize)).
/// </summary>
/// <remarks>
/// 这里仅验证语义正确：
///   - 首页传 cursor=null
///   - 拿下一页用上一页最后一行的 Id 作 cursor
///   - 升序遍历不重复 / 不遗漏 / 覆盖全集
/// 性能验证靠 CI baseline（F14 已栅）。
/// </remarks>
[Collection("DbConfigGlobal")]
public sealed class KeysetPaginationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _conn;
    private readonly DbConfig _cfg;

    public KeysetPaginationTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchKeysetPaginationTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _conn = $"Filename={_dbPath}";
        DbConfig.OverrideConnectionString(_conn);
        _cfg = DbConfig.Instance;
    }

    public void Dispose()
    {
        DbConfig.ResetOverride();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile)) File.Delete(logFile);
    }

    // ===== GetPageKeyset (PK-only) =====

    [Fact]
    public void GetPageKeyset_first_page_with_null_cursor_returns_lowest_ids()
    {
        var repo = new LiteDbExecutionRepository();
        var ids = SeedExecutions(repo, count: 25);

        var page = repo.GetPageKeyset(afterId: null, pageSize: 10);

        Assert.Equal(10, page.Count);
        var pageIds = page.Select(x => x.IdExecution).ToList();
        var expectedFirst10 = ids.OrderBy(g => g).Take(10).ToList();
        Assert.Equal(expectedFirst10, pageIds);
    }

    [Fact]
    public void GetPageKeyset_walks_all_rows_without_overlap_or_gap()
    {
        var repo = new LiteDbExecutionRepository();
        var seeded = SeedExecutions(repo, count: 23);

        var collected = new List<Guid>();
        Guid? cursor = null;
        const int pageSize = 5;
        for (int i = 0; i < 10 && i * pageSize < 100; i++)
        {
            var page = repo.GetPageKeyset(cursor, pageSize);
            if (page.Count == 0) break;
            collected.AddRange(page.Select(x => x.IdExecution));
            cursor = page[^1].IdExecution;
        }

        Assert.Equal(seeded.Count, collected.Count);
        Assert.Equal(seeded.OrderBy(g => g), collected);
        Assert.Equal(collected.Distinct().Count(), collected.Count);
    }

    [Fact]
    public void GetPageKeyset_returns_empty_when_cursor_is_past_max()
    {
        var repo = new LiteDbExecutionRepository();
        var ids = SeedExecutions(repo, count: 3);
        var maxId = ids.OrderBy(g => g).Last();

        var page = repo.GetPageKeyset(afterId: maxId, pageSize: 10);

        Assert.Empty(page);
    }

    [Fact]
    public void GetPageKeyset_with_zero_pageSize_returns_empty()
    {
        var repo = new LiteDbExecutionRepository();
        SeedExecutions(repo, count: 5);

        var page = repo.GetPageKeyset(afterId: null, pageSize: 0);

        Assert.Empty(page);
    }

    [Fact]
    public void GetPageKeyset_on_empty_collection_returns_empty()
    {
        var repo = new LiteDbExecutionRepository();
        var page = repo.GetPageKeyset(afterId: null, pageSize: 10);
        Assert.Empty(page);
    }

    // ===== GetByStatusKeyset (status + time cursor) =====

    [Fact]
    public void GetByStatusKeyset_filters_by_status_and_orders_by_queuedat_desc()
    {
        var repo = new LiteDbExecutionRepository();
        var anchor = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 5; i++)
            repo.Add(MakeExec(PipelineStatus.Anomaly, anchor.AddMinutes(i)));
        for (int i = 0; i < 3; i++)
            repo.Add(MakeExec(PipelineStatus.Ok, anchor.AddMinutes(i + 100)));

        var page = repo.GetByStatusKeyset(
            "anomaly", afterQueuedAt: null, afterId: null, pageSize: 10);

        Assert.Equal(5, page.Count);
        Assert.All(page, e => Assert.Equal("anomaly", e.Status));
        // newest (anchor+4min) first
        Assert.Equal(anchor.AddMinutes(4), page[0].QueuedAt);
        Assert.Equal(anchor, page[^1].QueuedAt);
    }

    [Fact]
    public void GetByStatusKeyset_uses_cursor_for_subsequent_pages()
    {
        var repo = new LiteDbExecutionRepository();
        var anchor = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 12; i++)
            repo.Add(MakeExec(PipelineStatus.Anomaly, anchor.AddMinutes(i)));

        var page1 = repo.GetByStatusKeyset("anomaly", null, null, pageSize: 5);
        Assert.Equal(5, page1.Count);
        var last1 = page1[^1];

        var page2 = repo.GetByStatusKeyset("anomaly", last1.QueuedAt, last1.IdExecution, pageSize: 5);
        Assert.Equal(5, page2.Count);
        Assert.True(page2[0].QueuedAt < last1.QueuedAt
                    || (page2[0].QueuedAt == last1.QueuedAt && page2[0].IdExecution.CompareTo(last1.IdExecution) < 0));

        var last2 = page2[^1];
        var page3 = repo.GetByStatusKeyset("anomaly", last2.QueuedAt, last2.IdExecution, pageSize: 5);
        Assert.Equal(2, page3.Count);

        var allCollected = page1.Concat(page2).Concat(page3).Select(e => e.IdExecution).Distinct().Count();
        Assert.Equal(12, allCollected);
    }

    [Fact]
    public void GetByStatusKeyset_disambiguates_same_queuedat_via_id_tiebreaker()
    {
        var repo = new LiteDbExecutionRepository();
        var sameTime = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 6; i++)
            repo.Add(MakeExec(PipelineStatus.Anomaly, sameTime));

        var page1 = repo.GetByStatusKeyset("anomaly", null, null, pageSize: 3);
        Assert.Equal(3, page1.Count);
        var last1 = page1[^1];
        Assert.Equal(sameTime, last1.QueuedAt);

        var page2 = repo.GetByStatusKeyset("anomaly", last1.QueuedAt, last1.IdExecution, pageSize: 3);
        Assert.Equal(3, page2.Count);
        Assert.All(page2, e => Assert.True(e.IdExecution.CompareTo(last1.IdExecution) < 0,
            $"page2 row {e.IdExecution} should be < cursor {last1.IdExecution}"));
    }

    [Fact]
    public void GetByStatusKeyset_returns_empty_when_no_rows_match_status()
    {
        var repo = new LiteDbExecutionRepository();
        repo.Add(MakeExec(PipelineStatus.Ok, DateTime.UtcNow));

        var page = repo.GetByStatusKeyset("anomaly", null, null, pageSize: 10);

        Assert.Empty(page);
    }

    [Fact]
    public void GetByStatusKeyset_with_zero_pageSize_returns_empty()
    {
        var repo = new LiteDbExecutionRepository();
        repo.Add(MakeExec(PipelineStatus.Anomaly, DateTime.UtcNow));

        var page = repo.GetByStatusKeyset("anomaly", null, null, pageSize: 0);

        Assert.Empty(page);
    }

    // ===== helpers =====

    private static List<Guid> SeedExecutions(LiteDbExecutionRepository repo, int count)
    {
        var ids = new List<Guid>(count);
        for (int i = 0; i < count; i++)
        {
            var e = MakeExec(PipelineStatus.Ok, DateTime.UtcNow.AddMinutes(i));
            repo.Add(e);
            ids.Add(e.IdExecution);
        }
        return ids;
    }

    private static Execution MakeExec(string status, DateTime queuedAt) => new()
    {
        IdExecution = Guid.NewGuid(),
        QueuedAt = queuedAt,
        StartedAt = queuedAt,
        FinishedAt = queuedAt.AddSeconds(5),
        Status = status,
        TriggeredBy = "test",
        CatalogVersionSha = "abc",
        SutVersionSnapshot = "openmoc@0.0.0",
        MetbenchVersion = "v2.1.4",
    };
}
