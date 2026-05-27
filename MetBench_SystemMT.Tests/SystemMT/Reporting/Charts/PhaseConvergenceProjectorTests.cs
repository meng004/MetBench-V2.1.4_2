using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Reporting.Charts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting.Charts;

public sealed class PhaseConvergenceProjectorTests
{
    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> MakePhases(
        params (string Role, double Value)[] phases)
    {
        var outer = new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal);
        foreach (var (role, value) in phases)
        {
            outer[role] = new Dictionary<string, double> { ["k_eff"] = value };
        }
        return outer;
    }

    [Fact]
    public void Project_three_phases_produces_three_ordered_points()
    {
        var phases = MakePhases(("coarse", 1.42), ("medium", 1.43), ("reference", 1.4317));

        var fig = PhaseConvergenceProjector.Project(phases, "openmoc-pincell-ray-track-convergence", "k_eff");

        Assert.Equal(ChartFigureKind.PhaseLine, fig.Kind);
        var pts = fig.SeriesList[0].Points;
        Assert.Equal(3, pts.Count);
        Assert.Equal(new[] { "coarse", "medium", "reference" }, new[] { pts[0].Label, pts[1].Label, pts[2].Label });
        Assert.Equal(new[] { 0.0, 1.0, 2.0 }, new[] { pts[0].X, pts[1].X, pts[2].X });
        Assert.Equal(new[] { 1.42, 1.43, 1.4317 }, new[] { pts[0].Y, pts[1].Y, pts[2].Y });
    }

    [Fact]
    public void Project_n_phases_with_n_geq_four_works()
    {
        var phases = MakePhases(("p0", 1.0), ("p1", 1.1), ("p2", 1.2), ("p3", 1.25), ("p4", 1.27));

        var fig = PhaseConvergenceProjector.Project(phases, "synthetic-mr", "k_eff");

        Assert.Equal(5, fig.SeriesList[0].Points.Count);
    }

    [Fact]
    public void Project_uses_metric_name_for_y_axis_label_and_title()
    {
        var phases = MakePhases(("a", 1.0), ("b", 2.0));

        var fig = PhaseConvergenceProjector.Project(phases, "mr-x", "k_eff");

        Assert.Equal("k_eff", fig.YAxisLabel);
        Assert.Contains("k_eff", fig.Title);
        Assert.Contains("mr-x", fig.Title);
    }

    [Fact]
    public void Project_throws_when_single_phase_because_convergence_is_meaningless()
    {
        var phases = MakePhases(("only", 1.0));

        var ex = Assert.Throws<ArgumentException>(
            () => PhaseConvergenceProjector.Project(phases, "mr-x", "k_eff"));
        Assert.Contains("at least 2 phases", ex.Message);
    }

    [Fact]
    public void Project_throws_when_phase_missing_metric_with_diagnostic_listing_available_keys()
    {
        var outer = new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal)
        {
            ["coarse"] = new Dictionary<string, double> { ["k_eff"] = 1.0 },
            ["medium"] = new Dictionary<string, double> { ["other_metric"] = 2.0 },
        };

        var ex = Assert.Throws<ArgumentException>(
            () => PhaseConvergenceProjector.Project(outer, "mr-x", "k_eff"));
        Assert.Contains("Phase 'medium'", ex.Message);
        Assert.Contains("other_metric", ex.Message);
    }

    [Fact]
    public void Project_throws_on_null_phase_metrics()
    {
        Assert.Throws<ArgumentNullException>(
            () => PhaseConvergenceProjector.Project(null!, "mr-x", "k_eff"));
    }

    [Fact]
    public void Project_throws_on_blank_mr_id()
    {
        var phases = MakePhases(("a", 1.0), ("b", 2.0));

        Assert.Throws<ArgumentException>(() => PhaseConvergenceProjector.Project(phases, "", "k_eff"));
        Assert.Throws<ArgumentException>(() => PhaseConvergenceProjector.Project(phases, "   ", "k_eff"));
    }

    [Fact]
    public void Project_throws_on_blank_metric()
    {
        var phases = MakePhases(("a", 1.0), ("b", 2.0));

        Assert.Throws<ArgumentException>(() => PhaseConvergenceProjector.Project(phases, "mr-x", ""));
        Assert.Throws<ArgumentException>(() => PhaseConvergenceProjector.Project(phases, "mr-x", "   "));
    }

    [Fact]
    public void Project_preserves_dictionary_insertion_order_for_phase_roles()
    {
        // Plan convention from PR-Bol-2A: last phase = ReferenceRole; earlier in declared order.
        // Insertion order of the input dict drives the rendered axis order.
        var phases = MakePhases(("reference", 1.4317), ("coarse", 1.42), ("medium", 1.43));

        var fig = PhaseConvergenceProjector.Project(phases, "mr-x", "k_eff");
        var pts = fig.SeriesList[0].Points;

        Assert.Equal("reference", pts[0].Label);
        Assert.Equal("coarse", pts[1].Label);
        Assert.Equal("medium", pts[2].Label);
    }
}
