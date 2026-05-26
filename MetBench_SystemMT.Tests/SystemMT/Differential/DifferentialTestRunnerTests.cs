using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Differential;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Differential;

/// <summary>
/// PR-B T1 cross-method differential runner contract tests.
///
/// The runner orchestrates two <see cref="ISystemMtLauncher.RunAsync"/> calls and applies a
/// deterministic agreement criterion over the two <see cref="MrRunResult"/> objects. These
/// tests pin every input the runner can see so the contract is total — no fall-through
/// case can be reached at runtime.
/// </summary>
public sealed class DifferentialTestRunnerTests
{
    // ---- Fakes & helpers ---------------------------------------------------------

    /// <summary>
    /// Fake launcher that returns canned <see cref="MrRunResult"/> per MR id and records
    /// every <see cref="RunAsync"/> invocation. Mirrors the in-memory <c>FakeExecRepo</c>
    /// pattern already used by <c>LauncherEndToEnd*Tests.cs</c>.
    /// </summary>
    private sealed class FakeLauncher : ISystemMtLauncher
    {
        public Dictionary<string, Func<IReadOnlyDictionary<string, string>?, MrRunResult>> Returns { get; } = new();
        public Dictionary<string, Exception> Throws { get; } = new();
        public List<(string MrId, IReadOnlyDictionary<string, string>? Overrides)> Calls { get; } = new();

        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException("not exercised by the differential runner");

        public Task<MrRunResult> RunAsync(
            string mrId,
            IReadOnlyDictionary<string, string>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((mrId, parameterOverrides));
            if (Throws.TryGetValue(mrId, out var ex))
                throw ex;
            if (!Returns.TryGetValue(mrId, out var factory))
                throw new InvalidOperationException($"FakeLauncher has no canned result for '{mrId}'.");
            return Task.FromResult(factory(parameterOverrides));
        }

        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
            IReadOnlyList<BatchMrRunRequest> requests,
            IProgress<BatchProgress>? progress = null,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException("not exercised by the differential runner");
    }

    private static MrRunResult MakeResult(
        string mrId,
        bool passed = true,
        string valueName = "k_eff",
        double sourceValue = 1.0,
        double followUpValue = 1.2,
        string failureReason = "")
        => new(
            RecordId: $"rec-{Guid.NewGuid():N}",
            MrId: mrId,
            Passed: passed,
            FailureReason: failureReason,
            ValueName: valueName,
            SourceValue: sourceValue,
            FollowUpValue: followUpValue,
            SourceElapsed: TimeSpan.FromMilliseconds(10),
            FollowUpElapsed: TimeSpan.FromMilliseconds(10));

    private static DifferentialRunRequest Req(
        string left = "openmoc-pincell-nu-sigma-f",
        string right = "openmc-pincell-nu-sigma-f",
        DifferentialAgreementCriterion criterion = DifferentialAgreementCriterion.BothPassed,
        double tolerance = 0.0,
        IReadOnlyDictionary<string, string>? leftOverrides = null,
        IReadOnlyDictionary<string, string>? rightOverrides = null)
        => new(left, right, criterion, tolerance, leftOverrides, rightOverrides);

    private static FakeLauncher LauncherWith(MrRunResult left, MrRunResult right)
    {
        var fake = new FakeLauncher();
        fake.Returns[left.MrId] = _ => left;
        // Distinct MR ids only; for same-MR-both-sides see the dedicated fact.
        if (!fake.Returns.ContainsKey(right.MrId))
            fake.Returns[right.MrId] = _ => right;
        return fake;
    }

    // ---- Happy paths -------------------------------------------------------------

