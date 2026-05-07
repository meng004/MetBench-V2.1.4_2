using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using System.Diagnostics;

namespace MetBench_BLL
{


    public class PiePlotter<TDataRecord, TDataReader> : BasePlotter<TDataRecord, TDataReader>
       where TDataRecord : IDataRecord
       where TDataReader : IMTDataInterface<TDataRecord>
    {
        private static readonly SKColor PassColor = new SKColor(173, 216, 230); // 淡蓝色(LightBlue)
        private static readonly SKColor PassBorderColor = new SKColor(100, 149, 237); // 矢车菊蓝(CornflowerBlue)
        private static readonly SKColor PassHighlightColor = new SKColor(135, 206, 250); // 天空蓝(SkyBlue)
        private static readonly SKColor FailColor = new SKColor(255, 182, 193); // 粉红色
        private static readonly SKColor FailBorderColor = new SKColor(220, 110, 150); // 深粉红边框
        private static readonly SKColor LabelColor = SKColors.Black;
        // 虚线边框定义（未通过部分）
        private static readonly DashEffect DashBorderEffect = new DashEffect(new float[] { 4, 4 }, 0);
        public override PlotType PlotType => PlotType.Pie;

        public override void Plot()
        {
            if (_dataReader == null || _columnDefinitions == null)
                throw new InvalidOperationException("Data reader or column definitions not initialized");

            try
            {
                _dataReader.LoadData();
                var series = CreateSeries();

                if (series.Count > 0)
                {
                    _chart.Series = series;
                    //ConfigureChart();
                }
                else
                {
                    Debug.WriteLine("Warning: No valid data to plot");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Plotting failed: {ex}");
                throw;
            }
        }

        public override ISeries[] GetSeries()
        {
            return CreateSeries().ToArray();
        }

        public override Axis[] GetXAxes()
        {
            return Array.Empty<Axis>(); // 饼图不需要X轴
            //return null;
        }

        public override Axis[] GetYAxes()
        {
            return Array.Empty<Axis>(); // 饼图不需要Y轴
            //return null;
        }

        private List<ISeries> CreateSeries()
        {
            // 1. 检查IsPass字段定义
            var isPassDef = _columnDefinitions.FirstOrDefault(c => c.Name == "IsPass");
            if (isPassDef == null)
                throw new ArgumentException("Missing required column definition: IsPass");

            // 2. 统计通过/未通过数量
            int passCount = 0, failCount = 0;
            foreach (var record in _dataReader.Data.Where(r => r.Values != null))
            {
                try
                {
                    if (record.Values.TryGetValue(isPassDef.Name, out var isPassObj))
                    {
                        var isPass = Convert.ToInt32(isPassObj);
                        if (isPass == 1) passCount++;
                        else if (isPass == 0) failCount++;
                        else Debug.WriteLine($"Invalid IsPass value: {isPass}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Record conversion failed: {ex.Message}");
                }
            }

            // 3. 创建饼图系列
            var series = new List<ISeries>();
            if (passCount + failCount > 0)
            {
                // 通过部分（蓝色渐变）
                series.Add(new PieSeries<int>
                {
                    Values = new[] { passCount },
                    Name = "Pass",
                    Fill = new LinearGradientPaint(
                        new SKColor(220, 240, 255), // 更淡的蓝色
                        PassColor,                 // 基础淡蓝色
                        new SKPoint(0, 0),
                        new SKPoint(1, 1)),
                    Stroke = new SolidColorPaint(PassBorderColor, 2), //
                    DataLabelsPaint = new SolidColorPaint(LabelColor),
                    
                    HoverPushout = 12,
                    IsHoverable = true,
                   
                });

                // 未通过部分（粉红色）
                series.Add(new PieSeries<int>
                {
                    Values = new[] { failCount },
                    Name = "Fail",
                    Fill = new SolidColorPaint(FailColor.WithAlpha(180)), // 半透明粉红
                    Stroke = new SolidColorPaint(FailBorderColor, 2)
                    {
                        PathEffect = DashBorderEffect // 关键：应用虚线效果
                    },
                    DataLabelsPaint = new SolidColorPaint(LabelColor),
                    
                    HoverPushout = 12,
                    IsHoverable = true,

                });
            }

            return series;
        }
        private IPieChartView PieChart => _chart as IPieChartView;
        private void ConfigureChart()
        {
            if (PieChart!=null)
            {
                // 基础配置
                PieChart.InitialRotation = -90;     // 起始角度（0=右侧开始）
                PieChart.MaxAngle = 360;          // 完整圆形
                                         // 按百分比显示

                // 图例样式
                PieChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right;
                PieChart.LegendTextSize = 12;
                PieChart.LegendBackgroundPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(150));

                // 交互效果
              
                PieChart.AnimationsSpeed = TimeSpan.FromMilliseconds(600);
                PieChart.EasingFunction = EasingFunctions.ElasticOut;
                // 视觉优化

                PieChart.Title = new LabelVisual
                {
                    Text = "MTExecutionResult",
                    TextSize = 16,
                    Paint = new SolidColorPaint(SKColors.DarkSlateGray)
                };


            }

      

    }
      
    }
}
