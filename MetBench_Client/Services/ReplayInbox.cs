using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;

namespace MetBench_Client.Services
{
    /// <summary>
    /// 跨页面传递 Replay 上下文的轻量盒子（singleton）。
    /// AnomalyListPage 在 navigate 到 ReplayResultPage 前 Set，
    /// ReplayResultPage 在 OnNavigatedTo 时 Read。
    /// </summary>
    public sealed class ReplayInbox
    {
        public Anomaly? PendingAnomaly { get; set; }

        /// <summary>已有的 ReplayResult（实际 Pipeline 已跑完）。VM 上读到则直接渲染。</summary>
        public ReplayResult? LastResult { get; set; }
    }
}
