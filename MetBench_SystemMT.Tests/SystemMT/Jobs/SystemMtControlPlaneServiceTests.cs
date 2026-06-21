using System.Reflection;
using MetBench_BLL.SystemMT.ControlPlane;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtControlPlaneServiceTests
{
    [Fact]
    public async Task SubmitRunAsync_forwards_business_job_request_without_runtime_paths()
    {
        var jobs = new CapturingJobService();
        var svc = new SystemMtControlPlaneService(jobs);
        var request = new SystemMtControlPlaneRunRequest(
            "  mr-alpha  ",
            new Dictionary<string, string> { ["scale"] = "2.0" });

        var receipt = await svc.SubmitRunAsync(request, default);

        Assert.Equal(jobs.Handle.JobId, receipt.JobId);
        Assert.Equal("mr-alpha", jobs.Submitted!.MrId);
        Assert.Equal("2.0", jobs.Submitted.ParameterOverrides!["scale"]);
        Assert.False(jobs.Submitted.ParameterOverrides.ContainsKey("manifestPath"));
        Assert.False(jobs.OperationSubmitted);
    }

    [Theory]
    [InlineData("argv")]
    [InlineData("manifestPath")]
    [InlineData("artifactRoot")]
    [InlineData("workingDirectory")]
    [InlineData("exportRoot")]
    public async Task SubmitRunAsync_rejects_reserved_infrastructure_override_keys(string key)
    {
        var svc = new SystemMtControlPlaneService(new CapturingJobService());
        var request = new SystemMtControlPlaneRunRequest(
            "mr-alpha",
            new Dictionary<string, string> { [key] = "unsafe" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SubmitRunAsync(request, default));

        Assert.Contains(key, ex.Message);
    }

    [Fact]
    public async Task SubmitRunAsync_rejects_reserved_override_keys_after_trimming()
    {
        var svc = new SystemMtControlPlaneService(new CapturingJobService());
        var request = new SystemMtControlPlaneRunRequest(
            "mr-alpha",
            new Dictionary<string, string> { [" ArGv "] = "unsafe" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SubmitRunAsync(request, default));

        Assert.Contains("ArGv", ex.Message);
    }

    [Theory]
    [InlineData("artifactPath")]
    [InlineData("sourcePath")]
    [InlineData("manifestUri")]
    [InlineData("runnerCommand")]
    [InlineData("pythonExecutable")]
    [InlineData("dataRoot")]
    public async Task SubmitRunAsync_rejects_infrastructure_looking_override_keys(string key)
    {
        var svc = new SystemMtControlPlaneService(new CapturingJobService());
        var request = new SystemMtControlPlaneRunRequest(
            "mr-alpha",
            new Dictionary<string, string> { [key] = "unsafe" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SubmitRunAsync(request, default));

        Assert.Contains(key, ex.Message);
    }

    [Fact]
    public async Task SubmitRunAsync_rejects_blank_override_key_and_null_value()
    {
        var svc = new SystemMtControlPlaneService(new CapturingJobService());

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SubmitRunAsync(
                new SystemMtControlPlaneRunRequest(
                    "mr-alpha",
                    new Dictionary<string, string> { ["  "] = "1.0" }),
                default));

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SubmitRunAsync(
                new SystemMtControlPlaneRunRequest(
                    "mr-alpha",
                    new Dictionary<string, string> { ["scale"] = null! }),
                default));
    }

    [Fact]
    public async Task GetResultAsync_projects_job_result_with_execution_id()
    {
        var jobId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var jobs = new CapturingJobService
        {
            Status = new SystemMtJobStatus(
                jobId,
                "mr-alpha",
                "openmc",
                SystemMtJobState.Succeeded,
                "succeeded",
                100,
                JobsTestData.T0,
                JobsTestData.T0,
                JobsTestData.T0,
                null,
                ExecutionId: executionId),
            Result = JobsTestData.Result("mr-alpha", passed: false, reason: "assertion failed"),
        };
        var svc = new SystemMtControlPlaneService(jobs);

        var result = await svc.GetResultAsync(jobId, default);

        Assert.NotNull(result);
        Assert.Equal(jobId, result!.JobId);
        Assert.Equal(executionId, result.ExecutionId);
        Assert.False(result.Passed);
        Assert.Equal("assertion failed", result.FailureReason);
    }

    [Fact]
    public async Task GetEvidenceAsync_reads_evidence_through_completed_job_execution_id()
    {
        var jobId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var jobs = new CapturingJobService
        {
            Status = new SystemMtJobStatus(
                jobId,
                "mr-alpha",
                "openmc",
                SystemMtJobState.Succeeded,
                "succeeded",
                100,
                JobsTestData.T0,
                JobsTestData.T0,
                JobsTestData.T0,
                null,
                ExecutionId: executionId)
        };
        var evidence = new FakeEvidenceRepository(new ExecutionEvidence
        {
            ExecutionId = executionId,
            RuntimeEvidence = new RuntimeEvidence
            {
                RuntimeKind = "DockerMcp",
                RuntimeKey = "openmc-docker",
                Passed = true,
                FailureKind = "None",
                SourceRunId = "mcp-run-source",
                FollowupRunId = "mcp-run-followup",
            },
        });
        var svc = new SystemMtControlPlaneService(jobs, evidence);

        var snapshot = await svc.GetEvidenceAsync(jobId, default);

        Assert.NotNull(snapshot);
        Assert.Equal(jobId, snapshot!.JobId);
        Assert.Equal(executionId, snapshot.ExecutionId);
        Assert.Equal("mcp-run-source", snapshot.SourceRunId);
        Assert.Equal("mcp-run-followup", snapshot.FollowupRunId);
    }

    [Fact]
    public void Control_plane_request_dtos_do_not_expose_runtime_control_fields()
    {
        var forbidden = new[]
        {
            "argv", "command", "manifest", "artifactroot", "packageroot",
            "stagingroot", "exportroot", "workingdirectory", "executable"
        };

        var dtoTypes = new[]
        {
            typeof(SystemMtControlPlaneRunRequest),
            typeof(SystemMtControlPlaneJobReceipt),
            typeof(SystemMtControlPlaneJobSnapshot),
            typeof(SystemMtControlPlaneRunResult),
            typeof(SystemMtControlPlaneEvidenceSnapshot),
        };

        foreach (var dto in dtoTypes)
        {
            foreach (var property in dto.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var normalized = property.Name.ToLowerInvariant();
                Assert.DoesNotContain(forbidden, term => normalized.Contains(term, StringComparison.Ordinal));
            }
        }
    }

    private sealed class CapturingJobService : ISystemMtJobService
    {
        public readonly SystemMtJobHandle Handle = new(Guid.NewGuid(), JobsTestData.T0);
        public SystemMtJobRequest? Submitted { get; private set; }
        public bool OperationSubmitted { get; private set; }
        public SystemMtJobStatus? Status { get; init; }
        public MrRunResult? Result { get; init; }

        public Task<SystemMtJobHandle> SubmitAsync(
            SystemMtJobRequest request,
            CancellationToken cancellationToken = default)
        {
            Submitted = request;
            return Task.FromResult(Handle);
        }

        public Task<SystemMtJobHandle> SubmitOperationAsync(
            SystemMtOperationJobRequest request,
            CancellationToken cancellationToken = default)
        {
            OperationSubmitted = true;
            return Task.FromResult(Handle);
        }

        public Task<SystemMtJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Task.FromResult(Status);

        public Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result);

        public Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEvidenceRepository : IExecutionEvidenceRepository
    {
        private readonly ExecutionEvidence _evidence;

        public FakeEvidenceRepository(ExecutionEvidence evidence)
        {
            _evidence = evidence;
        }

        public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
            => Task.FromResult(_evidence.ExecutionId == executionId ? _evidence : null);

        public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
