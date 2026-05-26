using System.Text.Json;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// 把 runnable System-MT catalog 的只读视图
/// 投影到 v2 实体表:每个 SUT → <see cref="Application"/>,每个 MR →
/// <see cref="MetamorphicRelation"/>,绑定 → <see cref="MRBinding"/>。
/// </summary>
/// <remarks>
/// 目的:让 Coverage / Reporting / Discovery 等 v2 子系统能用 IDAL
/// 接口枚举当前 runnable catalog,而不依赖 <see cref="SystemMtLauncher"/> 具体类。
/// 本类**不**修改 launcher 的 RunAsync 行为;它只是把同样的数据**额外**写一份到
/// v2 表里。
///
/// 幂等:重复导入不创建重复行,通过 (Name + Kind="system-level") /
/// (Code + Kind="system-level") / (MRId + ApplicationId + DefaultSampleCasePath)
/// 做查重,与 LiteDB 的 MRBinding_Composite 唯一索引保持一致。
///
/// 这是 migration-plan P5「launcher 转数据驱动」的入门半步 —— 把数据先搬过来,
/// 等 v2 entity-driven launcher 真正出现时再让 RunAsync 改读 v2 表。
/// </remarks>
public sealed class LauncherCatalogV2Importer
{
    private readonly ISystemMtCatalogReader _catalogReader;
    private readonly IApplicationRepository _apps;
    private readonly IMetamorphicRelationRepository _mrs;
    private readonly IMRBindingRepository _bindings;
    private readonly IAuditLogRepository _audit;

    public LauncherCatalogV2Importer(
        ISystemMtCatalogReader catalogReader,
        IApplicationRepository apps,
        IMetamorphicRelationRepository mrs,
        IMRBindingRepository bindings,
        IAuditLogRepository audit)
    {
        _catalogReader = catalogReader ?? throw new ArgumentNullException(nameof(catalogReader));
        _apps = apps ?? throw new ArgumentNullException(nameof(apps));
        _mrs = mrs ?? throw new ArgumentNullException(nameof(mrs));
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    /// <summary>
    /// 把 launcher 当前目录全部投影到 v2 实体表。返回各类计数。
    /// </summary>
    /// <param name="actor">审计 actor 标识(默认 "launcher-import")。</param>
    public CatalogImportSummary Import(string actor = "launcher-import")
    {
        var entries = _catalogReader.GetCatalogEntries();
        var summary = new CatalogImportSummary();

        var existingApps = _apps.GetAll().ToList();
        var existingMrs = _mrs.GetAll().ToList();
        var existingBindings = _bindings.GetAll().ToList();

        // SUT 名 → Application id(本次导入后)
        var sutToAppId = new Dictionary<string, int>(StringComparer.Ordinal);
        // MR id → MR int id(本次导入后)
        var mrIdToMrInt = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            // 1) Application
            var sutName = entry.Mr.SutName;
            if (!sutToAppId.ContainsKey(sutName))
            {
                var existing = existingApps.FirstOrDefault(a =>
                    string.Equals(a.Name, sutName, StringComparison.Ordinal)
                    && string.Equals(a.Kind, "system-level", StringComparison.Ordinal));
                if (existing is null)
                {
                    var app = new Application
                    {
                        Name = sutName,
                        Description = $"System-MT SUT (imported from launcher catalog): {sutName}",
                        ProgrammingLanguage = "Python",
                        Kind = "system-level",
                        RunnerEntryPath = entry.RunnerScriptPath,
                        InputParserPath = entry.InputParserScriptPath,
                        OutputParserPath = entry.OutputParserScriptPath,
                        DefaultTimeoutSeconds = 60,
                        MaxConcurrentRuns = 1,
                    };
                    _apps.Add(app);
                    sutToAppId[sutName] = app.IdApplication;
                    summary.ApplicationsCreated++;
                }
                else
                {
                    sutToAppId[sutName] = existing.IdApplication;
                    summary.ApplicationsExisting++;
                }
            }

            // 2) MetamorphicRelation
            var mrCode = entry.Mr.Id;
            if (!mrIdToMrInt.ContainsKey(mrCode))
            {
                var existing = existingMrs.FirstOrDefault(m =>
                    string.Equals(m.Code, mrCode, StringComparison.Ordinal)
                    && string.Equals(m.Kind, "system-level", StringComparison.Ordinal));
                if (existing is null)
                {
                    var mr = new MetamorphicRelation
                    {
                        Code = mrCode,
                        Description = entry.Mr.Description,
                        Kind = "system-level",
                        TransformationName = entry.PrimaryTransformationName,
                        AssertionTypeCode = entry.AssertionTypeCode,
                        ValueName = entry.Mr.ValueName,
                        MetaPatternCode = DeriveMetaPatternCode(entry.Mr.MrFamily),
                        DiscoveryMethod = "manual",
                        // v2 System MT 的真实绑定在 MRBinding 表里。这里仅填充旧
                        // MR_Idx 需要的稳定占位 pattern,并保持 obsolete
                        // ApplicationName 为空,防止 Method MT legacy 查询入口把
                        // System MT 行当作方法级 MR 暴露出去。
                        InputPattern = $"systemmt:{mrCode}:source",
                        OutputPattern = $"systemmt:{mrCode}:followup",
#pragma warning disable CS0618
                        ApplicationName = string.Empty,
#pragma warning restore CS0618
                        // S8-P5 review fix: 把 launcher blueprint 的 EquationKey 显式投影到 MR row
                        // （之前漏写，导致 V3 migration 全部 collapse 到 EquationKind.Other）
                        EquationKey = entry.EquationKey,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = actor,
                    };
                    if (!_mrs.Add(mr))
                    {
                        throw new InvalidOperationException(
                            $"Failed to import System MT MR '{mrCode}' for SUT '{sutName}'.");
                    }

                    var imported = ResolveImportedMr(mr, mrCode);
                    mrIdToMrInt[mrCode] = imported.IdMR;
                    existingMrs.Add(imported);
                    summary.MrsCreated++;
                }
                else
                {
                    mrIdToMrInt[mrCode] = existing.IdMR;
                    summary.MrsExisting++;
                }
            }

            // 3) MRBinding (mr × sut 唯一)
            var mrIntId = mrIdToMrInt[mrCode];
            var appId = sutToAppId[sutName];
            var existingBinding = existingBindings.FirstOrDefault(b =>
                b.MRId == mrIntId
                && b.ApplicationId == appId
                && string.Equals(
                    b.DefaultSampleCasePath,
                    entry.SampleCaseRelativePath,
                    StringComparison.Ordinal));
            if (existingBinding is null)
            {
                var binding = new MRBinding
                {
                    MRId = mrIntId,
                    ApplicationId = appId,
                    DefaultSampleCasePath = entry.SampleCaseRelativePath,
                    Status = "active",
                    BoundAt = DateTime.UtcNow,
                    BoundBy = actor,
                };
                if (!_bindings.Add(binding))
                {
                    throw new InvalidOperationException(
                        $"Failed to import System MT MR binding '{mrCode}' for SUT '{sutName}'.");
                }

                existingBindings.Add(binding);
                summary.BindingsCreated++;
            }
            else
            {
                summary.BindingsExisting++;
            }
        }

        _audit.Add(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = "launcher.catalog.import",
            TargetEntityType = "LauncherCatalog",
            TargetEntityId = string.Empty,
            DetailsJson = JsonSerializer.Serialize(new
            {
                applicationsCreated = summary.ApplicationsCreated,
                applicationsExisting = summary.ApplicationsExisting,
                mrsCreated = summary.MrsCreated,
                mrsExisting = summary.MrsExisting,
                bindingsCreated = summary.BindingsCreated,
                bindingsExisting = summary.BindingsExisting,
            }),
        });

