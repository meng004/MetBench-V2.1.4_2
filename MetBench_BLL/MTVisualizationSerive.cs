using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace MetBench_BLL
{
    public class MTVisualizationSerive
    {
        private ISeries[] _series;
        private Axis[] _xAxes;
        private Axis[] _yAxes;
        private MTVisualizationEngine<CsvDataRecord, CsvDataReader> _visualizationEngine;

        // 定义列结构（可配置化）
        private static readonly List<ColumnDefinition> ColumnDefinitions = new()
    {
        new ColumnDefinition("Y1", typeof(float)),
        new ColumnDefinition("Y2", typeof(float)),
        new ColumnDefinition("Actual", typeof(float)),
        new ColumnDefinition("IsPass", typeof(int)),
    };

        /// <summary>
        /// 初始化可视化引擎并加载数据
        /// </summary>
        public void Initialize(string csvFilePath, PlotType plotType = PlotType.Line)
        {
            // 也可以修改为IOC依赖注入
            try
            {
                // 1. 创建数据读取器
                var csvReader = new CsvDataReader(csvFilePath, ColumnDefinitions);

                // 2. 创建可视化引擎
                _visualizationEngine = new MTVisualizationEngine<CsvDataRecord, CsvDataReader>(
                    csvReader,
                    plotType);

                // 3. 渲染数据
                _visualizationEngine.Visualize();

                // 4. 获取绘图组件
                var plotter = _visualizationEngine.GetPlotter();
                _xAxes = plotter.GetXAxes();
                _yAxes = plotter.GetYAxes();
                _series = plotter.GetSeries();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw; // 抛出异常供 ViewModel 处理
            }
        }

        /// <summary>
        /// 获取可视化数据（供 ViewModel 调用）
        /// </summary>
        public (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) GetVisualizationData()
        {
            if (_series == null || _xAxes == null || _yAxes == null)
                throw new InvalidOperationException("Visualization data is not initialized. Call Initialize() first.");

            return (_series, _xAxes, _yAxes);
        }
    }
}
