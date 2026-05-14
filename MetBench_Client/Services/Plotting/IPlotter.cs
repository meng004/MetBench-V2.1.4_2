using System.Collections.Generic;
using MetBench_BLL;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Windows;

namespace MetBench_Client.Services.Plotting
{
    public interface IPlotter<TDataRecord, TDataReader>
       where TDataRecord : IDataRecord
       where TDataReader : IMTDataInterface<TDataRecord>
    {
        /// <summary>
        /// 初始化绘图器
        /// </summary>
        void Initialize(TDataReader dataReader);

        /// <summary>
        /// 渲染数据
        /// </summary>
        void Plot();

        /// <summary>
        /// 获取可视化元素
        /// </summary>
        FrameworkElement GetView();

        /// <summary>
        /// 图表类型
        /// </summary>
        PlotType PlotType { get; }

        ISeries[] GetSeries();
        Axis[] GetXAxes();
        Axis[] GetYAxes();
    }
}
