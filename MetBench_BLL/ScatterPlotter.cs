using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using System.Diagnostics;

namespace MetBench_BLL
{
    public class ScatterPlotter<TDataRecord, TDataReader> : BasePlotter<TDataRecord, TDataReader>
       where TDataRecord : IDataRecord
       where TDataReader : IMTDataInterface<TDataRecord>
    {
        public override PlotType PlotType => PlotType.Scatter;

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
                    ConfigureAxes();
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
            return new[]
            {
                new Axis
                {
                    Name = "Y1",
                    NameTextSize = 12,
                    TextSize = 10,
                    LabelsRotation = 0,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightBlue, 1)
                }
            };
        }

        public override Axis[] GetYAxes()
        {
            return new[]
            {
                new Axis
                {
                    Name = "Value",
                    NameTextSize = 12,
                    TextSize = 10,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };
        }
        private List<ISeries> CreateSeries()
        {
            var xDef = _columnDefinitions.FirstOrDefault(c => c.Name == "Y1");
            var y2Def = _columnDefinitions.FirstOrDefault(c => c.Name == "Y2");
            var actualDef = _columnDefinitions.FirstOrDefault(c => c.Name == "Actual");

            if (xDef == null || y2Def == null || actualDef == null)
                throw new ArgumentException("Missing required column definitions (Y1, Y2, Actual)");

            var y2Points = new List<ObservablePoint>();
            var actualPoints = new List<ObservablePoint>();

            foreach (var record in _dataReader.Data.Where(r => r.Values != null))
            {
                try
                {
                    if (!record.Values.TryGetValue(xDef.Name, out var xObj)) continue;
                    var x = Convert.ToDouble(xObj);

                    if (record.Values.TryGetValue(y2Def.Name, out var y2Obj))
                        y2Points.Add(new ObservablePoint(x, Convert.ToDouble(y2Obj)));

                    if (record.Values.TryGetValue(actualDef.Name, out var actualObj))
                        actualPoints.Add(new ObservablePoint(x, Convert.ToDouble(actualObj)));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Record conversion failed: {ex.Message}");
                }
            }

            var series = new List<ISeries>();
            // 蓝色正方形系列（Y2数据）
            if (y2Points.Count > 0)
            {
                series.Add(new ScatterSeries<ObservablePoint, RectangleGeometry>
                {
                    Values = y2Points,
                    Name = "(Y1,Y2)",
                    Fill = new SolidColorPaint(SKColors.Transparent),
                    Stroke = new SolidColorPaint(SKColors.Blue, 1),
                    GeometrySize = 10
                });
            }

            // 红色圆形系列（Actual数据）
            if (actualPoints.Count > 0)
            {
                series.Add(new ScatterSeries<ObservablePoint, CircleGeometry>
                {
                    Values = actualPoints,
                    Name = "(Y1,Actual)",
                    Fill = new SolidColorPaint(SKColors.Red.WithAlpha(20)),
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    GeometrySize = 20
                });
            }

            return series;
        }

 

        private ISeries CreateScatterSeries(
      IEnumerable<ObservablePoint> points,
      string name,
      SKColor borderColor,
      float size,
      bool isRectangle = false)
        {
            // 创建只有边框的绘制配置
            var borderPaint = new SolidColorPaint(borderColor, 2); // 边框宽度2px
            var transparentFill = new SolidColorPaint(SKColors.Transparent); // 透明填充

            return isRectangle
                ? new ScatterSeries<ObservablePoint, RectangleGeometry>
                {
                    Values = points.ToList(),
                    Name = name,
                    Fill = transparentFill,    // 透明填充
                    Stroke = borderPaint,     // 边框颜色
                    GeometrySize = size       // 点的大小
                }
                : new ScatterSeries<ObservablePoint, CircleGeometry>
                {
                    Values = points.ToList(),
                    Name = name,
                    Fill = transparentFill,    // 透明填充
                    Stroke = borderPaint,     // 边框颜色
                    GeometrySize = size       // 点的大小
                };
        }

        private void ConfigureAxes()
        {
            if (_chart is CartesianChart cartesianChart)
            {
                cartesianChart.XAxes = GetXAxes();
                cartesianChart.YAxes = GetYAxes();
                cartesianChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Top;
                cartesianChart.LegendTextSize = 12;
            }
        }
    }

}
