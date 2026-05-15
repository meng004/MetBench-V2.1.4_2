using MetBench_DAL;
using MetBench_DAL.V2;
using MetBench_IDAL;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// TDD 验证 P2.3：AddSystemMtRepositories() 扩展方法注册了 21 个 v2 Repository
/// (20 原始 + 1 新增 IMetaPatternRepository @ 2026-05-15)。
/// 由于 v1 DbConfig 单例依赖 ConfigurationManager 在 WPF 上下文里，本测试
/// 仅校验 ServiceCollection 描述符（描述 service-type → implementation-type 映射），
/// 不实际构造 service provider（避免触发数据库连接初始化）。
/// </summary>
public sealed class V2RepositoryDIBindingTests
{
    [Fact]
    public void P2_3_AddSystemMtRepositories_registers_21_v2_repositories()
    {
        var services = new ServiceCollection();
        services.AddSystemMtRepositories();

        Assert.Equal(21, services.Count);
    }

    [Fact]
    public void P2_3_All_int_pk_interfaces_bind_to_litedb_impl()
    {
        var services = new ServiceCollection();
        services.AddSystemMtRepositories();

        var expectedBindings = new (Type ServiceType, Type ImplType)[]
        {
            (typeof(IRuntimeRepository),            typeof(LiteDbRuntimeRepository)),
            (typeof(IMRBindingRepository),          typeof(LiteDbMRBindingRepository)),
            (typeof(IApplicationDomainRepository),  typeof(LiteDbApplicationDomainRepository)),
            (typeof(IMRInstanceRepository),         typeof(LiteDbMRInstanceRepository)),
            (typeof(IDiscoveryMethodRepository),    typeof(LiteDbDiscoveryMethodRepository)),
            (typeof(IMutationOperatorRepository),   typeof(LiteDbMutationOperatorRepository)),
            (typeof(IMutantRepository),             typeof(LiteDbMutantRepository)),
            (typeof(IKnownBugRepository),           typeof(LiteDbKnownBugRepository)),
            (typeof(IBatchPlanRepository),          typeof(LiteDbBatchPlanRepository)),
        };

        foreach (var (svc, impl) in expectedBindings)
        {
            var descriptor = services.FirstOrDefault(s => s.ServiceType == svc);
            Assert.NotNull(descriptor);
            Assert.Equal(impl, descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }
    }

    [Fact]
    public void P2_3_All_guid_pk_interfaces_bind_to_litedb_impl()
    {
        var services = new ServiceCollection();
        services.AddSystemMtRepositories();

        var expectedBindings = new (Type ServiceType, Type ImplType)[]
        {
            (typeof(IExecutionRepository),          typeof(LiteDbExecutionRepository)),
            (typeof(IResultRepository),             typeof(LiteDbResultRepository)),
            (typeof(IAnomalyRepository),            typeof(LiteDbAnomalyRepository)),
            (typeof(IDiscoveryRunRepository),       typeof(LiteDbDiscoveryRunRepository)),
            (typeof(ICandidateMRRepository),        typeof(LiteDbCandidateMRRepository)),
            (typeof(IValidationRunRepository),      typeof(LiteDbValidationRunRepository)),
            (typeof(IMutationCampaignRepository),   typeof(LiteDbMutationCampaignRepository)),
            (typeof(IMutationResultRepository),     typeof(LiteDbMutationResultRepository)),
            (typeof(IAuditLogRepository),           typeof(LiteDbAuditLogRepository)),
            (typeof(IBatchRepository),              typeof(LiteDbBatchRepository)),
            (typeof(IReportRepository),             typeof(LiteDbReportRepository)),
        };

        foreach (var (svc, impl) in expectedBindings)
        {
            var descriptor = services.FirstOrDefault(s => s.ServiceType == svc);
            Assert.NotNull(descriptor);
            Assert.Equal(impl, descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }
    }

    [Fact]
    public void P2_3_AddSystemMtRepositories_returns_same_collection_instance()
    {
        var services = new ServiceCollection();
        var result = services.AddSystemMtRepositories();
        Assert.Same(services, result);  // fluent API contract
    }

    [Fact]
    public void P2_3_All_v2_repo_impls_inherit_correct_base_class()
    {
        // int PK impl 必须继承 LiteDbIntPkRepositoryBase<T>
        var intPkImpls = new[]
        {
            typeof(LiteDbRuntimeRepository),
            typeof(LiteDbMRBindingRepository),
            typeof(LiteDbApplicationDomainRepository),
            typeof(LiteDbMRInstanceRepository),
            typeof(LiteDbDiscoveryMethodRepository),
            typeof(LiteDbMutationOperatorRepository),
            typeof(LiteDbMutantRepository),
            typeof(LiteDbKnownBugRepository),
            typeof(LiteDbBatchPlanRepository),
        };
        foreach (var t in intPkImpls)
        {
            var baseType = t.BaseType;
            Assert.NotNull(baseType);
            Assert.True(baseType.IsGenericType);
            Assert.Equal(typeof(LiteDbIntPkRepositoryBase<>),
                baseType.GetGenericTypeDefinition());
        }

        // Guid PK impl 必须继承 LiteDbGuidPkRepositoryBase<T>
        var guidPkImpls = new[]
        {
            typeof(LiteDbExecutionRepository),
            typeof(LiteDbResultRepository),
            typeof(LiteDbAnomalyRepository),
            typeof(LiteDbDiscoveryRunRepository),
            typeof(LiteDbCandidateMRRepository),
            typeof(LiteDbValidationRunRepository),
            typeof(LiteDbMutationCampaignRepository),
            typeof(LiteDbMutationResultRepository),
            typeof(LiteDbAuditLogRepository),
            typeof(LiteDbBatchRepository),
            typeof(LiteDbReportRepository),
        };
        foreach (var t in guidPkImpls)
        {
            var baseType = t.BaseType;
            Assert.NotNull(baseType);
            Assert.True(baseType.IsGenericType);
            Assert.Equal(typeof(LiteDbGuidPkRepositoryBase<>),
                baseType.GetGenericTypeDefinition());
        }
    }
}
