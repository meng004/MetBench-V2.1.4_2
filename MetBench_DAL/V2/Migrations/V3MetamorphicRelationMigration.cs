using MetBench_Domain;
using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using MetBench_IDAL;

namespace MetBench_DAL.V2.Migrations;

/// <summary>
/// V2 MR (<see cref="MetamorphicRelation"/>) → V3 MR (<see cref="MetamorphicRelationV3"/>)
/// 投影迁移。读 v2 repository 全表，按字符串字段映射到 V3 enum，按 MrCode upsert
/// 写入 V3 repository。
/// </summary>
/// <remarks>
/// 设计要点：
/// - **Idempotent**：按 MrCode 检查是否存在，存在则 Modify，否则 Add
/// - **Kind 过滤**：只迁移 <c>Kind=="system-level"</c> 的 v2 行（method-level 由 method
///   端独立维护，不进入 V3 索引视图）
/// - **不删 v2 数据**：V3 是只读索引视图，v2 仍是 source-of-truth
/// </remarks>
public static class V3MetamorphicRelationMigration
{
    public sealed record MigrationSummary(int Created, int Updated, int SkippedNonSystem);

    public static MigrationSummary MigrateAll(
        IMetamorphicRelationRepository v2Repo,
        IMetamorphicRelationV3Repository v3Repo)
    {
        ArgumentNullException.ThrowIfNull(v2Repo);
        ArgumentNullException.ThrowIfNull(v3Repo);

        int created = 0, updated = 0, skippedNonSystem = 0;
        foreach (var v2 in v2Repo.GetAll())
        {
            if (!string.Equals(v2.Kind, "system-level", StringComparison.Ordinal))
            {
                skippedNonSystem++;
                continue;
            }
            if (string.IsNullOrEmpty(v2.Code)) continue; // 无业务键的行跳过

            var existing = v3Repo.GetByCode(v2.Code);
            var v3 = existing ?? new MetamorphicRelationV3 { IdV3 = Guid.NewGuid() };
            Populate(v3, v2);
            if (existing is null)
            {
                v3Repo.Add(v3);
                created++;
            }
            else
            {
                v3Repo.Modify(v3);
                updated++;
            }
        }
        return new MigrationSummary(created, updated, skippedNonSystem);
    }

    private static void Populate(MetamorphicRelationV3 v3, MetamorphicRelation v2)
    {
        v3.MrCode = v2.Code;
        v3.Description = v2.Description ?? string.Empty;
        v3.Equation = MapEquation(v2.EquationKey);
        v3.ProgramType = MapProgram(v2.ApplicationName, v2.EquationKey);
        v3.MetaPattern = MapMetaPattern(v2.MetaPatternCode);
        v3.SourceLevel = MapSourceLevel(v2.DiscoveryMethod);
        v3.FailureCorrelation = FailureCorrelationKind.None;
        v3.RelationType = MapRelation(v2.AssertionTypeCode);
        v3.RigorClass = MapRigor(v3.MetaPattern, v3.ProgramType);
        v3.Tolerance = v2.ToleranceRel;
        v3.SyncedAt = DateTime.UtcNow;
    }

    private static EquationKind MapEquation(string equationKey) => equationKey switch
    {
        "bateman" => EquationKind.Bateman,
        "heat-equation-1d" => EquationKind.Fourier,
        "neutron-transport" => EquationKind.Boltzmann,
        "diffusion" => EquationKind.Diffusion,
        "navier-stokes" => EquationKind.NavierStokes,
        "" => EquationKind.Other,
        _ => EquationKind.Other,
    };

    private static ProgramKind MapProgram(string appName, string equationKey)
    {
        // 启发式：openmc 是 Monte Carlo，其他 SUT 都是数值/解析
        if (appName.Contains("openmc", StringComparison.OrdinalIgnoreCase))
            return ProgramKind.MC;
        // bateman 通过 decay_chain RK4 是数值；projectile/subchannel 闭式
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
