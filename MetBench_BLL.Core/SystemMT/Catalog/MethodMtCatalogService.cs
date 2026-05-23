using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// 方法级 MT 目录的 CRUD 服务 —— 与 <see cref="SystemMtCatalogService"/> 对称。
/// 强制 <c>Kind = "method-level"</c>；拒绝非空 <c>MetaPatternCode</c>（元模式仅用于系统级 MR）。
/// </summary>
public sealed class MethodMtCatalogService
{
    private const string MethodLevel = "method-level";

    private readonly IApplicationRepository _apps;
    private readonly IMetamorphicRelationRepository _mrs;
    private readonly IMRBindingRepository _bindings;
    private readonly IAuditLogRepository _audit;

    public MethodMtCatalogService(
        IApplicationRepository apps,
        IMetamorphicRelationRepository mrs,
        IMRBindingRepository bindings,
        IAuditLogRepository audit)
    {
        _apps = apps ?? throw new ArgumentNullException(nameof(apps));
        _mrs = mrs ?? throw new ArgumentNullException(nameof(mrs));
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    // ── MR CRUD ──────────────────────────────────────────────────────────────

    /// <summary>列出全部方法级 MR。</summary>
    public IReadOnlyList<MetamorphicRelation> ListMrs() =>
        _mrs.GetAll().Where(IsMethodLevel).ToList();

    /// <summary>按 Code 查找方法级 MR；不匹配方法级则返回 null。</summary>
    public MetamorphicRelation? FindMrByCode(string code) =>
        _mrs.GetAll().FirstOrDefault(m => IsMethodLevel(m) &&
            string.Equals(m.Code, code, StringComparison.Ordinal));

    /// <summary>按 IdMR 拿方法级 MR；非方法级返回 null。</summary>
    public MetamorphicRelation? GetMr(int mrId)
    {
        var mr = _mrs.GetAll().FirstOrDefault(m => m.IdMR == mrId);
        return (mr is not null && IsMethodLevel(mr)) ? mr : null;
    }

    /// <summary>
    /// 新建方法级 MR。
    /// - <c>Kind</c> 强制置为 <c>"method-level"</c>。
    /// - <c>MetaPatternCode</c> 非空时抛 <see cref="ArgumentException"/>（元模式仅系统级专用）。
    /// - <c>Code</c> 必填；同 Code 已存在抛 <see cref="InvalidOperationException"/>。
    /// </summary>
    public int CreateMr(MetamorphicRelation mr, string actor = "user")
    {
        ArgumentNullException.ThrowIfNull(mr);
        if (!string.IsNullOrWhiteSpace(mr.MetaPatternCode))
            throw new ArgumentException(
                "MetaPatternCode must be empty for method-level MR; " +
                "meta-patterns are reserved for system-level MR (mr-architecture §2).",
                nameof(mr));
        if (string.IsNullOrWhiteSpace(mr.Code))
            throw new ArgumentException("MR Code is required", nameof(mr));
        mr.Kind = MethodLevel;
        if (FindMrByCode(mr.Code) is not null)
            throw new InvalidOperationException(
                $"method-level MR with Code='{mr.Code}' already exists");
        if (mr.CreatedAt == default) mr.CreatedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(mr.CreatedBy)) mr.CreatedBy = actor;
        _mrs.Add(mr);
        Audit(actor, "mr.create", "MetamorphicRelation", mr.IdMR.ToString(), mr.Code);
        return mr.IdMR;
    }

    /// <summary>
    /// 更新方法级 MR。
    /// - 拒绝 Kind 非方法级或 MetaPatternCode 非空。
    /// - 撞 Code 检查排除自身。
    /// </summary>
    public bool UpdateMr(MetamorphicRelation mr, string actor = "user")
    {
        ArgumentNullException.ThrowIfNull(mr);
        if (!IsMethodLevel(mr))
            throw new ArgumentException("MR.Kind must be 'method-level'", nameof(mr));
        if (!string.IsNullOrWhiteSpace(mr.MetaPatternCode))
            throw new ArgumentException(
                "MetaPatternCode must be empty for method-level MR (mr-architecture §2).",
                nameof(mr));
        var existing = GetMr(mr.IdMR);
        if (existing is null)
            throw new InvalidOperationException(
                $"method-level MR id={mr.IdMR} not found");
        if (FindMrByCode(mr.Code) is { } collision
            && collision.IdMR != mr.IdMR)
        {
            throw new InvalidOperationException(
                $"another method-level MR with Code='{mr.Code}' already exists");
        }
        var ok = _mrs.Modify(mr);
        if (ok) Audit(actor, "mr.update", "MetamorphicRelation", mr.IdMR.ToString(), mr.Code);
        return ok;
    }

    /// <summary>
    /// 删除方法级 MR。
    /// </summary>
    public DeleteOutcome DeleteMr(int mrId, string actor = "user")
    {
        var mr = GetMr(mrId);
        if (mr is null) return DeleteOutcome.NotFound;
        var ok = _mrs.Remove(mr);
        if (ok) Audit(actor, "mr.delete", "MetamorphicRelation", mrId.ToString(), mr.Code);
        return ok ? DeleteOutcome.Deleted : DeleteOutcome.Blocked("repository refused delete");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static bool IsMethodLevel(MetamorphicRelation m) =>
        string.Equals(m.Kind, MethodLevel, StringComparison.Ordinal);

    private void Audit(string actor, string action, string entityType, string entityId, string detail)
    {
        _audit.Add(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = action,
            TargetEntityType = entityType,
            TargetEntityId = entityId,
            DetailsJson = detail,
        });
    }
}
