using MetBench_BLL;
using System;
using System.Windows;

namespace MetBench_Client.Services.Plotting
{
    public class MTVisualizationEngine<TDataRecord, TDataReader>
        where TDataRecord : IDataRecord
        where TDataReader : IMTDataInterface<TDataRecord>
    {
        private readonly TDataReader _dataReader;
        private IPlotter<TDataRecord, TDataReader> _currentPlotter;
        public IPlotter<TDataRecord, TDataReader> GetPlotter() 
        {
            return _currentPlotter;
        }

        public MTVisualizationEngine(TDataReader dataReader, PlotType plotType = PlotType.Line)
        {
            _dataReader = dataReader;
            SetPlotterType(plotType);
        }

        public void SetPlotterType(PlotType plotType)
        {
            _currentPlotter = CreatePlotter(plotType);
            _currentPlotter.Initialize(_dataReader);
        }

        private IPlotter<TDataRecord, TDataReader> CreatePlotter(PlotType plotType)
        {
            return plotType switch
            {
                PlotType.Line => new LinePlotter<TDataRecord, TDataReader>(),
                PlotType.Scatter => new ScatterPlotter<TDataRecord, TDataReader>(),
                PlotType.Pie => new PiePlotter<TDataRecord, TDataReader>(),
                _ => throw new NotSupportedException($"Plotter for {plotType} is not supported")
            };
        }

        public void Visualize()
        {
            _currentPlotter.Plot();
        }

        public FrameworkElement GetPlotView() => _currentPlotter.GetView();
    }
}