        return summary;
    }

    /// <summary>
    /// 从 launcher 的 MrFamily 字符串(如 "NeutronTransport.Scaling.NuSigmaF")反推
    /// NOETHER 元模式代码。识别 Scaling → m_mono、Invariance → m_inv、
    /// Convergence → m_conv。后续 MR 库扩展可再增 m_cmp 等。
    /// </summary>
    private static string DeriveMetaPatternCode(string mrFamily)
    {
        if (string.IsNullOrEmpty(mrFamily)) return string.Empty;
        if (mrFamily.Contains("Scaling", StringComparison.Ordinal)) return "m_mono";
        if (mrFamily.Contains("Invariance", StringComparison.Ordinal)) return "m_inv";
        if (mrFamily.Contains("Convergence", StringComparison.Ordinal)) return "m_conv";
        return string.Empty;
    }

    private MetamorphicRelation ResolveImportedMr(MetamorphicRelation inserted, string mrCode)
    {
        if (inserted.IdMR > 0)
        {
            return inserted;
        }

        var persisted = _mrs.GetAll().FirstOrDefault(m =>
            string.Equals(m.Code, mrCode, StringComparison.Ordinal)
            && string.Equals(m.Kind, "system-level", StringComparison.Ordinal));

        if (persisted is null || persisted.IdMR <= 0)
        {
            throw new InvalidOperationException(
                $"Imported System MT MR '{mrCode}' did not receive a persistent IdMR.");
        }

        return persisted;
    }
}

public sealed record CatalogImportSummary
{
    public int ApplicationsCreated { get; set; }
    public int ApplicationsExisting { get; set; }
    public int MrsCreated { get; set; }
    public int MrsExisting { get; set; }
    public int BindingsCreated { get; set; }
    public int BindingsExisting { get; set; }
}
