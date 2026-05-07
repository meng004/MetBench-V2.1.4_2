using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace MetBench_BLL
{
    public class VisualizationDemo
    {
        private ISeries[] series;


        private Axis[] xAxes;


        private Axis[] yAxes;

        public  void RunDemo()
        {
            try
            {
                // 1. 定义列结构
                var columnDefinitions = new List<ColumnDefinition>
                {
                    new ColumnDefinition("X", typeof(float)),
                    new ColumnDefinition("Y2", typeof(float)),
                    new ColumnDefinition("Actual", typeof(float)),
                    new ColumnDefinition("IsPass", typeof(int)),

                };

                // 2. 创建数据读取器
                var csvReader = new CsvDataReader(@"C:\Users\suyongcheng\Desktop\cos_MR_visualization_20250410_100026.csv", columnDefinitions);

                // 3. 创建可视化引擎
                var visualizationEngine = new MTVisualizationEngine<CsvDataRecord, CsvDataReader>(
                    csvReader,
                    PlotType.Line);

                // 4. 加载并渲染数据
                visualizationEngine.Visualize();

                //5 
                var plotter = visualizationEngine.GetPlotter();
                xAxes = plotter.GetXAxes();
                yAxes = plotter.GetYAxes();
                series = plotter.GetSeries();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
