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

    /// <summary>
    /// 新建方法级 MR。
    /// - <c>Kind</c> 强制置为 <c>"method-level"</c>。
    /// - <c>MetaPatternCode</c> 非空时抛 <see cref="ArgumentException"/>（元模式仅系统级专用）。
    /// </summary>
    public int CreateMr(MetamorphicRelation mr, string actor = "user")
    {
        ArgumentNullException.ThrowIfNull(mr);
        if (!string.IsNullOrWhiteSpace(mr.MetaPatternCode))
            throw new ArgumentException(
                "MetaPatternCode must be empty for method-level MR; " +
                "meta-patterns are reserved for system-level MR (mr-architecture §2).",
                nameof(mr));
        mr.Kind = MethodLevel;
        _mrs.Add(mr);
        Audit(actor, "mr.create", "MetamorphicRelation", mr.IdMR.ToString(), mr.Code);
        return mr.IdMR;
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
