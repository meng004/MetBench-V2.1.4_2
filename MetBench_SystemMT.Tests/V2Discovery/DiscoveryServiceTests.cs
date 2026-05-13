using MetBench_BLL.Discovery;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class DiscoveryServiceTests
{
    [Fact]
    public async Task RunAsync_writes_run_and_candidates()
    {
        var (svc, runs, cands, _) = MakeService();
        var discoverer = new StubDiscoverer("stub", "ok",
            new CandidateMrProposal("MR-T-001", "scale fuel σ_f", null, "less", "k_eff", "rationale", 0.8),
            new CandidateMrProposal("MR-T-002", "scale moderator σ_a", null, "less", "k_eff", "rationale", 0.7));

        var result = await svc.RunAsync(discoverer, methodId: 1, targetApplicationId: 1, actor: "alice");

        Assert.Single(runs.Data);
        Assert.Equal("ok", runs.Data[0].Status);
        Assert.Equal(2, runs.Data[0].CandidatesProduced);
        Assert.Equal(2, cands.Data.Count);
        Assert.Equal("pending", cands.Data[0].Status);
        Assert.Equal(2, result.CreatedCandidateIds.Count);
    }

    [Fact]
    public async Task RunAsync_error_status_keeps_zero_candidates()
    {
        var (svc, runs, cands, _) = MakeService();
        var discoverer = new StubDiscoverer("stub", "error",
            errorMessage: "python crashed");

        var result = await svc.RunAsync(discoverer, methodId: 1, null, actor: "bob");

        Assert.Single(runs.Data);
        Assert.Equal("error", runs.Data[0].Status);
        Assert.Empty(cands.Data);
        Assert.Equal(0, runs.Data[0].CandidatesProduced);
        Assert.NotNull(result.RawOutcome.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_writes_audit_log()
    {
        var (svc, _, _, audit) = MakeService();
        var discoverer = new StubDiscoverer("stub", "ok",
            new CandidateMrProposal("MR-T-001", "n", null, null, "k_eff", "r", 0.5));

        await svc.RunAsync(discoverer, methodId: 1, null, actor: "alice");

        Assert.Single(audit.Logs);
        Assert.Equal("discovery.run", audit.Logs[0].Action);
        Assert.Equal("alice", audit.Logs[0].Actor);
    }

    [Fact]
    public async Task RunAsync_catches_discoverer_throw_marks_error()
    {
        var (svc, runs, cands, _) = MakeService();
        var discoverer = new ThrowingDiscoverer();

        var result = await svc.RunAsync(discoverer, methodId: 1, null, "alice");

        Assert.Single(runs.Data);
        Assert.Equal("error", runs.Data[0].Status);
        Assert.Empty(cands.Data);
        Assert.Equal("boom", result.RawOutcome.ErrorMessage);
    }

    private static (DiscoveryService, FakeDiscoveryRunRepository, FakeCandidateMRRepository, FakeAuditLogRepository) MakeService()
    {
        var runs = new FakeDiscoveryRunRepository();
        var cands = new FakeCandidateMRRepository();
        var audit = new FakeAuditLogRepository();
        var svc = new DiscoveryService(new FakeDiscoveryMethodRepository(), runs, cands, audit);
        return (svc, runs, cands, audit);
    }
}

internal sealed class StubDiscoverer : IMRDiscoverer
{
    private readonly DiscoveryRunOutcome _outcome;
    public StubDiscoverer(string name, string status, params CandidateMrProposal[] proposals)
    {
        Name = name;
        _outcome = new DiscoveryRunOutcome(status, $"stub:{name}", proposals);
    }
    public StubDiscoverer(string name, string status, string errorMessage)
    {
        Name = name;
        _outcome = new DiscoveryRunOutcome(status, $"stub:{name}", Array.Empty<CandidateMrProposal>(), errorMessage);
    }
    public string Name { get; }
    public Task<DiscoveryRunOutcome> DiscoverAsync(int? t, CancellationToken ct = default)
        => Task.FromResult(_outcome);
}

internal sealed class ThrowingDiscoverer : IMRDiscoverer
{
    public string Name => "throwing";
    public Task<DiscoveryRunOutcome> DiscoverAsync(int? t, CancellationToken ct = default)
        => throw new InvalidOperationException("boom");
}
