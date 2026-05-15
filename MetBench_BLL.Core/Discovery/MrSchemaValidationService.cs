using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery;

/// <summary>
/// MetamorphicRelation 在写库前的"软外键"校验 —— 拒绝 MetaPatternCode 不在 MetaPatterns 表里的 MR。
/// </summary>
/// <remarks>
/// LiteDB 没有原生外键约束。本类作为 BLL 层守护，凡是经过 <see cref="ValidateAndAdd"/>
/// 的 MR 必然引用已存在的 MetaPattern。
///
/// 直接绕过本服务的 DAL <c>Add</c> 调用（如 migration 脚本）会跳过校验 ——
/// 那是设计意图（让 migration 能 bypass 用于初始化）；但日常代码路径都应过本服务。
///
/// 此服务也修了 cold-start finding C4 的复发隐患 ——
/// MetaPatternCode 拼错（如 "M_inv" 大小写）会被立刻拒绝。
/// </remarks>
public sealed class MrSchemaValidationService
{
    private readonly IMetaPatternRepository _metaPatternRepo;
    private readonly IMetamorphicRelationRepository _mrRepo;

    public MrSchemaValidationService(
        IMetaPatternRepository metaPatternRepo,
        IMetamorphicRelationRepository mrRepo)
    {
        _metaPatternRepo = metaPatternRepo;
        _mrRepo = mrRepo;
    }

    /// <summary>
    /// 校验 MR 的 MetaPatternCode 存在 + Status 不是 out-of-scope，然后 Add 入 MR 表。
    /// </summary>
    /// <exception cref="InvalidOperationException">MetaPatternCode 不存在 / out-of-scope / Code 重复。</exception>
    public int ValidateAndAdd(MetamorphicRelation mr)
    {
        if (string.IsNullOrEmpty(mr.MetaPatternCode))
            throw new InvalidOperationException(
                $"MR '{mr.Code}' has empty MetaPatternCode — must reference one of the active MetaPatterns.");

        var mp = _metaPatternRepo.Get(mr.MetaPatternCode);
        // LiteDB FindById 大小写不敏感，但 Code 应严格匹配（防 "M_INV" 漂移）。
        if (mp is null || !string.Equals(mp.Code, mr.MetaPatternCode, StringComparison.Ordinal))
        {
            var active = _metaPatternRepo.GetActive()
                .Select(p => p.Code)
                .OrderBy(c => c);
            throw new InvalidOperationException(
                $"MR '{mr.Code}' references unknown MetaPattern '{mr.MetaPatternCode}'. " +
                $"Active MetaPatterns: {string.Join(", ", active)}.");
        }

        if (mp.Status == "out-of-scope")
            throw new InvalidOperationException(
                $"MR '{mr.Code}' references out-of-scope MetaPattern '{mr.MetaPatternCode}' " +
                $"(reason: {mp.OutOfScopeReason}). Pick an active MetaPattern.");

        if (mp.Status == "deprecated")
            throw new InvalidOperationException(
                $"MR '{mr.Code}' references deprecated MetaPattern '{mr.MetaPatternCode}'. " +
                "Migrate to an active replacement before adding.");

        if (_mrRepo.IsDuplicate(mr, excludeSelf: false))
            throw new InvalidOperationException(
                $"MR Code '{mr.Code}' already exists — pick a unique code or use Modify.");

        _mrRepo.Add(mr);
        return mr.IdMR;
    }
}
