using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Persistence.Editing;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence.Editing;

public sealed class ExecutionHistoryEditorTests
{
    [Fact]
    public async Task DeleteAsync_happy_path_deletes_evidence_first_then_result()
    {
        var evidence = new SequencingEvidenceRepo();
        var results = new SequencingResultRepo();
        var id = Guid.NewGuid();
        evidence.Seed(id);
        results.Seed(id);

        var editor = new ExecutionHistoryEditor(results, evidence);

        var outcome = await editor.DeleteAsync(id);

        Assert.Equal(new ExecutionHistoryDeleteResult(Deleted: 1, EvidenceOnly: 0, Failed: 0), outcome);
        Assert.Equal(new[] { "evidence", "result" }, SequencingLog.OrderedCalls);
    }

    [Fact]
    public async Task DeleteAsync_returns_evidence_only_when_result_delete_throws()
    {
        var evidence = new SequencingEvidenceRepo();
        var results = new SequencingResultRepo { ThrowOnDelete = true };
        var id = Guid.NewGuid();
        evidence.Seed(id);
        results.Seed(id);
        var editor = new ExecutionHistoryEditor(results, evidence);

        var outcome = await editor.DeleteAsync(id);

        Assert.Equal(new ExecutionHistoryDeleteResult(Deleted: 0, EvidenceOnly: 1, Failed: 0), outcome);
    }

    [Fact]
    public async Task DeleteAsync_returns_failed_when_evidence_delete_throws_and_does_not_touch_result()
    {
        var evidence = new SequencingEvidenceRepo { ThrowOnDelete = true };
        var results = new SequencingResultRepo();
        var id = Guid.NewGuid();
        evidence.Seed(id);
        results.Seed(id);
        var editor = new ExecutionHistoryEditor(results, evidence);

        var outcome = await editor.DeleteAsync(id);

        Assert.Equal(new ExecutionHistoryDeleteResult(Deleted: 0, EvidenceOnly: 0, Failed: 1), outcome);
        // Result row not touched
        Assert.True(results.Contains(id));
    }

    [Fact]
    public async Task DeleteAsync_succeeds_when_evidence_row_does_not_exist_legacy_execution()
    {
        var evidence = new SequencingEvidenceRepo(); // no seed
        var results = new SequencingResultRepo();
        var id = Guid.NewGuid();
        results.Seed(id);
        var editor = new ExecutionHistoryEditor(results, evidence);

        var outcome = await editor.DeleteAsync(id);

        Assert.Equal(new ExecutionHistoryDeleteResult(Deleted: 1, EvidenceOnly: 0, Failed: 0), outcome);
    }

    [Fact]
    public async Task DeleteAsync_returns_evidence_only_when_result_id_not_found()
    {
        var evidence = new SequencingEvidenceRepo();
        var results = new SequencingResultRepo();
        var id = Guid.NewGuid();
        evidence.Seed(id);
        // Result not seeded → DeleteAsync returns false (not parseable / not present)
        var editor = new ExecutionHistoryEditor(results, evidence);

        var outcome = await editor.DeleteAsync(id);

        Assert.Equal(new ExecutionHistoryDeleteResult(Deleted: 0, EvidenceOnly: 1, Failed: 0), outcome);
    }

    [Fact]
    public async Task DeleteBatchAsync_sums_per_row_outcomes_across_3_segments()
    {
        var evidence = new SequencingEvidenceRepo();
        var results = new SequencingResultRepo();
        var ok = Guid.NewGuid();
        var evidenceOnly = Guid.NewGuid();
        var failed = Guid.NewGuid();

        evidence.Seed(ok);
        results.Seed(ok);
        evidence.Seed(evidenceOnly);
        // result row missing for evidenceOnly
        // evidence ThrowOnce on first call to simulate per-row variance: use per-id behaviour
        // Simpler approach: mark `failed` as throwing for both repos
        evidence.PerIdThrow.Add(failed);
        results.Seed(failed);

        var editor = new ExecutionHistoryEditor(results, evidence);

        var outcome = await editor.DeleteBatchAsync(new[] { ok, evidenceOnly, failed });

        Assert.Equal(new ExecutionHistoryDeleteResult(Deleted: 1, EvidenceOnly: 1, Failed: 1), outcome);
    }

