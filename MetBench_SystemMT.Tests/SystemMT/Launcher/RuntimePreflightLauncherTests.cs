using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class RuntimePreflightLauncherTests
{
    [Fact]
    public async Task Healthy_pure_stdlib_runtime_still_runs_existing_MR_and_attaches_runtime_evidence()
    {
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var evidence = new InMemoryEvidenceRepo();
        var anomalies = new RecordingAnomalyService();
        var preflight = new StubRuntimePreflightService(profile =>
            RuntimePreflightResult.Pass(
                profile,
                "ok",
                new[]
                {
                    new RuntimePreflightDiagnostic(
                        "startup",
                        profile.DisplayName,
                        true,
                        RuntimeFailureKind.None,
                        "startup passed",
                        ExitCode: 0,
                        Stdout: "Python 3"),
                }));
        var options = Options();
        var launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(execs, results, evidence),
            anomalies,
            new ManifestMrCatalogProvider(options),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: preflight);

        var result = await launcher.RunAsync("heat-equation-amplitude");

        Assert.True(result.Passed, result.FailureReason);
        var preflightCall = Assert.Single(preflight.Calls);
        Assert.Equal("system", preflightCall.RuntimeKey);
        var executionId = Assert.Single(execs.Data).IdExecution;
        Assert.Single(results.Data);
        Assert.Empty(anomalies.Recorded);
        var loadedEvidence = await evidence.GetByExecutionAsync(executionId);
        Assert.NotNull(loadedEvidence);
        Assert.NotNull(loadedEvidence!.RuntimeEvidence);
        Assert.Equal("system", loadedEvidence.RuntimeEvidence!.RuntimeKey);
        Assert.True(loadedEvidence.RuntimeEvidence.Passed);
        Assert.Equal(RuntimeFailureKind.None.ToString(), loadedEvidence.RuntimeEvidence.FailureKind);
        Assert.Single(loadedEvidence.RuntimeEvidence.Diagnostics);
    }

    [Fact]
    public async Task Failed_preflight_blocks_before_SUT_execution_and_records_runtime_evidence_without_result_or_anomaly()
    {
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var evidence = new InMemoryEvidenceRepo();
        var anomalies = new RecordingAnomalyService();
        var preflight = new StubRuntimePreflightService(profile =>
            RuntimePreflightResult.Blocked(
                profile,
                RuntimeFailureKind.DependencyMissing,
                "missing dependency: numpy",
                new[]
                {
                    new RuntimePreflightDiagnostic(
                        "dependency",
                        "numpy",
                        false,
                        RuntimeFailureKind.DependencyMissing,
                        "import numpy failed",
                        ExitCode: 1,
                        Stderr: "ModuleNotFoundError"),
                }));
        var options = Options();
        var launcher = new SystemMtLauncher(
            options,
            new ThrowingPipeline(),
            new SystemMtExecutionRecorder(execs, results, evidence),
            anomalies,
            new ManifestMrCatalogProvider(options),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: preflight);

        var result = await launcher.RunAsync("heat-equation-amplitude");

        Assert.False(result.Passed);
        Assert.Contains("missing dependency: numpy", result.FailureReason, StringComparison.Ordinal);
        Assert.Equal("heat-equation-amplitude", result.MrId);
        Assert.True(Guid.TryParse(result.RecordId, out var executionId));

        var execution = Assert.Single(execs.Data);
        Assert.Equal(executionId, execution.IdExecution);
        Assert.Equal(PipelineStatus.Error, execution.Status);
        Assert.Contains("preflight", execution.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(results.Data);
        Assert.Empty(anomalies.Recorded);

        var loadedEvidence = await evidence.GetByExecutionAsync(executionId);
        Assert.NotNull(loadedEvidence);
        Assert.NotNull(loadedEvidence!.RuntimeEvidence);
        Assert.False(loadedEvidence.RuntimeEvidence!.Passed);
        Assert.Equal(RuntimeFailureKind.DependencyMissing.ToString(), loadedEvidence.RuntimeEvidence.FailureKind);
        Assert.Contains("missing dependency: numpy", loadedEvidence.RuntimeEvidence.FailureDetail, StringComparison.Ordinal);
        var diagnostic = Assert.Single(loadedEvidence.RuntimeEvidence.Diagnostics);
        Assert.Equal("dependency", diagnostic.CheckKind);
        Assert.Equal("numpy", diagnostic.Name);
        Assert.False(diagnostic.Passed);
    }

    [Fact]
    public async Task Manifest_runtime_key_is_preserved_when_multiple_keys_share_same_python_executable()
    {
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var evidence = new InMemoryEvidenceRepo();
        var anomalies = new RecordingAnomalyService();
        var preflight = new StubRuntimePreflightService(profile =>
            RuntimePreflightResult.Blocked(
                profile,
                RuntimeFailureKind.DependencyMissing,
                "scipy unavailable",
                new[]
                {
                    new RuntimePreflightDiagnostic(
                        "dependency",
                        "scipy",
                        false,
                        RuntimeFailureKind.DependencyMissing,
                        "import scipy failed",
                        ExitCode: 1),
                }));
        var python = TestAssetPaths.PythonExecutable();
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: python,
            OpenMocPython: python,
            ScipyPython: python);
        var launcher = new SystemMtLauncher(
            options,
            new ThrowingPipeline(),
            new SystemMtExecutionRecorder(execs, results, evidence),
            anomalies,
            new ManifestMrCatalogProvider(options),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: preflight);

        var result = await launcher.RunAsync("scipy-ivp-lv-prey-growth-monotone");

        Assert.False(result.Passed);
        var profile = Assert.Single(preflight.Calls);
        Assert.Equal("scipy", profile.RuntimeKey);
        Assert.Equal(RuntimeKind.PythonVirtualEnvironment, profile.Kind);

        Assert.True(Guid.TryParse(result.RecordId, out var executionId));
        var loadedEvidence = await evidence.GetByExecutionAsync(executionId);
        Assert.NotNull(loadedEvidence);
        Assert.Equal("scipy", loadedEvidence!.RuntimeEvidence!.RuntimeKey);
        Assert.Equal(RuntimeKind.PythonVirtualEnvironment.ToString(), loadedEvidence.RuntimeEvidence.RuntimeKind);
        Assert.Empty(results.Data);
        Assert.Empty(anomalies.Recorded);
    }

    [Fact]
    public async Task Missing_runtime_profile_records_runtime_profile_missing_evidence_before_SUT_execution()
    {
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var evidence = new InMemoryEvidenceRepo();
        var anomalies = new RecordingAnomalyService();
        var options = Options();
        var launcher = new SystemMtLauncher(
            options,
            new ThrowingPipeline(),
            new SystemMtExecutionRecorder(execs, results, evidence),
            anomalies,
            new SingleEntryCatalogProvider(CreateFutureRuntimeEntry(options)),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: new ThrowingPreflightService());

        var result = await launcher.RunAsync("future-runtime-mr");

        Assert.False(result.Passed);
        Assert.Contains("Runtime preflight failed", result.FailureReason, StringComparison.Ordinal);
        Assert.Contains("fenics", result.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(Guid.TryParse(result.RecordId, out var executionId));
        Assert.Empty(results.Data);
        Assert.Empty(anomalies.Recorded);

        var execution = Assert.Single(execs.Data);
        Assert.Equal(executionId, execution.IdExecution);
        Assert.Equal(PipelineStatus.Error, execution.Status);

        var loadedEvidence = await evidence.GetByExecutionAsync(executionId);
        Assert.NotNull(loadedEvidence);
        Assert.NotNull(loadedEvidence!.RuntimeEvidence);
        Assert.Equal("fenics", loadedEvidence.RuntimeEvidence!.RuntimeKey);
        Assert.False(loadedEvidence.RuntimeEvidence.Passed);
        Assert.Equal(RuntimeFailureKind.RuntimeProfileMissing.ToString(), loadedEvidence.RuntimeEvidence.FailureKind);
        var diagnostic = Assert.Single(loadedEvidence.RuntimeEvidence.Diagnostics);
        Assert.Equal("profile", diagnostic.CheckKind);
        Assert.Equal(RuntimeFailureKind.RuntimeProfileMissing.ToString(), diagnostic.FailureKind);
    }

    [Fact]
    public async Task Docker_runtime_profile_preserves_mcp_metadata_when_passed_to_preflight()
    {
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var evidence = new InMemoryEvidenceRepo();
        var anomalies = new RecordingAnomalyService();
        var preflight = new StubRuntimePreflightService(profile =>
            RuntimePreflightResult.Blocked(
                profile,
                RuntimeFailureKind.MiddlewareUnavailable,
                "docker preflight stopped before SUT execution",
                new[]
                {
                    new RuntimePreflightDiagnostic(
                        "middleware",
                        "docker-mcp",
                        false,
                        RuntimeFailureKind.MiddlewareUnavailable,
                        "docker preflight stopped before SUT execution"),
                }));
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            RuntimePythons: new Dictionary<string, string>
            {
                ["openmoc-docker"] =
                    "docker-mcp://openmoc-docker?image=metbench-sut:latest&python=/opt/openmoc-venv/bin/python&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
            });
        var launcher = new SystemMtLauncher(
            options,
            new ThrowingPipeline(),
            new SystemMtExecutionRecorder(execs, results, evidence),
            anomalies,
            new SingleEntryCatalogProvider(CreateDockerRuntimeEntry(options)),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: preflight);

        var result = await launcher.RunAsync("docker-runtime-mr");

        Assert.False(result.Passed);
        var profile = Assert.Single(preflight.Calls);
        Assert.Equal(RuntimeKind.Docker, profile.Kind);
        Assert.NotNull(profile.DockerMcp);
        Assert.Equal("metbench-sut:latest", profile.DockerMcp!.Image);
        Assert.Equal("/opt/openmoc-venv/bin/python", profile.DockerMcp.PythonExecutable);
        Assert.Equal("http://127.0.0.1:8765", profile.DockerMcp.Endpoint);
        Assert.Empty(results.Data);
        Assert.Empty(anomalies.Recorded);
    }

    [Fact]
    public async Task Docker_runtime_profile_is_attached_to_pipeline_context_after_preflight_passes()
    {
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var evidence = new InMemoryEvidenceRepo();
        var anomalies = new RecordingAnomalyService();
        var preflight = new StubRuntimePreflightService(profile =>
            RuntimePreflightResult.Pass(
                profile,
                "docker preflight ok",
                new[]
                {
                    new RuntimePreflightDiagnostic(
                        "docker-mcp",
                        profile.RuntimeKey,
                        true,
                        RuntimeFailureKind.None,
                        "docker preflight ok"),
                }));
        var pipeline = new RecordingPipeline();
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            RuntimePythons: new Dictionary<string, string>
            {
                ["openmoc-docker"] =
                    "docker-mcp://openmoc-docker?image=metbench-sut:latest&python=/opt/openmoc-venv/bin/python&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
            });
        var launcher = new SystemMtLauncher(
            options,
            pipeline,
            new SystemMtExecutionRecorder(execs, results, evidence),
            anomalies,
            new SingleEntryCatalogProvider(CreateDockerRuntimeEntry(options)),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: preflight);

        var result = await launcher.RunAsync("docker-runtime-mr");

        Assert.True(result.Passed, result.FailureReason);
        var context = Assert.Single(pipeline.Contexts);
        Assert.NotNull(context.RuntimeProfile);
        Assert.Equal(RuntimeKind.Docker, context.RuntimeProfile!.Kind);
        Assert.NotNull(context.RuntimeProfile.DockerMcp);
        Assert.Equal("metbench-sut:latest", context.RuntimeProfile.DockerMcp!.Image);
        Assert.Equal("http://127.0.0.1:8765", context.RuntimeProfile.DockerMcp.Endpoint);
        Assert.Equal("/opt/openmoc-venv/bin/python", context.InputParserInvocation.FileName);
        Assert.Equal("/opt/openmoc-venv/bin/python", context.OutputParserInvocation.FileName);
        Assert.Equal("/opt/openmoc-venv/bin/python", context.RunnerInvocation.FileName);
        Assert.DoesNotContain("docker-mcp://", context.InputParserInvocation.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain("docker-mcp://", context.RunnerInvocation.FileName, StringComparison.Ordinal);
        Assert.Single(context.InputParserInvocation.Arguments);
        Assert.Single(context.OutputParserInvocation.Arguments);
        Assert.Single(context.RunnerInvocation.Arguments);
    }


    private static LauncherOptions Options() => new(
        SutRoot: TestAssetPaths.AssetRoot(),
        SystemPython: TestAssetPaths.PythonExecutable(),
        OpenMocPython: TestAssetPaths.PythonExecutable());

    private sealed class StubRuntimePreflightService : IRuntimePreflightService
    {
        private readonly Func<RuntimeProfile, RuntimePreflightResult> _handler;

        public StubRuntimePreflightService(Func<RuntimeProfile, RuntimePreflightResult> handler)
        {
            _handler = handler;
        }

        public List<RuntimeProfile> Calls { get; } = new();

        public Task<RuntimePreflightResult> CheckAsync(
            RuntimeProfile profile,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(profile);
            return Task.FromResult(_handler(profile));
        }
    }

    private sealed class ThrowingPipeline : ISystemMtPipeline
    {
        public Task<PipelineOutcome> ExecuteAsync(
            PipelineContext context,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SUT pipeline should not run after a failed runtime preflight.");

        public Task<PipelineOutcome> ExecuteMultiPhaseAsync(
            MultiPhaseExecutionContext mp,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SUT pipeline should not run after a failed runtime preflight.");
    }

    private sealed class RecordingPipeline : ISystemMtPipeline
    {
        public List<PipelineContext> Contexts { get; } = new();

        public Task<PipelineOutcome> ExecuteAsync(
            PipelineContext context,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Contexts.Add(context);
            return Task.FromResult(PassingOutcome(context));
        }

        public Task<PipelineOutcome> ExecuteMultiPhaseAsync(
            MultiPhaseExecutionContext mp,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Contexts.Add(mp.Base);
            return Task.FromResult(PassingOutcome(mp.Base));
        }

        private static PipelineOutcome PassingOutcome(PipelineContext context) =>
            new(
                FinalStatus: PipelineStatus.Ok,
                ErrorMessage: null,
                StartedAt: DateTime.UtcNow,
                FinishedAt: DateTime.UtcNow,
                ArtifactsDirectory: context.WorkingDirectory,
                SourceInputPath: context.SourceCasePath,
                FollowupInputPath: Path.Combine(context.WorkingDirectory, "followup.in.json"),
                SourceOutputPath: Path.Combine(context.WorkingDirectory, "source.out.json"),
                FollowupOutputPath: Path.Combine(context.WorkingDirectory, "followup.out.json"),
                SourceMetrics: new Dictionary<string, double> { [context.ValueName] = 1.0 },
                FollowupMetrics: new Dictionary<string, double> { [context.ValueName] = 2.0 },
                AssertionResult: new MetBench_BLL.SystemMT.Assertions.SystemMtAssertionResultV2(
                    context.AssertionTypeCode,
                    Passed: true,
                    SourceValue: 1.0,
                    FollowupValue: 2.0,
                    ObservedDelta: 1.0,
                    ExpectedThreshold: null,
                    Expression: "test",
                    FailureReason: null),
                SourceElapsed: TimeSpan.FromMilliseconds(1),
                FollowupElapsed: TimeSpan.FromMilliseconds(1),
                SourceExitCode: 0,
                FollowupExitCode: 0);
    }

    private sealed class ThrowingPreflightService : IRuntimePreflightService
    {
        public Task<RuntimePreflightResult> CheckAsync(
            RuntimeProfile profile,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Runtime preflight service should not run when profile resolution failed.");
    }

    private sealed class SingleEntryCatalogProvider : IMrCatalogProvider
    {
        private readonly MrCatalogEntry _entry;

        public SingleEntryCatalogProvider(MrCatalogEntry entry)
        {
            _entry = entry;
        }

        public string SourceDescription => "Single test entry";

        public IReadOnlyList<MrCatalogEntry> Load() => new[] { _entry };
    }

    private static MrCatalogEntry CreateFutureRuntimeEntry(LauncherOptions options) =>
        new(
            Mr: new MrSummary(
                Id: "future-runtime-mr",
                DisplayName: "Future runtime MR",
                SutName: "heat-equation",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description: "test",
                MrFamily: "HeatEquation.Mono.Amplitude"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: string.Empty,
            WorkRootName: "metbench-runtime-profile-missing-test",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[]
            {
                new MrCatalogTransformStep("ScaleField", "/initial/amplitude"),
            },
            AssertionTypeCode: "greater",
            EquationKey: string.Empty,
            Tolerance: null)
        {
            RuntimeKey = "fenics",
        };

    private static MrCatalogEntry CreateDockerRuntimeEntry(LauncherOptions options) =>
        new(
            Mr: new MrSummary(
                Id: "docker-runtime-mr",
                DisplayName: "Docker runtime MR",
                SutName: "heat-equation",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description: "test",
                MrFamily: "HeatEquation.Mono.Amplitude"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: string.Empty,
            WorkRootName: "metbench-docker-runtime-profile-test",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[]
            {
                new MrCatalogTransformStep("ScaleField", "/initial/amplitude"),
            },
            AssertionTypeCode: "greater",
            EquationKey: string.Empty,
            Tolerance: null)
        {
            RuntimeKey = "openmoc-docker",
        };

    private sealed class InMemoryEvidenceRepo : IExecutionEvidenceRepository
    {
        private readonly List<ExecutionEvidence> _store = new();

        public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _store.RemoveAll(e => e.ExecutionId == evidence.ExecutionId);
            _store.Add(evidence);
            return Task.CompletedTask;
        }

        public Task<ExecutionEvidence?> GetByExecutionAsync(
            Guid executionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ExecutionEvidence?>(_store.Find(e => e.ExecutionId == executionId));
        }

        public Task<bool> DeleteByExecutionIdAsync(
            Guid executionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_store.RemoveAll(e => e.ExecutionId == executionId) > 0);
        }
    }
}
