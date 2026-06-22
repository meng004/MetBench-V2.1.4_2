using MetBench_Api;
using MetBench_BLL.SystemMT.ControlPlane;
using MetBench_BLL.SystemMT.Jobs;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Api;

public sealed class SystemMtApiEndpointsTests
{
    [Fact]
    public void Program_registers_control_plane_facade_dependencies()
    {
        var root = SolutionRoot();
        var program = File.ReadAllText(Path.Combine(root, "MetBench_Api", "Program.cs"));

        Assert.Contains("AddSingleton<IJobQueue, ChannelJobQueue>()", program);
        Assert.Contains("AddSingleton<IJobStore, InMemoryJobStore>()", program);
        Assert.Contains("AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>()", program);
        Assert.Contains("AddSingleton<ISystemMtJobService, SystemMtJobService>()", program);
        Assert.Contains(
            "AddSingleton<ISystemMtControlPlaneService, SystemMtControlPlaneService>()",
            program);
        Assert.Contains("AddSystemMtRepositories()", program);
        Assert.Contains("AddScoped<ISystemMtLauncher>", program);
        Assert.Contains("AddHostedService<SystemMtApiJobWorkerHostedService>()", program);
    }

    [Fact]
    public void Program_uses_dbconfig_as_single_database_configuration_path()
    {
        var root = SolutionRoot();
        var program = File.ReadAllText(Path.Combine(root, "MetBench_Api", "Program.cs"));

        Assert.DoesNotContain("new LiteDatabase", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MetBench:DataDir", program, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<LiteDatabase>", program, StringComparison.Ordinal);
        Assert.Contains("DbConfig.Instance._conn", program, StringComparison.Ordinal);
        Assert.Contains("new LiteDbSystemMtResultRepository(DbConfig.Instance._conn)", program, StringComparison.Ordinal);
        Assert.Contains("new LiteDbExecutionEvidenceRepository(DbConfig.Instance._conn)", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_maps_cancel_as_job_action_not_delete_resource()
    {
        var root = SolutionRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "MetBench_Api", "SystemMtApiEndpoints.cs"));

        Assert.Contains("MapPost(\"/jobs/{jobId:guid}/cancel\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete(\"/jobs/{jobId:guid}\"", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitRunAsync_returns_accepted_and_forwards_control_plane_request()
    {
        var controlPlane = new FakeControlPlane();
        var request = new SystemMtSubmitRunRequest(
            "mr-alpha",
            new Dictionary<string, string> { ["scale"] = "2.0" });

        var result = await SystemMtApiEndpoints.SubmitRunAsync(controlPlane, request, default);

        var accepted = Assert.IsType<Accepted<SystemMtJobReceiptResponse>>(result.Result);
        Assert.Equal($"/api/v1/systemmt/jobs/{controlPlane.Receipt.JobId}", accepted.Location);
        Assert.Equal(controlPlane.Receipt.JobId, accepted.Value!.JobId);
        Assert.Equal("mr-alpha", controlPlane.Submitted!.MrId);
        Assert.Equal("2.0", controlPlane.Submitted.ParameterOverrides!["scale"]);
    }

    [Fact]
    public async Task SubmitRunAsync_returns_bad_request_for_control_plane_validation_errors()
    {
        var controlPlane = new FakeControlPlane
        {
            SubmitException = new ArgumentException("MrId must be non-blank."),
        };

        var result = await SystemMtApiEndpoints.SubmitRunAsync(
            controlPlane,
            new SystemMtSubmitRunRequest(" ", null),
            default);

        var badRequest = Assert.IsType<BadRequest<SystemMtApiError>>(result.Result);
        Assert.Equal("bad_request", badRequest.Value!.Code);
        Assert.Contains("MrId", badRequest.Value.Message);
    }

    [Fact]
    public async Task GetJobAsync_returns_not_found_for_unknown_job()
    {
        var result = await SystemMtApiEndpoints.GetJobAsync(
            new FakeControlPlane(),
            Guid.NewGuid(),
            default);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetJobAsync_returns_snapshot_from_control_plane()
    {
        var jobId = Guid.NewGuid();
        var controlPlane = new FakeControlPlane
        {
            Job = new SystemMtControlPlaneJobSnapshot(
                jobId,
                "mr-alpha",
                "openmc",
                SystemMtJobState.Succeeded,
                "succeeded",
                100,
                new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 21, 0, 1, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 21, 0, 2, 0, DateTimeKind.Utc),
                null,
                null,
                Guid.NewGuid()),
        };

        var result = await SystemMtApiEndpoints.GetJobAsync(controlPlane, jobId, default);

        var ok = Assert.IsType<Ok<SystemMtControlPlaneJobSnapshot>>(result.Result);
        Assert.Equal(jobId, ok.Value!.JobId);
        Assert.Equal(SystemMtJobState.Succeeded, ok.Value.State);
    }

    [Fact]
    public async Task GetResultAsync_returns_result_from_control_plane()
    {
        var jobId = Guid.NewGuid();
        var controlPlane = new FakeControlPlane
        {
            Result = new SystemMtControlPlaneRunResult(
                jobId,
                Guid.NewGuid(),
                "mr-alpha",
                Passed: false,
                "assertion failed",
                "k_eff",
                SourceValue: 1.0,
                FollowUpValue: 2.0,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2)),
        };

        var result = await SystemMtApiEndpoints.GetResultAsync(controlPlane, jobId, default);

        var ok = Assert.IsType<Ok<SystemMtControlPlaneRunResult>>(result.Result);
        Assert.Equal(jobId, ok.Value!.JobId);
        Assert.False(ok.Value.Passed);
    }

    [Fact]
    public async Task GetEvidenceAsync_returns_runtime_evidence_from_control_plane()
    {
        var jobId = Guid.NewGuid();
        var controlPlane = new FakeControlPlane
        {
            Evidence = new SystemMtControlPlaneEvidenceSnapshot(
                jobId,
                Guid.NewGuid(),
                "DockerMcp",
                "openmc-docker",
                RuntimePassed: true,
                "None",
                "mcp-run-source",
                "mcp-run-followup"),
        };

        var result = await SystemMtApiEndpoints.GetEvidenceAsync(controlPlane, jobId, default);

        var ok = Assert.IsType<Ok<SystemMtControlPlaneEvidenceSnapshot>>(result.Result);
        Assert.Equal("mcp-run-source", ok.Value!.SourceRunId);
        Assert.Equal("mcp-run-followup", ok.Value.FollowupRunId);
    }

    [Fact]
    public async Task CancelJobAsync_delegates_to_control_plane_and_returns_no_content()
    {
        var jobId = Guid.NewGuid();
        var controlPlane = new FakeControlPlane();

        var result = await SystemMtApiEndpoints.CancelJobAsync(controlPlane, jobId, default);

        Assert.IsType<NoContent>(result);
        Assert.Equal(jobId, controlPlane.CancelledJobId);
    }

    private sealed class FakeControlPlane : ISystemMtControlPlaneService
    {
        public SystemMtControlPlaneJobReceipt Receipt { get; } =
            new(Guid.NewGuid(), new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc));

        public SystemMtControlPlaneRunRequest? Submitted { get; private set; }
        public Exception? SubmitException { get; init; }
        public SystemMtControlPlaneJobSnapshot? Job { get; init; }
        public SystemMtControlPlaneRunResult? Result { get; init; }
        public SystemMtControlPlaneEvidenceSnapshot? Evidence { get; init; }
        public Guid? CancelledJobId { get; private set; }

        public Task<SystemMtControlPlaneJobReceipt> SubmitRunAsync(
            SystemMtControlPlaneRunRequest request,
            CancellationToken cancellationToken = default)
        {
            if (SubmitException is not null)
                throw SubmitException;

            Submitted = request;
            return Task.FromResult(Receipt);
        }

        public Task<SystemMtControlPlaneJobSnapshot?> GetJobAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Job);

        public Task<SystemMtControlPlaneRunResult?> GetResultAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result);

        public Task<SystemMtControlPlaneEvidenceSnapshot?> GetEvidenceAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Evidence);

        public Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            CancelledJobId = jobId;
            return Task.CompletedTask;
        }
    }

    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate solution root from {AppContext.BaseDirectory}.");
    }
}