    [Fact]
    public async Task ListPagedAsync_delegates_to_underlying_result_repository()
    {
        var results = new SequencingResultRepo();
        results.Seed(Guid.NewGuid());
        results.Seed(Guid.NewGuid());
        var editor = new ExecutionHistoryEditor(results, new SequencingEvidenceRepo());

        var page = await editor.ListPagedAsync(new PageRequest(0, 50));

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task ListPagedByMrNameAsync_delegates_to_underlying_result_repository()
    {
        var results = new SequencingResultRepo();
        var id = Guid.NewGuid();
        results.Seed(id, mrName: "mr-alpha");
        results.Seed(Guid.NewGuid(), mrName: "mr-beta");
        var editor = new ExecutionHistoryEditor(results, new SequencingEvidenceRepo());

        var page = await editor.ListPagedByMrNameAsync("mr-alpha", new PageRequest(0, 50));

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("mr-alpha", page.Items[0].MrName);
    }

    [Fact]
    public async Task GetEvidenceAsync_delegates_to_evidence_repository()
    {
        var evidence = new SequencingEvidenceRepo();
        var id = Guid.NewGuid();
        evidence.Seed(id);
        var editor = new ExecutionHistoryEditor(new SequencingResultRepo(), evidence);

        var ev = await editor.GetEvidenceAsync(id);

        Assert.NotNull(ev);
        Assert.Equal(id, ev!.ExecutionId);
    }

    /// <summary>
    /// Test-shared ordering log. Each repo appends its kind ("evidence"|"result")
    /// when its delete is called; the test reads the log to assert the
    /// editor sequences Evidence before Result.
    /// </summary>
    private static class SequencingLog
    {
        public static List<string> OrderedCalls { get; } = new();
        public static void Reset() => OrderedCalls.Clear();
    }

    private sealed class SequencingEvidenceRepo : IExecutionEvidenceRepository
    {
        private readonly HashSet<Guid> _seeded = new();
        public bool ThrowOnDelete { get; set; }
        public HashSet<Guid> PerIdThrow { get; } = new();

        public SequencingEvidenceRepo() { SequencingLog.Reset(); }

        public void Seed(Guid executionId) => _seeded.Add(executionId);

        public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("not used in editor tests");

        public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
        {
            if (!_seeded.Contains(executionId))
                return Task.FromResult<ExecutionEvidence?>(null);
            return Task.FromResult<ExecutionEvidence?>(new ExecutionEvidence { ExecutionId = executionId });
        }

        public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default)
        {
            SequencingLog.OrderedCalls.Add("evidence");
            if (ThrowOnDelete || PerIdThrow.Contains(executionId))
                throw new InvalidOperationException("evidence delete failed");
            return Task.FromResult(_seeded.Remove(executionId));
        }
    }

    private sealed class SequencingResultRepo : ISystemMtResultRepository
    {
        private readonly Dictionary<Guid, string> _seeded = new();
        public bool ThrowOnDelete { get; set; }

        public void Seed(Guid id, string mrName = "mr-x") => _seeded[id] = mrName;
        public bool Contains(Guid id) => _seeded.ContainsKey(id);

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            SequencingLog.OrderedCalls.Add("result");
            if (ThrowOnDelete)
                throw new InvalidOperationException("result delete failed");
            if (!Guid.TryParse(id, out var guid))
                return Task.FromResult(false);
            return Task.FromResult(_seeded.Remove(guid));
        }

        public Task<int> DeleteBatchAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            int removed = 0;
            foreach (var id in ids)
            {
                if (Guid.TryParse(id, out var guid) && _seeded.Remove(guid))
                    removed++;
            }
            return Task.FromResult(removed);
        }

        public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default)
        {
            var items = _seeded.Select(kv => new SystemMtResultRecord
            {
                Id = kv.Key,
                MrName = kv.Value,
                RunAt = DateTimeOffset.UtcNow,
            }).ToList();
            return Task.FromResult(new PagedResult<SystemMtResultRecord>(
                items, items.Count, request.PageIndex, request.PageSize));
        }

        public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default)
        {
            var items = _seeded
                .Where(kv => kv.Value == mrName)
                .Select(kv => new SystemMtResultRecord { Id = kv.Key, MrName = kv.Value, RunAt = DateTimeOffset.UtcNow })
                .ToList();
            return Task.FromResult(new PagedResult<SystemMtResultRecord>(
                items, items.Count, request.PageIndex, request.PageSize));
        }

        // Unused interface members
        public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<string> SaveAsync(SystemMtResultRecord record, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
