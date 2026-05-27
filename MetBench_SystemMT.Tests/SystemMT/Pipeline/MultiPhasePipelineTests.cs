using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Pipeline;

/// <summary>
/// PR-Bol-2A pin: <see cref="SystemMtPipeline.ExecuteMultiPhaseAsync"/> flow — per-phase
/// parameter overrides, status-string emission, display-compat field mapping, failure
/// surfaces. Complementary to <see cref="ErrorMonotonicPipelineWiringTests"/> which pins
/// the typed-spec injection contract.
/// </summary>
public sealed class MultiPhasePipelineTests : IDisposable
{
    private readonly string _workDir;
    private readonly List<string> _runnerCommands = new();
    private readonly List<string> _parseSourceCalls = new();

    public MultiPhasePipelineTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), "MetBenchMultiPhasePipelineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
        File.WriteAllText(
            Path.Combine(_workDir, "source.in.json"),
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["tracking"] = new Dictionary<string, object?> { ["num_azim"] = 16.0 }
            }));
    }

    public void Dispose()
    {
        try { Directory.Delete(_workDir, recursive: true); } catch { /* swallow */ }
    }

    private PipelineContext MakeBaseContext() => new(
        MrCode: "mr-em",
        TransformationName: "ScaleField",
        AssertionTypeCode: AssertionTypeCodes.ErrorMonotonic,
        ValueName: "k_eff",
        TargetFieldPath: "/tracking/num_azim",
        PathSyntax: "json-pointer",
        // PR-Bol-2A: phase.Parameters override ctx.Parameters; the default `factor=1`
        // makes the ScaleField transform a no-op so each phase's role-keyed output is
        // determined by the FakeProcessExecutor mapping rather than the transform.
        Parameters: new Dictionary<string, string> { ["factor"] = "1" },
        Tolerance: new AssertionTolerance(),
        ExtraAssertionValues: null,
        SutName: "fake-sut",
        SourceCasePath: Path.Combine(_workDir, "source.in.json"),
        WorkingDirectory: _workDir,
        InputParserCommand: "fake-input-parser",
        OutputParserCommand: "fake-output-parser",
        RunnerCommand: "fake-runner",
        TimeoutSeconds: 30,
        CatalogVersionSha: "test",
        SutVersionSnapshot: "test",
        MetbenchVersion: "v2",
        TriggeredBy: "test");

    private (MrSpec spec, ErrorMonotonicPredicate predicate) BuildSpec()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            "mr-em", "k_eff",
            new[] { "coarse", "medium" }, "reference");
        var predicate = (ErrorMonotonicPredicate)spec.Predicates![0];
        return (spec, predicate);
    }

    private FakeProcessExecutor MakeExecutor(Dictionary<string, double> phaseKeff)
    {
        return new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
                _parseSourceCalls.Add(cmd);
                var data = new Dictionary<string, object?>
                {
                    ["tracking"] = new Dictionary<string, object?> { ["num_azim"] = 16.0 }
                };
                return new ProcessResult(0, JsonSerializer.Serialize(data), "", TimeSpan.FromMilliseconds(10), false);
            }
            if (cmd.Contains("fake-input-parser write"))
                return new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(10), false);
            if (cmd.Contains("fake-runner"))
            {
                _runnerCommands.Add(cmd);
                var outPath = ExtractArg(cmd, "--output \"");
                var inPath = ExtractArg(cmd, "--input \"");
                var role = Path.GetFileNameWithoutExtension(inPath).Replace("phase.in.", "");
                if (!phaseKeff.TryGetValue(role, out var k)) k = double.NaN;
                var values = new { values = new Dictionary<string, double> { ["k_eff"] = k }, metadata = new { } };
                File.WriteAllText(outPath, JsonSerializer.Serialize(values));
                return new ProcessResult(0, "", "", TimeSpan.FromMilliseconds(20), false);
            }
            if (cmd.Contains("fake-output-parser"))
            {
                var outPath = ExtractArg(cmd, "--output-file \"");
                return new ProcessResult(0, File.ReadAllText(outPath), "", TimeSpan.FromMilliseconds(5), false);
            }
            return new ProcessResult(1, "", "Unknown command: " + cmd, TimeSpan.Zero, false);
        });
    }

    private static string ExtractArg(string cmd, string marker)
    {
        var i = cmd.IndexOf(marker, StringComparison.Ordinal);
        var start = i + marker.Length;
        var end = cmd.IndexOf('"', start);
        return cmd.Substring(start, end - start);
    }

    [Fact]
    public async Task Multi_phase_runs_the_SUT_runner_once_per_phase()
    {
        var (spec, predicate) = BuildSpec();
        var exec = MakeExecutor(new() { ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44 });
        var pipeline = new SystemMtPipeline(exec);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(3, _runnerCommands.Count);
        Assert.Contains(_runnerCommands, c => c.Contains("phase.in.coarse.json"));
        Assert.Contains(_runnerCommands, c => c.Contains("phase.in.medium.json"));
        Assert.Contains(_runnerCommands, c => c.Contains("phase.in.reference.json"));
    }

    [Fact]
    public async Task Multi_phase_parses_source_case_exactly_once_regardless_of_phase_count()
    {
        var (spec, predicate) = BuildSpec();
        var exec = MakeExecutor(new() { ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44 });
        var pipeline = new SystemMtPipeline(exec);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        await pipeline.ExecuteMultiPhaseAsync(mp);

        // ParsingSource step runs once. Subsequent phases reuse the parsed source dict in-memory.
        Assert.Single(_parseSourceCalls);
    }

    [Fact]
    public async Task Multi_phase_display_compat_first_phase_into_SourceMetrics()
    {
        var (spec, predicate) = BuildSpec();
        var exec = MakeExecutor(new() { ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44 });
        var pipeline = new SystemMtPipeline(exec);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        // Display-compat: SourceMetrics = first phase (coarse), FollowupMetrics = last phase (reference)
        Assert.Equal(1.40, outcome.SourceMetrics!["k_eff"], precision: 12);
        Assert.Equal(1.44, outcome.FollowupMetrics!["k_eff"], precision: 12);
    }

    [Fact]
    public async Task Multi_phase_emits_progress_strings_with_running_phase_role_prefix()
    {
        var (spec, predicate) = BuildSpec();
        var exec = MakeExecutor(new() { ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44 });
        var pipeline = new SystemMtPipeline(exec);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });
        var states = new List<string>();
        var progress = new SyncProgress(s => states.Add(s));

        await pipeline.ExecuteMultiPhaseAsync(mp, progress);

        Assert.Contains("running-phase:coarse", states);
        Assert.Contains("running-phase:medium", states);
        Assert.Contains("running-phase:reference", states);
    }

    [Fact]
    public async Task Multi_phase_parsing_source_failure_ends_in_error()
    {
        var (spec, predicate) = BuildSpec();
        var fake = new FakeProcessExecutor(_ =>
            new ProcessResult(1, "", "parse error", TimeSpan.FromMilliseconds(5), false));
        var pipeline = new SystemMtPipeline(fake);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Error, outcome.FinalStatus);
        Assert.Contains("ParsingSource", outcome.ErrorMessage!);
    }

    [Fact]
    public async Task Multi_phase_mid_phase_runner_failure_short_circuits_to_error()
    {
        var (spec, predicate) = BuildSpec();
        var runCount = 0;
        var fake = new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
                var data = new Dictionary<string, object?>
                {
                    ["tracking"] = new Dictionary<string, object?> { ["num_azim"] = 16.0 }
                };
                return new ProcessResult(0, JsonSerializer.Serialize(data), "", TimeSpan.FromMilliseconds(5), false);
            }
            if (cmd.Contains("fake-input-parser write"))
                return new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(5), false);
            if (cmd.Contains("fake-runner"))
            {
                runCount++;
                if (runCount == 2) // medium fails
                    return new ProcessResult(13, "", "SUT segfault", TimeSpan.FromMilliseconds(20), false);
                var outPath = ExtractArg(cmd, "--output \"");
                File.WriteAllText(outPath, """{"values":{"k_eff":1.40},"metadata":{}}""");
                return new ProcessResult(0, "", "", TimeSpan.FromMilliseconds(10), false);
            }
            if (cmd.Contains("fake-output-parser"))
            {
                var outPath = ExtractArg(cmd, "--output-file \"");
                return new ProcessResult(0, File.ReadAllText(outPath), "", TimeSpan.FromMilliseconds(5), false);
            }
            return new ProcessResult(1, "", "", TimeSpan.Zero, false);
        });
        var pipeline = new SystemMtPipeline(fake);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Error, outcome.FinalStatus);
        Assert.Contains("medium", outcome.ErrorMessage!);
        // 3rd runner call (reference) is short-circuited
        Assert.Equal(2, runCount);
    }

    [Fact]
    public async Task Multi_phase_mid_phase_timeout_ends_in_timeout()
    {
        var (spec, predicate) = BuildSpec();
        var runCount = 0;
        var fake = new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
                var data = new Dictionary<string, object?>
                {
                    ["tracking"] = new Dictionary<string, object?> { ["num_azim"] = 16.0 }
                };
                return new ProcessResult(0, JsonSerializer.Serialize(data), "", TimeSpan.FromMilliseconds(5), false);
            }
            if (cmd.Contains("fake-input-parser write"))
                return new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(5), false);
            if (cmd.Contains("fake-runner"))
            {
                runCount++;
                if (runCount == 1)
                    return new ProcessResult(-1, "", "killed", TimeSpan.FromMilliseconds(30000), true);
                var outPath = ExtractArg(cmd, "--output \"");
                File.WriteAllText(outPath, """{"values":{"k_eff":1.40},"metadata":{}}""");
                return new ProcessResult(0, "", "", TimeSpan.FromMilliseconds(10), false);
            }
            return new ProcessResult(1, "", "", TimeSpan.Zero, false);
        });
        var pipeline = new SystemMtPipeline(fake);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Timeout, outcome.FinalStatus);
        Assert.Equal(1, runCount); // short-circuits after first phase timeout
    }

    [Fact]
    public async Task Multi_phase_three_phase_minimum_runs_three_SUT_invocations()
    {
        // Boundary: ErrorMonotonicPredicate validator requires OrderedRoles.Count ≥ 2
        // (ErrorMonotonicPredicateValidator.cs:21–24). With the "last phase = ReferenceRole"
        // convention this means total phases ≥ 3. Sanity-check that the boundary actually
        // works end-to-end.
        var spec = TypedSpecFactory.ForErrorMonotonic(
            "mr-em", "k_eff",
            new[] { "coarse", "medium" }, "reference");
        var predicate = (ErrorMonotonicPredicate)spec.Predicates![0];
        var exec = MakeExecutor(new() { ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44 });
        var pipeline = new SystemMtPipeline(exec);
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
        Assert.Equal(3, _runnerCommands.Count);
        Assert.Equal(3, outcome.PhaseMetrics!.Count);
    }
}
