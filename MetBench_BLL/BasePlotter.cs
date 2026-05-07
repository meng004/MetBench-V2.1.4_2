using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System.Windows;

namespace MetBench_BLL
{
    public abstract class BasePlotter<TDataRecord, TDataReader> : IPlotter<TDataRecord, TDataReader>
       where TDataRecord : IDataRecord
       where TDataReader : IMTDataInterface<TDataRecord>
    {
        protected readonly CartesianChart _chart = new CartesianChart();
        protected TDataReader _dataReader;
        protected IEnumerable<ColumnDefinition> _columnDefinitions;

        public abstract PlotType PlotType { get; }

        public virtual void Initialize(TDataReader dataReader)
        {
            _dataReader = dataReader;
            _columnDefinitions = dataReader.ColumnDefinitions;
            InitializeChart();
        }
        public abstract ISeries[] GetSeries();
        public abstract Axis[] GetXAxes();
        public abstract Axis[] GetYAxes();

        protected virtual void InitializeChart()
        {
            _chart.XAxes = new[] { new Axis { Name = "X Axis" } };
            _chart.YAxes = new[] { new Axis { Name = "Y Axis" } };
            _chart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Top;
        }

        public abstract void Plot();

        public FrameworkElement GetView() => _chart;

        protected T ConvertValue<T>(object value)
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
    }
}
