using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MetBench_BLL;
using MetBench_BLL.Visualization;
using Xunit;

namespace MetBench_SystemMT.Tests.Bll;

/// <summary>
/// G-02: MTVisualizationService 跨平台数据层单测（不依赖 WPF chart 控件，
/// 验证 CSV → series/axes 三元组形态）。
/// </summary>
public sealed class MtVisualizationServiceTests : IDisposable
{
    private readonly string _csvPath;

    public MtVisualizationServiceTests()
    {
        // 4 列：Y1, Y2, Actual, IsPass（与 ColumnDefinitions 对齐）+ 5 行
        _csvPath = Path.Combine(Path.GetTempPath(),
            $"metbench-viz-test-{Guid.NewGuid():N}.csv");
        File.WriteAllText(_csvPath,
            "Y1,Y2,Actual,IsPass\n" +
            "1.0,2.0,1.5,1\n" +
            "2.0,4.0,3.0,1\n" +
            "3.0,6.0,4.5,0\n" +
            "4.0,8.0,6.0,1\n" +
            "5.0,10.0,7.5,0\n");
    }

    public void Dispose()
    {
        if (File.Exists(_csvPath)) File.Delete(_csvPath);
    }

    [Fact]
    public void GetVisualizationData_throws_when_Initialize_not_called()
    {
        var svc = new MTVisualizationService();
        var ex = Assert.Throws<InvalidOperationException>(() => svc.GetVisualizationData());
        Assert.Contains("Initialize", ex.Message);
    }

    [Fact]
    public void Initialize_Line_yields_nonempty_series_and_axes()
    {
        var svc = new MTVisualizationService();
        svc.Initialize(_csvPath, PlotType.Line);

        var (series, xAxes, yAxes) = svc.GetVisualizationData();

        Assert.NotEmpty(series);
        Assert.NotEmpty(xAxes);
        Assert.NotEmpty(yAxes);
        Assert.All(series, s => Assert.NotNull(s));
    }

    [Fact]
    public void Initialize_Scatter_yields_axes_aligned_with_Line()
    {
        var svc = new MTVisualizationService();
        svc.Initialize(_csvPath, PlotType.Scatter);

        var (series, xAxes, yAxes) = svc.GetVisualizationData();

        Assert.NotEmpty(series);
        Assert.NotEmpty(xAxes);
        // ScatterYAxes 等同 LineYAxes（see SeriesBuilder.BuildScatterYAxes）
        Assert.Equal(SeriesBuilder.BuildLineYAxes().Length, yAxes.Length);
    }

    [Fact]
    public void Initialize_Pie_yields_pie_series()
    {
        var svc = new MTVisualizationService();
        svc.Initialize(_csvPath, PlotType.Pie);

        var (series, _, _) = svc.GetVisualizationData();
        Assert.NotEmpty(series);
    }

    [Fact]
    public void Initialize_unsupported_PlotType_throws_NotSupportedException()
    {
        var svc = new MTVisualizationService();
        // 用强制 cast 制造非法 enum 值
        Assert.Throws<NotSupportedException>(() =>
            svc.Initialize(_csvPath, (PlotType)999));
    }

    [Fact]
    public void Initialize_repeats_replaces_previous_data()
    {
        var svc = new MTVisualizationService();
        svc.Initialize(_csvPath, PlotType.Line);
        var (firstSeries, _, _) = svc.GetVisualizationData();

        // 二次 Initialize 应替换原数据
        svc.Initialize(_csvPath, PlotType.Pie);
        var (secondSeries, _, _) = svc.GetVisualizationData();

        // 形态不同（line 多列 series vs pie 单 series）
        Assert.NotSame(firstSeries, secondSeries);
    }
}
