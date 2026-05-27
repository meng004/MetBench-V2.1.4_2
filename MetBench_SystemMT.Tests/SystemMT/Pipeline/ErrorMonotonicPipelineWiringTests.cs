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
/// PR-Bol-2A pin: <see cref="SystemMtPipeline.ExecuteMultiPhaseAsync"/> contract.
/// The launcher pre-builds the typed spec + predicate via
/// <see cref="TypedSpecFactory.ForErrorMonotonic"/> and injects them onto the context;
/// the pipeline does NOT string-dispatch in the multi-phase path. These tests pin the
/// fail-closed behaviour when the typed pair is missing, and that the dispatch happens
/// against the pre-built predicate.
/// </summary>
public sealed class ErrorMonotonicPipelineWiringTests : IDisposable
{
    private readonly string _workDir;

    public ErrorMonotonicPipelineWiringTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), "MetBenchErrorMonotonicWiringTests", Guid.NewGuid().ToString("N"));
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
        // PR-Bol-2A: phase.Parameters override ctx.Parameters; this default lets phases
        // omit "factor" and inherit the no-op multiplier when they only care about
        // accumulating outputs (each phase's per-role identity is what matters here).
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

    private static (MrSpec spec, ErrorMonotonicPredicate predicate) BuildSpec()
    {
        var spec = TypedSpecFactory.ForErrorMonotonic(
            "mr-em", "k_eff",
            new[] { "coarse", "medium" }, "reference");
        var predicate = (ErrorMonotonicPredicate)spec.Predicates![0];
        return (spec, predicate);
    }

    private FakeProcessExecutor BuildHappyExecutor(Dictionary<string, double> phaseKeff)
    {
        // Returns "k_eff": phaseKeff[role] for each phase, monotonically approaching reference.
        return new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
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
                var outPath = ExtractArg(cmd, "--output \"");
                // Phase role inferred from input filename phase.in.{role}.json
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
    public async Task ExecuteMultiPhaseAsync_returns_error_when_typed_spec_is_missing()
    {
        var pipeline = new SystemMtPipeline(BuildHappyExecutor(new()));
        var ctx = MakeBaseContext(); // no TypedSpec attached
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string> { ["factor"] = "1" }),
            new RefinementPhase("reference", new Dictionary<string, string> { ["factor"] = "2" }),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Error, outcome.FinalStatus);
        Assert.Contains("TypedSpec", outcome.ErrorMessage!);
    }

    [Fact]
    public async Task ExecuteMultiPhaseAsync_dispatches_pre_built_typed_predicate()
    {
        var (spec, predicate) = BuildSpec();
        var pipeline = new SystemMtPipeline(BuildHappyExecutor(new()
        {
            ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44
        }));
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string> { ["factor"] = "1" }),
            new RefinementPhase("medium", new Dictionary<string, string> { ["factor"] = "2" }),
            new RefinementPhase("reference", new Dictionary<string, string> { ["factor"] = "4" }),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
        Assert.NotNull(outcome.TypedSpec);
        Assert.Same(spec, outcome.TypedSpec);
        Assert.NotNull(outcome.TypedPredicate);
        Assert.Same(predicate, outcome.TypedPredicate);
    }

    [Fact]
    public async Task ExecuteMultiPhaseAsync_passes_on_monotonic_error_decrease()
    {
        var (spec, predicate) = BuildSpec();
        var pipeline = new SystemMtPipeline(BuildHappyExecutor(new()
        {
            ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44
        }));
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
        Assert.True(outcome.AssertionResult!.Passed);
    }

    [Fact]
    public async Task ExecuteMultiPhaseAsync_anomaly_on_non_monotonic_error_increase()
    {
        var (spec, predicate) = BuildSpec();
        // coarse error = |1.43 - 1.44| = 0.01; medium error = |1.40 - 1.44| = 0.04 > 0.01 → fail
        var pipeline = new SystemMtPipeline(BuildHappyExecutor(new()
        {
            ["coarse"] = 1.43, ["medium"] = 1.40, ["reference"] = 1.44
        }));
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.Equal(PipelineStatus.Anomaly, outcome.FinalStatus);
        Assert.False(outcome.AssertionResult!.Passed);
    }

    [Fact]
    public async Task ExecuteMultiPhaseAsync_rejects_null_phases()
    {
        var pipeline = new SystemMtPipeline(BuildHappyExecutor(new()));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await pipeline.ExecuteMultiPhaseAsync(
                new MultiPhaseExecutionContext(MakeBaseContext(), Array.Empty<RefinementPhase>())));
    }

    [Fact]
    public async Task ExecuteMultiPhaseAsync_rejects_null_context()
    {
        var pipeline = new SystemMtPipeline(BuildHappyExecutor(new()));
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await pipeline.ExecuteMultiPhaseAsync(null!));
    }

    [Fact]
    public async Task Legacy_ExecuteAsync_remains_byte_identical_to_pre_PR_Bol_2A_for_2_side_MRs()
    {
        // Regression guard: PR-Bol-2A must NOT alter the source/followup 2-side flow. We use a
        // less-comparison MR and a fake executor that returns 1.13 vs 0.51 for source vs followup;
        // the legacy ExecuteAsync was green for this in PR-VR tests. Pin Ok + Passed here too.
        var sourceOut = new { values = new Dictionary<string, double> { ["k_eff"] = 1.13 }, metadata = new { } };
        var followupOut = new { values = new Dictionary<string, double> { ["k_eff"] = 0.51 }, metadata = new { } };
        var fake = new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
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
                var outPath = ExtractArg(cmd, "--output \"");
                var which = cmd.Contains("source.in.json") ? (object)sourceOut : followupOut;
                File.WriteAllText(outPath, JsonSerializer.Serialize(which));
                return new ProcessResult(0, "", "", TimeSpan.FromMilliseconds(20), false);
            }
            if (cmd.Contains("fake-output-parser"))
            {
                var outPath = ExtractArg(cmd, "--output-file \"");
                return new ProcessResult(0, File.ReadAllText(outPath), "", TimeSpan.FromMilliseconds(5), false);
            }
            return new ProcessResult(1, "", "Unknown", TimeSpan.Zero, false);
        });
        var pipeline = new SystemMtPipeline(fake);
        var ctx = MakeBaseContext() with
        {
            AssertionTypeCode = "less",
            Parameters = new Dictionary<string, string> { ["factor"] = "1.5" },
        };
        var outcome = await pipeline.ExecuteAsync(ctx);

        Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
        Assert.True(outcome.AssertionResult!.Passed);
        Assert.Null(outcome.PhaseMetrics);
    }

    [Fact]
    public async Task ExecuteMultiPhaseAsync_populates_PhaseMetrics_dict_keyed_by_role()
    {
        var (spec, predicate) = BuildSpec();
        var pipeline = new SystemMtPipeline(BuildHappyExecutor(new()
        {
            ["coarse"] = 1.40, ["medium"] = 1.43, ["reference"] = 1.44
        }));
        var ctx = MakeBaseContext() with { TypedSpec = spec, TypedPredicate = predicate };
        var mp = new MultiPhaseExecutionContext(ctx, new[]
        {
            new RefinementPhase("coarse", new Dictionary<string, string>()),
            new RefinementPhase("medium", new Dictionary<string, string>()),
            new RefinementPhase("reference", new Dictionary<string, string>()),
        });

        var outcome = await pipeline.ExecuteMultiPhaseAsync(mp);

        Assert.NotNull(outcome.PhaseMetrics);
        Assert.Equal(3, outcome.PhaseMetrics!.Count);
        Assert.Equal(1.40, outcome.PhaseMetrics["coarse"]["k_eff"], precision: 12);
        Assert.Equal(1.43, outcome.PhaseMetrics["medium"]["k_eff"], precision: 12);
        Assert.Equal(1.44, outcome.PhaseMetrics["reference"]["k_eff"], precision: 12);
    }
}
