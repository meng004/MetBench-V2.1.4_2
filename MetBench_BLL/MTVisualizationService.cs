using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MetBench_BLL.Visualization;

namespace MetBench_BLL
{
    /// <summary>
    /// 跨平台可视化数据服务 —— 读 CSV → 返回 <see cref="ISeries"/>[] + <see cref="Axis"/>[]。
    /// UI 侧（WPF / Web）拿这三个数组自己塞进 chart 控件，不再经过 WPF-only Engine。
    /// </summary>
    public class MTVisualizationService
    {
        private ISeries[]? _series;
        private Axis[]? _xAxes;
        private Axis[]? _yAxes;

        // 列结构（可后续抽到配置文件）
        private static readonly List<ColumnDefinition> ColumnDefinitions = new()
        {
            new ColumnDefinition("Y1", typeof(float)),
            new ColumnDefinition("Y2", typeof(float)),
            new ColumnDefinition("Actual", typeof(float)),
            new ColumnDefinition("IsPass", typeof(int)),
        };

        /// <summary>读 CSV → 按 plotType 构造 series/axes。失败抛异常给上层处理。</summary>
        public void Initialize(string csvFilePath, PlotType plotType = PlotType.Line)
        {
            var csvReader = new CsvDataReader(csvFilePath, ColumnDefinitions);
            csvReader.LoadData();

            (_series, _xAxes, _yAxes) = plotType switch
            {
                PlotType.Line => (
                    SeriesBuilder.BuildLineSeries(csvReader, ColumnDefinitions),
                    SeriesBuilder.BuildLineXAxes(),
                    SeriesBuilder.BuildLineYAxes()),
                PlotType.Scatter => (
                    SeriesBuilder.BuildScatterSeries(csvReader, ColumnDefinitions),
                    SeriesBuilder.BuildScatterXAxes(),
                    SeriesBuilder.BuildScatterYAxes()),
                PlotType.Pie => (
                    SeriesBuilder.BuildPieSeries(csvReader, ColumnDefinitions),
                    SeriesBuilder.BuildPieXAxes(),
                    SeriesBuilder.BuildPieYAxes()),
                _ => throw new NotSupportedException($"Plotter for {plotType} is not supported"),
            };
        }

        /// <summary>取构造好的 series + axes 三元组（ViewModel 调）。</summary>
        public (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) GetVisualizationData()
        {
            if (_series == null || _xAxes == null || _yAxes == null)
                throw new InvalidOperationException("Visualization data is not initialized. Call Initialize() first.");
            return (_series, _xAxes, _yAxes);
        }
    }
}
