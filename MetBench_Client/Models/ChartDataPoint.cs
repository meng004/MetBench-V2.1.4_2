using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetBench_Client.Models
{
    /// <summary>
    /// 数据点模型，表示图表中的单个数据记录
    /// </summary>
    // 添加图表数据类（新增）
    public class ChartDataPoint
    {
        /// <summary>
        /// X轴值（测试用例序号）
        /// </summary>
        public double XValue { get; set; }

        /// <summary>
        /// 预期值
        /// </summary>
        public double ExpectedValue { get; set; }

        /// <summary>
        /// 实际值
        /// </summary>
        public double ActualValue { get; set; }

        /// <summary>
        /// 误差值
        /// </summary>
        public double Deviation { get; set; }
    }
}
