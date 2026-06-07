using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// PR-Bol-2A pin: <see cref="SystemMtLauncher.RunAsync"/> branches between the legacy
/// 2-side <see cref="ISystemMtPipeline.ExecuteAsync"/> path and the new multi-phase
/// <see cref="ISystemMtPipeline.ExecuteMultiPhaseAsync"/> path depending on whether
/// the matched <c>MrBlueprint</c> carries non-empty <c>RefinementPhases</c>.
/// </summary>
public sealed class ErrorMonotonicLauncherBranchingTests : IDisposable
{
    private readonly string _sutRoot;

    public ErrorMonotonicLauncherBranchingTests()
    {
        _sutRoot = Path.Combine(Path.GetTempPath(), "MetBenchEmLauncherTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_sutRoot, "fake"));
        File.WriteAllText(
            Path.Combine(_sutRoot, "fake", "sample.json"),
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["tracking"] = new Dictionary<string, object?> { ["num_azim"] = 16.0 }
            }));
    }

    public void Dispose()
    {
        try { Directory.Delete(_sutRoot, recursive: true); } catch { /* swallow */ }
    }

    private sealed class SpyPipeline : ISystemMtPipeline
    {
        public int SingleCallCount { get; private set; }
        public int MultiPhaseCallCount { get; private set; }
        public PipelineContext? LastSingleContext { get; private set; }
        public MultiPhaseExecutionContext? LastMultiPhaseContext { get; private set; }

        public Task<PipelineOutcome> ExecuteAsync(
            PipelineContext context, IProgress<string>? progress = null, CancellationToken ct = default)
        {
            SingleCallCount++;
            LastSingleContext = context;
            return Task.FromResult(MakeOk(context.WorkingDirectory));
        }

        public Task<PipelineOutcome> ExecuteMultiPhaseAsync(
            MultiPhaseExecutionContext mp, IProgress<string>? progress = null, CancellationToken ct = default)
        {
            MultiPhaseCallCount++;
            LastMultiPhaseContext = mp;
            return Task.FromResult(MakeOk(mp.Base.WorkingDirectory) with
            {
                TypedSpec = mp.Base.TypedSpec,
                TypedPredicate = mp.Base.TypedPredicate,
            });
        }

        private static PipelineOutcome MakeOk(string workDir) => new(
            FinalStatus: PipelineStatus.Ok,
            ErrorMessage: null,
            StartedAt: DateTime.UtcNow,
            FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: workDir,
            SourceInputPath: "",
            FollowupInputPath: "",
            SourceOutputPath: "",
            FollowupOutputPath: "",
            SourceMetrics: new Dictionary<string, double> { ["k_eff"] = 1.0 },
            FollowupMetrics: new Dictionary<string, double> { ["k_eff"] = 1.0 },
            AssertionResult: new SystemMtAssertionResultV2(
                AssertionTypeCode: "stub", Passed: true,
                SourceValue: 1.0, FollowupValue: 1.0,
                ObservedDelta: null, ExpectedThreshold: null,
                Expression: "stub", FailureReason: null),
            SourceElapsed: TimeSpan.Zero,
            FollowupElapsed: TimeSpan.Zero,
            SourceExitCode: 0,
            FollowupExitCode: 0);
    }

    private sealed class FakeProvider : IMrCatalogProvider
    {
        private readonly IReadOnlyList<MrCatalogEntry> _entries;
        public FakeProvider(IReadOnlyList<MrCatalogEntry> entries) => _entries = entries;
        public string SourceDescription => "Fake";
        public IReadOnlyList<MrCatalogEntry> Load() => _entries;
    }

    private sealed class PassingRuntimePreflightService : IRuntimePreflightService
    {
        public Task<RuntimePreflightResult> CheckAsync(
            RuntimeProfile profile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(RuntimePreflightResult.Pass(profile, "test preflight bypass"));
    }

    private MrCatalogEntry SingleSideEntry() => new(
        Mr: new MrSummary(
            Id: "mr-single", DisplayName: "single", SutName: "fake",
            TransformationName: "ScaleField", AssertionName: "GreaterThan", ValueName: "k_eff",
            DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
            Description: "test", MrFamily: "Test"),
        SampleCaseRelativePath: Path.Combine("fake", "sample.json"),
        RunnerScriptPath: "/tmp/runner.py",
        InputAdapterScriptPath: "/tmp/in_adapter.py",
        OutputAdapterScriptPath: "/tmp/out_adapter.py",
        PythonExecutable: "python3",
        WorkRootName: "MetBenchSingle",
        Timeout: TimeSpan.FromSeconds(30),
        InputParserScriptPath: "/tmp/in_parser.py",
        OutputParserScriptPath: "/tmp/out_parser.py",
        TransformSteps: new[] { new MrCatalogTransformStep("ScaleField", "/tracking/num_azim") },
        AssertionTypeCode: "greater",
        EquationKey: "",
        Tolerance: null,
        RefinementPhases: null);

    private MrCatalogEntry MultiPhaseEntry() => new(
        Mr: new MrSummary(
            Id: "mr-em", DisplayName: "em", SutName: "fake",
            TransformationName: "ScaleField", AssertionName: "ErrorMonotonic", ValueName: "k_eff",
            DefaultParameters: new Dictionary<string, string>(),
            Description: "test", MrFamily: "Test"),
        SampleCaseRelativePath: Path.Combine("fake", "sample.json"),
        RunnerScriptPath: "/tmp/runner.py",
        InputAdapterScriptPath: "/tmp/in_adapter.py",
        OutputAdapterScriptPath: "/tmp/out_adapter.py",
        PythonExecutable: "python3",
        WorkRootName: "MetBenchEm",
        Timeout: TimeSpan.FromSeconds(30),
        InputParserScriptPath: "/tmp/in_parser.py",
        OutputParserScriptPath: "/tmp/out_parser.py",
        TransformSteps: new[] { new MrCatalogTransformStep("ScaleField", "/tracking/num_azim") },
        AssertionTypeCode: AssertionTypeCodes.ErrorMonotonic,
        EquationKey: "",
        Tolerance: null,
        RefinementPhases: new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string> { ["factor"] = "1" }),
            new RefinementPhase("medium", new Dictionary<string, string> { ["factor"] = "2" }),
            new RefinementPhase("reference", new Dictionary<string, string> { ["factor"] = "4" }),
        });

    private SystemMtLauncher BuildLauncher(SpyPipeline pipeline, params MrCatalogEntry[] entries) =>
        new(
            options: new LauncherOptions(_sutRoot, "python3", "python3"),
            pipeline: pipeline,
            recorder: new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            anomalyService: new RecordingAnomalyService(),
            catalogProvider: new FakeProvider(entries),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: new PassingRuntimePreflightService());

    [Fact]
    public async Task RunAsync_routes_2_side_MR_to_ExecuteAsync_only()
    {
        var spy = new SpyPipeline();
        var launcher = BuildLauncher(spy, SingleSideEntry());

        await launcher.RunAsync("mr-single");

        Assert.Equal(1, spy.SingleCallCount);
        Assert.Equal(0, spy.MultiPhaseCallCount);
    }

    [Fact]
    public async Task RunAsync_routes_multi_phase_MR_to_ExecuteMultiPhaseAsync_only()
    {
        var spy = new SpyPipeline();
        var launcher = BuildLauncher(spy, MultiPhaseEntry());

        await launcher.RunAsync("mr-em");

        Assert.Equal(0, spy.SingleCallCount);
        Assert.Equal(1, spy.MultiPhaseCallCount);
    }

    [Fact]
    public async Task RunAsync_multi_phase_injects_ErrorMonotonicPredicate_typed_spec()
    {
        var spy = new SpyPipeline();
        var launcher = BuildLauncher(spy, MultiPhaseEntry());

        await launcher.RunAsync("mr-em");

        var ctx = spy.LastMultiPhaseContext!.Base;
        Assert.NotNull(ctx.TypedSpec);
        Assert.NotNull(ctx.TypedPredicate);
        var predicate = Assert.IsType<ErrorMonotonicPredicate>(ctx.TypedPredicate);
        Assert.Equal("k_eff", predicate.Metric);
        Assert.Equal(NormKind.Relative, predicate.NormKind);
    }

    [Fact]
    public async Task RunAsync_multi_phase_uses_last_phase_as_reference_role_convention()
    {
        var spy = new SpyPipeline();
        var launcher = BuildLauncher(spy, MultiPhaseEntry());

        await launcher.RunAsync("mr-em");

        var predicate = (ErrorMonotonicPredicate)spy.LastMultiPhaseContext!.Base.TypedPredicate!;
        Assert.Equal(new[] { "coarse", "medium" }, predicate.OrderedRoles);
        Assert.Equal("reference", predicate.ReferenceRole);
    }
}
