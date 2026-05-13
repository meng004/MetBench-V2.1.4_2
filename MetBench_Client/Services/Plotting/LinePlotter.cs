using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using System.Diagnostics;

namespace MetBench_Client.Services.Plotting
{
    public class LinePlotter<TDataRecord, TDataReader> : BasePlotter<TDataRecord, TDataReader>
       where TDataRecord : IDataRecord
       where TDataReader : IMTDataInterface<TDataRecord>
    {
       
        public override PlotType PlotType => PlotType.Line;

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
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
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

            if (y2Points.Count > 0)
            {
                var fill = new SolidColorPaint(SKColors.Blue.WithAlpha(90));
                series.Add(CreateLineSeries(
                    y2Points,
                    "(Y1,Y2)",
                    SKColors.Blue,
                    3,
                    null,
                    null));

            }

            if (actualPoints.Count > 0)
            {
                var fill = new SolidColorPaint(SKColors.Red.WithAlpha(90));
                series.Add(CreateLineSeries(
                    actualPoints,
                    "(Y1,Actual)",
                    SKColors.Red,
                    2,
                    null,
                    new DashEffect(new float[] { 6, 6 }, 0)));
            }

            return series;
        }

        private LineSeries<ObservablePoint> CreateLineSeries(
            IEnumerable<ObservablePoint> points,
            string name,
            SKColor color,
            float strokeWidth,
            SolidColorPaint solidColorPaint,
            DashEffect dashEffect)
        {
            return new LineSeries<ObservablePoint>
            {
                Values = points.ToList(),
                Name = name,
                Stroke = new SolidColorPaint(color, strokeWidth) { PathEffect = dashEffect },
                LineSmoothness = 10,
                Fill = solidColorPaint,
                GeometrySize = 10,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
                
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