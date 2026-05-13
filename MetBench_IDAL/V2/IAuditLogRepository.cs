using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>AuditLog 写入 + 查询接口（Guid PK）。</summary>
public interface IAuditLogRepository : IGuidRepository<AuditLog>
{
    /// <summary>按时间范围 + Action 类型列出（合规审计用）。</summary>
    ObservableCollection<AuditLog> GetByDateRange(DateTime from, DateTime to);

    /// <summary>按 Action 列出（如 'mr.promote'）。</summary>
    ObservableCollection<AuditLog> GetByAction(string action);

    /// <summary>按 TargetEntityId 列出（追溯一个实体的全部历史操作）。</summary>
    ObservableCollection<AuditLog> GetByTargetEntity(string entityType, string entityId);
}