    [Fact]
    public async Task BothPassed_returns_Agree_when_both_results_passed()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", passed: true);
        var right = MakeResult("openmc-pincell-nu-sigma-f", passed: true);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.BothPassed));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status);
        Assert.Equal(DifferentialDisagreementReason.None, result.Reason);
        Assert.Null(result.Diagnostic);
        Assert.Equal(left, result.Left);
        Assert.Equal(right, result.Right);
    }

    [Fact]
    public async Task DirectionConcordant_returns_Agree_when_both_FollowUp_greater_than_Source()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 1.20);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 1.1, followUpValue: 1.25);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.DirectionConcordant));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status);
    }

    [Fact]
    public async Task DirectionConcordant_returns_Agree_when_both_FollowUp_less_than_Source()
    {
        var left = MakeResult("openmoc-pincell-sigma-a", sourceValue: 1.0, followUpValue: 0.8);
        var right = MakeResult("openmc-pincell-sigma-a", sourceValue: 1.0, followUpValue: 0.9);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(
            left: "openmoc-pincell-sigma-a",
            right: "openmc-pincell-sigma-a",
            criterion: DifferentialAgreementCriterion.DirectionConcordant));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status);
    }

    [Fact]
    public async Task DirectionConcordant_returns_Agree_when_both_differences_are_exactly_zero()
    {
        // sign(0) == sign(0); both runs are flat-line, treated as Agree (the metric
        // moved in the same direction: neither did).
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 1.0);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 1.0);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.DirectionConcordant));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status);
    }

    [Fact]
    public async Task FollowUpRatioWithinTolerance_returns_Agree_when_ratios_match_within_tolerance()
    {
        // ratio left  = 1.20 / 1.00 = 1.20
        // ratio right = 1.25 / 1.05 ≈ 1.190476
        // |1.20 - 1.190476| ≈ 0.0095, tolerance 0.02 → Agree.
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 1.00, followUpValue: 1.20);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 1.05, followUpValue: 1.25);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(
            criterion: DifferentialAgreementCriterion.FollowUpRatioWithinTolerance,
            tolerance: 0.02));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status);
    }

    // ---- Disagreement paths ------------------------------------------------------

    [Fact]
    public async Task BothPassed_returns_Disagree_BothFailed_when_neither_passed()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", passed: false, failureReason: "left failed");
        var right = MakeResult("openmc-pincell-nu-sigma-f", passed: false, failureReason: "right failed");
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.BothPassed));

        Assert.Equal(DifferentialAgreementStatus.Disagree, result.Status);
        Assert.Equal(DifferentialDisagreementReason.BothFailed, result.Reason);
        Assert.NotNull(result.Diagnostic);
    }

    [Fact]
    public async Task BothPassed_returns_Disagree_LeftFailed_when_only_left_failed()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", passed: false, failureReason: "left failed");
        var right = MakeResult("openmc-pincell-nu-sigma-f", passed: true);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.BothPassed));

        Assert.Equal(DifferentialAgreementStatus.Disagree, result.Status);
        Assert.Equal(DifferentialDisagreementReason.LeftFailed, result.Reason);
    }

    [Fact]
    public async Task BothPassed_returns_Disagree_RightFailed_when_only_right_failed()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", passed: true);
        var right = MakeResult("openmc-pincell-nu-sigma-f", passed: false, failureReason: "right failed");
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.BothPassed));

        Assert.Equal(DifferentialAgreementStatus.Disagree, result.Status);
        Assert.Equal(DifferentialDisagreementReason.RightFailed, result.Reason);
    }

    [Fact]
    public async Task DirectionConcordant_returns_Disagree_when_directions_differ()
    {
        // left increases (1.0 → 1.2), right decreases (1.0 → 0.9) — directions disagree.
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 1.2);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 0.9);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.DirectionConcordant));

        Assert.Equal(DifferentialAgreementStatus.Disagree, result.Status);
        Assert.Equal(DifferentialDisagreementReason.DirectionsDisagreed, result.Reason);
    }

    [Fact]
    public async Task FollowUpRatioWithinTolerance_returns_Disagree_when_ratio_gap_exceeds_tolerance()
    {
        // ratio left  = 2.0 / 1.0 = 2.0
        // ratio right = 1.1 / 1.0 = 1.1
        // |2.0 - 1.1| = 0.9, tolerance 0.05 → RatioOutsideTolerance.
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 2.0);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 1.1);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(
            criterion: DifferentialAgreementCriterion.FollowUpRatioWithinTolerance,
            tolerance: 0.05));

        Assert.Equal(DifferentialAgreementStatus.Disagree, result.Status);
        Assert.Equal(DifferentialDisagreementReason.RatioOutsideTolerance, result.Reason);
    }

    // ---- Inconclusive paths ------------------------------------------------------

    [Fact]
    public async Task Metric_name_mismatch_returns_Inconclusive_MetricNameMismatch()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", valueName: "k_eff");
        var right = MakeResult("openmc-pincell-nu-sigma-f", valueName: "phi_max");
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req());

        Assert.Equal(DifferentialAgreementStatus.Inconclusive, result.Status);
        Assert.Equal(DifferentialDisagreementReason.MetricNameMismatch, result.Reason);
        Assert.Contains("k_eff", result.Diagnostic!);
        Assert.Contains("phi_max", result.Diagnostic!);
    }

    [Theory]
    [InlineData(double.NaN, 1.0, 1.0, 1.2)]
    [InlineData(1.0, double.NaN, 1.0, 1.2)]
    [InlineData(1.0, 1.2, double.NaN, 1.2)]
    [InlineData(1.0, 1.2, 1.0, double.NaN)]
    public async Task NaN_in_any_value_returns_Inconclusive_NonFiniteValue(
        double leftSrc, double leftFu, double rightSrc, double rightFu)
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: leftSrc, followUpValue: leftFu);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: rightSrc, followUpValue: rightFu);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.DirectionConcordant));

        Assert.Equal(DifferentialAgreementStatus.Inconclusive, result.Status);
        Assert.Equal(DifferentialDisagreementReason.NonFiniteValue, result.Reason);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task Infinity_in_any_value_returns_Inconclusive_NonFiniteValue(double bad)
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: bad);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 1.2);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.DirectionConcordant));

        Assert.Equal(DifferentialAgreementStatus.Inconclusive, result.Status);
        Assert.Equal(DifferentialDisagreementReason.NonFiniteValue, result.Reason);
    }

    [Fact]
    public async Task Zero_source_under_RatioWithinTolerance_returns_Inconclusive_NonFiniteValue()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 0.0, followUpValue: 1.0);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 1.0, followUpValue: 1.0);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(
            criterion: DifferentialAgreementCriterion.FollowUpRatioWithinTolerance,
            tolerance: 0.01));

        Assert.Equal(DifferentialAgreementStatus.Inconclusive, result.Status);
        Assert.Equal(DifferentialDisagreementReason.NonFiniteValue, result.Reason);
        Assert.Contains("zero source", result.Diagnostic!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Zero_source_under_BothPassed_is_allowed_no_division_required()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 0.0, followUpValue: 1.0, passed: true);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 0.0, followUpValue: 1.0, passed: true);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.BothPassed));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status);
    }

    [Fact]
    public async Task Zero_source_under_DirectionConcordant_is_allowed_no_division_required()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f", sourceValue: 0.0, followUpValue: 1.0);
        var right = MakeResult("openmc-pincell-nu-sigma-f", sourceValue: 0.0, followUpValue: 0.5);
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(criterion: DifferentialAgreementCriterion.DirectionConcordant));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status); // both diffs > 0
    }

    [Fact]
    public async Task Negative_tolerance_returns_Inconclusive_ToleranceNegative()
    {
        var left = MakeResult("openmoc-pincell-nu-sigma-f");
        var right = MakeResult("openmc-pincell-nu-sigma-f");
        var runner = new DifferentialTestRunner(LauncherWith(left, right));

        var result = await runner.RunPairAsync(Req(
            criterion: DifferentialAgreementCriterion.FollowUpRatioWithinTolerance,
            tolerance: -0.01));

        Assert.Equal(DifferentialAgreementStatus.Inconclusive, result.Status);
        Assert.Equal(DifferentialDisagreementReason.ToleranceNegative, result.Reason);
    }

    // ---- Launcher integration semantics -----------------------------------------

    [Fact]
    public async Task Launcher_throws_propagates_without_swallow()
    {
        var fake = new FakeLauncher();
        fake.Throws["openmoc-pincell-nu-sigma-f"] = new InvalidOperationException("simulated launcher failure");
        fake.Returns["openmc-pincell-nu-sigma-f"] = _ => MakeResult("openmc-pincell-nu-sigma-f");
        var runner = new DifferentialTestRunner(fake);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunPairAsync(Req()));
    }

    [Fact]
    public async Task Request_null_throws_ArgumentNullException()
    {
        var runner = new DifferentialTestRunner(new FakeLauncher());

        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunPairAsync(null!));
    }

    [Fact]
    public async Task Launcher_null_in_constructor_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DifferentialTestRunner(null!));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Same_MR_id_on_both_sides_is_permitted_and_runs_launcher_twice()
    {
        var fake = new FakeLauncher();
        var counter = 0;
        fake.Returns["openmoc-pincell-nu-sigma-f"] = _ =>
        {
            counter++;
            return MakeResult("openmoc-pincell-nu-sigma-f", followUpValue: 1.0 + counter * 0.1);
        };
        var runner = new DifferentialTestRunner(fake);

        var result = await runner.RunPairAsync(Req(
            left: "openmoc-pincell-nu-sigma-f",
            right: "openmoc-pincell-nu-sigma-f",
            criterion: DifferentialAgreementCriterion.BothPassed));

        Assert.Equal(DifferentialAgreementStatus.Agree, result.Status);
        Assert.Equal(2, counter);
        Assert.Equal(2, fake.Calls.Count);
    }

    [Fact]
    public async Task Parameter_overrides_are_forwarded_per_side()
    {
        var fake = new FakeLauncher();
        fake.Returns["openmoc-pincell-nu-sigma-f"] = _ => MakeResult("openmoc-pincell-nu-sigma-f");
        fake.Returns["openmc-pincell-nu-sigma-f"] = _ => MakeResult("openmc-pincell-nu-sigma-f");
        var leftOverrides = new Dictionary<string, string> { ["factor"] = "1.5" };
        var rightOverrides = new Dictionary<string, string> { ["factor"] = "2.0" };
        var runner = new DifferentialTestRunner(fake);

        await runner.RunPairAsync(Req(
            leftOverrides: leftOverrides,
            rightOverrides: rightOverrides));

        Assert.Equal(2, fake.Calls.Count);
        Assert.Equal("openmoc-pincell-nu-sigma-f", fake.Calls[0].MrId);
        Assert.Same(leftOverrides, fake.Calls[0].Overrides);
        Assert.Equal("openmc-pincell-nu-sigma-f", fake.Calls[1].MrId);
        Assert.Same(rightOverrides, fake.Calls[1].Overrides);
    }

    [Fact]
    public async Task CancellationToken_is_observed_when_launcher_respects_it()
    {
        var fake = new FakeLauncher();
        fake.Throws["openmoc-pincell-nu-sigma-f"] = new OperationCanceledException();
        var runner = new DifferentialTestRunner(fake);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            runner.RunPairAsync(Req(), CancellationToken.None));
    }

    [Fact]
    public async Task Left_runs_before_right_for_predictable_diagnostics()
    {
        var fake = new FakeLauncher();
        fake.Returns["openmoc-pincell-nu-sigma-f"] = _ => MakeResult("openmoc-pincell-nu-sigma-f");
        fake.Returns["openmc-pincell-nu-sigma-f"] = _ => MakeResult("openmc-pincell-nu-sigma-f");
        var runner = new DifferentialTestRunner(fake);

        await runner.RunPairAsync(Req());

        Assert.Equal(2, fake.Calls.Count);
        Assert.Equal("openmoc-pincell-nu-sigma-f", fake.Calls[0].MrId);  // left first
        Assert.Equal("openmc-pincell-nu-sigma-f", fake.Calls[1].MrId);   // right second
    }
}
