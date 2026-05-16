using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery;

/// <summary>
/// 跨求解器 (m_cmp) MR 配对 binding 的创建 + 一致性维护 (F16)。
/// </summary>
/// <remarks>
/// 闭环 cold-start C1 — m_cmp MR 同时绑两个 SUT 对比，旧 schema (MRId, ApplicationId)
/// 不能表达 "primary ↔ partner" 关系。新方案：
///
///   一份 m_cmp MR 产 2 条 binding：
///     binding A: { MRId, ApplicationId=openmoc, Role="primary", PartnerBindingId=B.Id }
///     binding B: { MRId, ApplicationId=openmc,  Role="partner", PartnerBindingId=A.Id }
///
/// 通过 <see cref="CreatePair"/> 创建保证两条 binding 互指 + 同 MRId；
/// 通过 <see cref="GetPartner"/> 反查对端。
///
/// 非 m_cmp MR 仍用 1:1 binding（Role 空 + PartnerBindingId null）。
/// </remarks>
public sealed class MRPairingService
{
    private readonly IMRBindingRepository _bindings;

    public MRPairingService(IMRBindingRepository bindings)
    {
        _bindings = bindings;
    }

    /// <summary>
    /// 为 m_cmp MR 创建配对 binding。返回 (primaryId, partnerId)。
    /// </summary>
    /// <exception cref="InvalidOperationException">primaryApp == partnerApp 等错误。</exception>
    public (int PrimaryId, int PartnerId) CreatePair(
        int mrId, int primaryApplicationId, int partnerApplicationId,
        string defaultSampleCasePath = "", string boundBy = "system")
    {
        if (primaryApplicationId == partnerApplicationId)
            throw new InvalidOperationException(
                $"m_cmp pair needs distinct SUTs; primary == partner = {primaryApplicationId}");

        var primary = new MRBinding
        {
            MRId = mrId,
            ApplicationId = primaryApplicationId,
            Role = "primary",
            DefaultSampleCasePath = defaultSampleCasePath,
            BoundBy = boundBy,
            Status = "active",
        };
        _bindings.Add(primary);

        var partner = new MRBinding
        {
            MRId = mrId,
            ApplicationId = partnerApplicationId,
            Role = "partner",
            DefaultSampleCasePath = defaultSampleCasePath,
            BoundBy = boundBy,
            Status = "active",
            PartnerBindingId = primary.IdMRBinding,
        };
        _bindings.Add(partner);

        // 回填 primary 指向 partner
        primary.PartnerBindingId = partner.IdMRBinding;
        _bindings.Modify(primary);

        return (primary.IdMRBinding, partner.IdMRBinding);
    }

    /// <summary>
    /// 查 binding 的对端。返回 null = 非配对 binding。
    /// </summary>
    public MRBinding? GetPartner(int bindingId)
    {
        var binding = _bindings.Get(bindingId);
        if (binding?.PartnerBindingId is not int pid) return null;
        return _bindings.Get(pid);
    }

    /// <summary>
    /// 校验配对一致性：每个 Role 非空 binding 都互指 + Role 互补 + MRId 相同。
    /// </summary>
    /// <returns>不一致的 binding ID 列表（空 = 全部 OK）。</returns>
    public IReadOnlyList<int> ValidatePairs()
    {
        var bad = new List<int>();
        var all = _bindings.GetAll();
        foreach (var b in all)
        {
            if (string.IsNullOrEmpty(b.Role)) continue;  // 普通 binding，跳过

            if (b.PartnerBindingId is null)
            {
                bad.Add(b.IdMRBinding);
                continue;
            }

            var partner = _bindings.Get(b.PartnerBindingId.Value);
            if (partner is null
                || partner.PartnerBindingId != b.IdMRBinding
                || partner.MRId != b.MRId
                || ExpectedComplementaryRole(b.Role) != partner.Role)
            {
                bad.Add(b.IdMRBinding);
            }
        }
        return bad;
    }

    /// <summary>列出指定 MR 的所有配对 binding（按 Role 排序：primary first）。</summary>
    public IReadOnlyList<MRBinding> GetPairsForMR(int mrId)
    {
        return _bindings.GetByMR(mrId)
            .Where(b => !string.IsNullOrEmpty(b.Role))
            .OrderBy(b => b.Role == "primary" ? 0 : 1)
            .ToList();
    }

    internal static string ExpectedComplementaryRole(string role) => role switch
    {
        "primary" => "partner",
        "partner" => "primary",
        _ => "",
    };
}
