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
