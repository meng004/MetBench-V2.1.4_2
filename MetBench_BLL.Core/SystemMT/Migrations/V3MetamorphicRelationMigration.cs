using MetBench_Domain;
using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using MetBench_IDAL;

namespace MetBench_BLL.SystemMT.Migrations;

/// <summary>
/// V2 MR (<see cref="MetamorphicRelation"/>) → V3 MR (<see cref="MetamorphicRelationV3"/>)
/// 投影迁移。读 v2 repository 全表，按字段映射到 V3 enum，按 MrCode upsert 写入 V3。
/// </summary>
/// <remarks>
/// 设计要点：
/// - **Idempotent**：按 MrCode 检查是否存在，存在则 Modify，否则 Add
/// - **Kind 过滤**：只迁移 <c>Kind=="system-level"</c> 的 v2 行（method-level 由 method
///   端独立维护，不进入 V3 索引视图）
/// - **不删 v2 数据**：V3 是只读索引视图，v2 仍是 source-of-truth
/// - **ProgramKind 推断**：通过 MR.IdMR → MRBinding → Application.Name 查 SutName
///   （而非依赖已 [Obsolete] 的 MR.ApplicationName 字段）。若 bindingRepo 或 appRepo 为
///   null，则退化为 ProgramKind.Unspecified。
/// - **重复 Code**：按 v2 表内同 Code 行后到者覆盖（last-wins，并增 Conflicts 计数）
/// </remarks>
public static class V3MetamorphicRelationMigration
{
    public sealed record MigrationSummary(int Created, int Updated, int SkippedNonSystem,
        int Conflicts, int Failed);

