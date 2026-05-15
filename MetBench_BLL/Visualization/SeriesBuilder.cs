using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System.Diagnostics;

namespace MetBench_BLL.Visualization
{
    /// <summary>
    /// 把数据 → LiveCharts <see cref="ISeries"/> + <see cref="Axis"/> 数组的纯计算助手。
    /// 完全跨平台 —— 不引用任何 WPF / WindowsDesktop 类型，可在 Linux CI / VM 端通用。
    /// UI 侧 plotter（Client/Services/Plotting/）若想 DRY，可委派本类构造系列再喂给
    /// LiveChartsCore.SkiaSharpView.WPF 的 CartesianChart / PieChart 控件。
    /// </summary>
    public static class SeriesBuilder
    {
        // ===== Line =====

        public static ISeries[] BuildLineSeries(IMTDataInterface<CsvDataRecord> reader, IReadOnlyList<ColumnDefinition> columns)
        {
            var (y2Points, actualPoints) = ExtractXyPoints(reader, columns);
            var series = new List<ISeries>();

            if (y2Points.Count > 0)
            {
                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = y2Points,
                    Name = "(Y1,Y2)",
                    Stroke = new SolidColorPaint(SKColors.Blue, 3),
                    LineSmoothness = 10,
                    GeometrySize = 10,
                    GeometryStroke = new SolidColorPaint(SKColors.Blue, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                });
            }
            if (actualPoints.Count > 0)
            {
                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = actualPoints,
                    Name = "(Y1,Actual)",
                    Stroke = new SolidColorPaint(SKColors.Red, 2)
                    {
                        PathEffect = new DashEffect(new float[] { 6, 6 }, 0)
                    },
                    LineSmoothness = 10,
                    GeometrySize = 10,
                    GeometryStroke = new SolidColorPaint(SKColors.Red, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                });
            }
            return series.ToArray();
        }

        public static Axis[] BuildLineXAxes() => new[]
        {
            new Axis
            {
                Name = "Y1", NameTextSize = 12, TextSize = 10, LabelsRotation = 0,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
            }
        };

        public static Axis[] BuildLineYAxes() => new[]
        {
            new Axis
            {
                Name = "Value", NameTextSize = 12, TextSize = 10,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1),
            }
        };

        // ===== Scatter =====

        public static ISeries[] BuildScatterSeries(IMTDataInterface<CsvDataRecord> reader, IReadOnlyList<ColumnDefinition> columns)
        {
            var (y2Points, actualPoints) = ExtractXyPoints(reader, columns);
            var series = new List<ISeries>();

            if (y2Points.Count > 0)
            {
                series.Add(new ScatterSeries<ObservablePoint, LiveChartsCore.SkiaSharpView.Drawing.Geometries.RectangleGeometry>
                {
                    Values = y2Points,
                    Name = "(Y1,Y2)",
                    Fill = new SolidColorPaint(SKColors.Transparent),
                    Stroke = new SolidColorPaint(SKColors.Blue, 1),
                    GeometrySize = 10,
                });
            }
            if (actualPoints.Count > 0)
            {
                series.Add(new ScatterSeries<ObservablePoint, LiveChartsCore.SkiaSharpView.Drawing.Geometries.CircleGeometry>
                {
                    Values = actualPoints,
                    Name = "(Y1,Actual)",
                    Fill = new SolidColorPaint(SKColors.Red.WithAlpha(20)),
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    GeometrySize = 20,
                });
            }
            return series.ToArray();
        }

        public static Axis[] BuildScatterXAxes() => new[]
        {
            new Axis
            {
                Name = "Y1", NameTextSize = 12, TextSize = 10, LabelsRotation = 0,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightBlue, 1),
            }
        };

        public static Axis[] BuildScatterYAxes() => BuildLineYAxes();

        // ===== Pie =====

        public static ISeries[] BuildPieSeries(IMTDataInterface<CsvDataRecord> reader, IReadOnlyList<ColumnDefinition> columns)
        {
            var isPassDef = columns.FirstOrDefault(c => c.Name == "IsPass")
                ?? throw new ArgumentException("Missing required column definition: IsPass");

            int passCount = 0, failCount = 0;
            foreach (var record in reader.Data.Where(r => r.Values != null))
            {
                try
                {
                    if (record.Values.TryGetValue(isPassDef.Name, out var isPassObj))
                    {
                        var isPass = Convert.ToInt32(isPassObj);
                        if (isPass == 1) passCount++;
                        else if (isPass == 0) failCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Record conversion failed: {ex.Message}");
                }
            }

            var series = new List<ISeries>();
            if (passCount + failCount == 0) return series.ToArray();

            series.Add(new PieSeries<int>
            {
                Values = new[] { passCount },
                Name = "Pass",
                Fill = new SolidColorPaint(new SKColor(170, 210, 250)),
                Stroke = new SolidColorPaint(new SKColor(80, 140, 200), 2),
            });
            series.Add(new PieSeries<int>
            {
                Values = new[] { failCount },
                Name = "Fail",
                Fill = new SolidColorPaint(new SKColor(255, 150, 170).WithAlpha(180)),
                Stroke = new SolidColorPaint(new SKColor(220, 80, 100), 2),
            });
            return series.ToArray();
        }

        public static Axis[] BuildPieXAxes() => Array.Empty<Axis>();
        public static Axis[] BuildPieYAxes() => Array.Empty<Axis>();

        // ===== Helpers =====

        private static (List<ObservablePoint> Y2, List<ObservablePoint> Actual) ExtractXyPoints(
            IMTDataInterface<CsvDataRecord> reader, IReadOnlyList<ColumnDefinition> columns)
        {
            var xDef = columns.FirstOrDefault(c => c.Name == "Y1")
                ?? throw new ArgumentException("Missing required column: Y1");
            var y2Def = columns.FirstOrDefault(c => c.Name == "Y2")
                ?? throw new ArgumentException("Missing required column: Y2");
            var actualDef = columns.FirstOrDefault(c => c.Name == "Actual")
                ?? throw new ArgumentException("Missing required column: Actual");

            var y2 = new List<ObservablePoint>();
            var actual = new List<ObservablePoint>();
            foreach (var record in reader.Data.Where(r => r.Values != null))
            {
                try
                {
                    if (!record.Values.TryGetValue(xDef.Name, out var xObj)) continue;
                    var x = Convert.ToDouble(xObj);
                    if (record.Values.TryGetValue(y2Def.Name, out var y2Obj))
                        y2.Add(new ObservablePoint(x, Convert.ToDouble(y2Obj)));
                    if (record.Values.TryGetValue(actualDef.Name, out var actualObj))
                        actual.Add(new ObservablePoint(x, Convert.ToDouble(actualObj)));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Record conversion failed: {ex.Message}");
                }
            }
            return (y2, actual);
        }
    }
}
