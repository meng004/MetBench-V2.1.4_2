using MetBench_DAL.V2;
using MetBench_IDAL;
using Microsoft.Extensions.DependencyInjection;

namespace MetBench_DAL;

/// <summary>
/// DI 注册扩展：把 v2 system-MT 的 20 个 Repository 一次性注册进 IServiceCollection。
/// </summary>
/// <remarks>
/// 在 WPF App.xaml.cs ConfigureServices 中调用：
/// <code>
/// services.AddSystemMtRepositories();
/// </code>
/// 或在 BLL.Core 测试 / CLI tool 中等同调用。
///
/// 使用 Scoped 生命周期，与 v1 Repository 的 DI 风格一致；底层 DbConfig 是
/// 单例（线程安全），所以 Scoped 不会重复初始化数据库。
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>注册 20 个 v2 system-MT Repository（int PK 9 个 + Guid PK 11 个）。</summary>
    public static IServiceCollection AddSystemMtRepositories(this IServiceCollection services)
    {
        // ===== int PK Repository =====
        services.AddScoped<IRuntimeRepository, LiteDbRuntimeRepository>();
        services.AddScoped<IMRBindingRepository, LiteDbMRBindingRepository>();
        services.AddScoped<IApplicationDomainRepository, LiteDbApplicationDomainRepository>();
        services.AddScoped<IMRInstanceRepository, LiteDbMRInstanceRepository>();
        services.AddScoped<IDiscoveryMethodRepository, LiteDbDiscoveryMethodRepository>();
        services.AddScoped<IMutationOperatorRepository, LiteDbMutationOperatorRepository>();
        services.AddScoped<IMutantRepository, LiteDbMutantRepository>();
        services.AddScoped<IKnownBugRepository, LiteDbKnownBugRepository>();
        services.AddScoped<IBatchPlanRepository, LiteDbBatchPlanRepository>();
        services.AddScoped<IMetaPatternRepository, LiteDbMetaPatternRepository>();

        // ===== Guid PK Repository =====
        services.AddScoped<IExecutionRepository, LiteDbExecutionRepository>();
        services.AddScoped<IResultRepository, LiteDbResultRepository>();
        services.AddScoped<IAnomalyRepository, LiteDbAnomalyRepository>();
        services.AddScoped<IDiscoveryRunRepository, LiteDbDiscoveryRunRepository>();
        services.AddScoped<ICandidateMRRepository, LiteDbCandidateMRRepository>();
        services.AddScoped<IValidationRunRepository, LiteDbValidationRunRepository>();
        services.AddScoped<IMutationCampaignRepository, LiteDbMutationCampaignRepository>();
        services.AddScoped<IMutationResultRepository, LiteDbMutationResultRepository>();
        services.AddScoped<IAuditLogRepository, LiteDbAuditLogRepository>();
        services.AddScoped<IBatchRepository, LiteDbBatchRepository>();
        services.AddScoped<IReportRepository, LiteDbReportRepository>();

        return services;
    }
}