    public static MigrationSummary MigrateAll(
        IMetamorphicRelationRepository v2Repo,
        IMetamorphicRelationV3Repository v3Repo,
        IMRBindingRepository? bindingRepo = null,
        IApplicationRepository? appRepo = null)
    {
        ArgumentNullException.ThrowIfNull(v2Repo);
        ArgumentNullException.ThrowIfNull(v3Repo);

        // 预建 (MR.IdMR → SutName) 查找表，避免 N×M scan
        var sutNameByMrId = BuildSutNameLookup(bindingRepo, appRepo);

        int created = 0, updated = 0, skippedNonSystem = 0, conflicts = 0, failed = 0;
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var v2 in v2Repo.GetAll())
        {
            if (!string.Equals(v2.Kind, "system-level", StringComparison.Ordinal))
            {
                skippedNonSystem++;
                continue;
            }
            if (string.IsNullOrEmpty(v2.Code)) continue; // 无业务键的行跳过
            if (!seenCodes.Add(v2.Code)) conflicts++;

            var sutName = sutNameByMrId.TryGetValue(v2.IdMR, out var sn) ? sn : string.Empty;

            var existing = v3Repo.GetByCode(v2.Code);
            var v3 = existing ?? new MetamorphicRelationV3 { IdV3 = Guid.NewGuid() };
            Populate(v3, v2, sutName);
            bool ok;
            if (existing is null)
            {
                ok = v3Repo.Add(v3);
                if (ok) created++; else failed++;
            }
            else
            {
                ok = v3Repo.Modify(v3);
                if (ok) updated++; else failed++;
            }
        }
        return new MigrationSummary(created, updated, skippedNonSystem, conflicts, failed);
    }

    private static IReadOnlyDictionary<int, string> BuildSutNameLookup(
        IMRBindingRepository? bindingRepo, IApplicationRepository? appRepo)
    {
        if (bindingRepo is null || appRepo is null)
            return new Dictionary<int, string>();
        var appNameById = appRepo.GetAll().ToDictionary(a => a.IdApplication, a => a.Name ?? string.Empty);
        var result = new Dictionary<int, string>();
        foreach (var b in bindingRepo.GetAll())
        {
            if (appNameById.TryGetValue(b.ApplicationId, out var name))
                result[b.MRId] = name; // 一个 MR 多 SUT 时取任意（实际 launcher 是 1:1）
        }
        return result;
    }

    private static void Populate(MetamorphicRelationV3 v3, MetamorphicRelation v2, string sutName)
    {
        v3.MrCode = v2.Code;
        v3.Description = v2.Description ?? string.Empty;
        v3.Equation = MapEquation(v2.EquationKey);
        v3.ProgramType = MapProgram(sutName);
        v3.MetaPattern = MapMetaPattern(v2.MetaPatternCode);
        v3.SourceLevel = MapSourceLevel(v2.DiscoveryMethod);
        v3.FailureCorrelation = FailureCorrelationKind.None;
        v3.RelationType = MapRelation(v2.AssertionTypeCode);
        v3.RigorClass = MapRigor(v3.MetaPattern, v3.ProgramType);
        v3.Tolerance = v2.ToleranceRel;
        v3.SyncedAt = DateTime.UtcNow;
    }

    private static EquationKind MapEquation(string? equationKey) => equationKey switch
    {
        "bateman" => EquationKind.Bateman,
        "heat-equation-1d" => EquationKind.Fourier,
        "neutron-transport" => EquationKind.Boltzmann,
        "diffusion" => EquationKind.Diffusion,
        "navier-stokes" => EquationKind.NavierStokes,
        null or "" => EquationKind.Unspecified,  // 语义修正：未指定 ≠ 非反应堆 (Other)
        _ => EquationKind.Other,
    };

    /// <summary>
    /// 按 SUT 名推断 ProgramKind。空 sutName（缺 binding/app repos 或未注册）→ Unspecified。
    /// </summary>
    private static ProgramKind MapProgram(string? sutName)
    {
        if (string.IsNullOrEmpty(sutName)) return ProgramKind.Unspecified;
        if (sutName.Contains("openmc", StringComparison.OrdinalIgnoreCase))
            return ProgramKind.MC;
        // projectile / subchannel-1d 是闭式代数（Analytic）；其余 ODE/FD/RK4 是 Num
        if (sutName.Contains("projectile", StringComparison.OrdinalIgnoreCase)
            || sutName.Contains("subchannel", StringComparison.OrdinalIgnoreCase))
            return ProgramKind.Analytic;
        return ProgramKind.Num;
    }

    private static MetaPatternKind MapMetaPattern(string code) => code switch
    {
        "m_mono" => MetaPatternKind.Mono,
        "m_inv" => MetaPatternKind.Inv,
        "m_conv" => MetaPatternKind.Conv,
        "m_part" => MetaPatternKind.Part,
        "m_traj" => MetaPatternKind.Traj,
        "m_cmp" => MetaPatternKind.Cmp,
        "m_rev" => MetaPatternKind.Rev,
        "m_dyn" => MetaPatternKind.Dyn,
        "m_adj" => MetaPatternKind.Adj,
        "m_rel" => MetaPatternKind.Rel,
        _ => MetaPatternKind.Unspecified,
    };

    private static SourceLevelKind MapSourceLevel(string discoveryMethod) => discoveryMethod switch
    {
        "manual" => SourceLevelKind.Manual,
        "literature" => SourceLevelKind.Literature,
        "meta-prompt" => SourceLevelKind.MetaPrompt,
        "multi-llm-consensus" => SourceLevelKind.MultiLlmConsensus,
        "scg-heuristic" => SourceLevelKind.ScgHeuristic,
        _ => SourceLevelKind.Unspecified,
    };

    private static RelationKind MapRelation(string assertionTypeCode) => assertionTypeCode switch
    {
        "greater" or "less" => RelationKind.Ordinal,
        "approx" => RelationKind.Equality,
        "cross-program-agree" => RelationKind.CrossProgramAgreement,
        _ => RelationKind.Unspecified,
    };

    private static RigorClassKind MapRigor(MetaPatternKind mp, ProgramKind pk)
    {
        // 启发式：解析解或不变量 → A；数值收敛 → B；其他 → C
        if (pk == ProgramKind.Analytic || mp == MetaPatternKind.Inv) return RigorClassKind.A;
        if (mp == MetaPatternKind.Conv) return RigorClassKind.B;
        return RigorClassKind.C;
    }
}
