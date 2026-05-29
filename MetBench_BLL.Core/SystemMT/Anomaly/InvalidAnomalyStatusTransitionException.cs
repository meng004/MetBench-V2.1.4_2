using MetBench_Domain;

namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// 当请求一个不在状态机里的 <see cref="AnomalyStatus"/> 转移时抛出。
/// （CLAUDE.md §6 显式报错：非法转移是契约违反，不静默吞掉 / 返回 false。）
/// </summary>
public sealed class InvalidAnomalyStatusTransitionException : InvalidOperationException
{
    public AnomalyStatus From { get; }
    public AnomalyStatus To { get; }

    public InvalidAnomalyStatusTransitionException(AnomalyStatus from, AnomalyStatus to)
        : base(BuildMessage(from, to))
    {
        From = from;
        To = to;
    }

    private static string BuildMessage(AnomalyStatus from, AnomalyStatus to)
    {
        var allowed = from.AllowedNext();
        var allowedKebab = allowed.Count == 0
            ? "(none — terminal state)"
            : string.Join(", ", allowed.Select(s => s.ToKebab()));
        return $"Illegal anomaly status transition: {from.ToKebab()} → {to.ToKebab()}. " +
               $"Allowed from {from.ToKebab()}: {allowedKebab}.";
    }
}
