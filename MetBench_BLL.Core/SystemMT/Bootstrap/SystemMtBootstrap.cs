using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Metadata;

namespace MetBench_BLL.SystemMT.Bootstrap;

/// <summary>
/// System-MT 启动期 catalog 同步 helper —— 把"手动跑 importer"路径封装为
/// 启动时一次性自动 bootstrap。
/// </summary>
/// <remarks>
/// 设计目的（关联缺口 G-08）：当前 system MT 有三处 MR 数据：
/// <list type="number">
///   <item><see cref="SystemMtLauncher"/> 内部硬编码 <c>MrBlueprint</c></item>
///   <item><see cref="SystemMtMetadataCatalog"/> 静态 seed（含 EquationMetadata + MrMetadata）</item>
///   <item>LiteDB 实体表（Application / MetamorphicRelation / MRBinding）</item>
/// </list>
/// (1) 与 (2) 由 drift-guard 测试强制对齐；(3) 需要 <see cref="LauncherCatalogV2Importer"/>
/// 投影。本类把 (2)→metadata DB 与 (1)→entity 表两条路径串成一个 idempotent 启动调用。
///
/// 完整 source-of-truth 收口（把 launcher 改为读 catalog 而非硬编码）属 Stage 9+ 重构，
/// 不在本类范围。本类只**简化生产路径接入**，不改变现有 seed 数据。
/// </remarks>
public sealed class SystemMtBootstrap
{
    /// <summary>
    /// 串行执行 catalog seed → entity 投影。两步均 idempotent，可在每次启动跑。
    /// </summary>
    /// <param name="metadataRepository">元信息仓库；为 null 时跳过 metadata seed。</param>
    /// <param name="launcherImporter">launcher → entity 投影器；为 null 时跳过 entity import。</param>
    /// <param name="actor">审计日志 actor 标识。</param>
    /// <param name="cancellationToken">取消令牌（metadata seed 阶段生效）。</param>
    /// <returns>本次 bootstrap 的摘要。</returns>
    public static async Task<BootstrapResult> SeedCatalogsAsync(
        ISystemMtMetadataRepository? metadataRepository,
        LauncherCatalogV2Importer? launcherImporter,
        string actor = "bootstrap",
        CancellationToken cancellationToken = default)
    {
        int equationsSeeded = 0;
        int mrsSeeded = 0;
        CatalogImportSummary? importSummary = null;

        // Phase 1: metadata DB seed（idempotent upsert）
        if (metadataRepository is not null)
        {
            await SystemMtMetadataCatalog.SeedAsync(metadataRepository, cancellationToken);
            equationsSeeded = SystemMtMetadataCatalog.Equations.Count;
            mrsSeeded = SystemMtMetadataCatalog.MetamorphicRelations.Count;
        }

        // Phase 2: launcher → entity 表投影（idempotent："created vs existing"）
        if (launcherImporter is not null)
        {
            importSummary = launcherImporter.Import(actor);
        }

        return new BootstrapResult(
            EquationsSeeded: equationsSeeded,
            MrsSeeded: mrsSeeded,
            ImportSummary: importSummary);
    }
}

/// <summary>
/// <see cref="SystemMtBootstrap.SeedCatalogsAsync"/> 的执行摘要。
/// </summary>
public sealed record BootstrapResult(
    int EquationsSeeded,
    int MrsSeeded,
    CatalogImportSummary? ImportSummary);
