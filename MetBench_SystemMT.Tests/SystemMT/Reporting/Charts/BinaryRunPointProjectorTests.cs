using System;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting.Charts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting.Charts;

public sealed class BinaryRunPointProjectorTests
{
    private static SystemMtResultRecord MakeRecord(
        string mrName = "openmoc-pincell-nu-sigma-f",
        string valueName = "k_eff",
        double source = 1.0,
        double followUp = 1.5)
        => new()
        {
            Id = Guid.NewGuid(),
            MrName = mrName,
            ValueName = valueName,
            SourceValue = source,
            FollowUpValue = followUp,
            Passed = followUp > source,
            RunAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public void Project_returns_figure_with_two_points_in_binary_scatter_kind()
    {
        var fig = BinaryRunPointProjector.Project(MakeRecord());

        Assert.Equal(ChartFigureKind.BinaryScatter, fig.Kind);
        var series = Assert.Single(fig.SeriesList);
        Assert.Equal(2, series.Points.Count);
    }

    [Fact]
    public void Project_orders_source_at_x_zero_and_followup_at_x_one()
    {
        var record = MakeRecord(source: 1.23, followUp: 4.56);

        var fig = BinaryRunPointProjector.Project(record);
        var pts = fig.SeriesList[0].Points;

        Assert.Equal(0.0, pts[0].X);
        Assert.Equal(1.23, pts[0].Y);
        Assert.Equal("source", pts[0].Label);
        Assert.Equal(1.0, pts[1].X);
        Assert.Equal(4.56, pts[1].Y);
        Assert.Equal("follow-up", pts[1].Label);
    }

    [Fact]
    public void Project_uses_value_name_for_y_axis_label()
    {
        var fig = BinaryRunPointProjector.Project(MakeRecord(valueName: "k_eff_std"));

        Assert.Equal("k_eff_std", fig.YAxisLabel);
        Assert.Contains("k_eff_std", fig.Title);
    }

    [Fact]
    public void Project_falls_back_to_value_when_value_name_is_blank()
    {
        var fig = BinaryRunPointProjector.Project(MakeRecord(valueName: ""));

        Assert.Equal("value", fig.YAxisLabel);
    }

    [Fact]
    public void Project_falls_back_to_unknown_mr_when_mr_name_is_blank()
    {
        var fig = BinaryRunPointProjector.Project(MakeRecord(mrName: "   "));

        Assert.Contains("(unknown MR)", fig.Title);
    }

    [Fact]
    public void Project_handles_zero_source_value_without_throwing()
    {
        var fig = BinaryRunPointProjector.Project(MakeRecord(source: 0.0, followUp: 1.0));

        Assert.Equal(0.0, fig.SeriesList[0].Points[0].Y);
    }

    [Fact]
    public void Project_preserves_NaN_and_infinity_in_data_layer()
    {
        // Policy: data layer is faithful; downstream renderer decides how to
        // visually represent non-finite values. Do NOT silently coerce.
        var fig = BinaryRunPointProjector.Project(
            MakeRecord(source: double.NaN, followUp: double.PositiveInfinity));

        Assert.True(double.IsNaN(fig.SeriesList[0].Points[0].Y));
        Assert.True(double.IsPositiveInfinity(fig.SeriesList[0].Points[1].Y));
    }

    [Fact]
    public void Project_throws_on_null_record()
    {
        Assert.Throws<ArgumentNullException>(() => BinaryRunPointProjector.Project(null!));
    }
}
