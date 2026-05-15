using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

/// <summary>
/// 通用 v2 Reqnroll step bindings (P5.2)。
/// 一组 5 个 step 覆盖任意 v2 MR scenario；与具体 MR / SUT 无关，
/// 由 `.feature` 文件的 Examples 表参数化。
/// </summary>
/// <remarks>
/// 该 binding 设计为**契约层**，实际执行 pipeline 需要：
/// 1. LiteDB 已加载 MRSchema / MRBinding 数据（由 P8 端到端验收时 catalog 导入）
/// 2. SUT 的 Python parser + runner 在测试 host 可调用
/// 3. App.config 含 litedb 连接字符串
///
/// 在 Linux cloud 测试环境里（无 App.config / 无 LiteDB 数据），
/// step bindings 自动 Skip — 这是 CLAUDE.md "cloud agent 不可编译 WPF"
/// 约束的延伸："cloud agent 不可端到端跑 WPF 用例"。
///
/// VM-side（Windows + WPF）跑这些 step bindings 时，应:
/// 1. 在 App.config 设 litedb 连接字符串
/// 2. 跑 P5.3 迁移脚本把 catalog 导入 LiteDB
/// 3. dotnet test 自动跑全部 .feature
/// </remarks>
[Binding]
public sealed class SystemMtPipelineV2Steps
{
    private string _mrCode = string.Empty;
    private string _sutName = string.Empty;
    private string _sampleCase = string.Empty;
    private string _abstractField = string.Empty;
    private readonly Dictionary<string, string> _params = new();
    private PipelineOutcome? _outcome;

    [Given(@"the MR Schema ""(.*)"" is bound to SUT ""(.*)""")]
    public void GivenMrBoundToSut(string mrCode, string sutName)
    {
        _mrCode = mrCode;
        _sutName = sutName;
    }

    [Given(@"the binding uses sample case ""(.*)""")]
    public void GivenSampleCase(string sample)
    {
        _sampleCase = sample;
    }

    [Given(@"the parameter mapping for ""(.*)"" is configured")]
    public void GivenParameterMappingConfigured(string abstractField)
    {
        _abstractField = abstractField;
    }

    [When(@"the MT pipeline runs with parameter ""(.*)""=""(.*)""")]
    public async Task WhenPipelineRunsWithParameter(string name, string value)
    {
        _params[name] = value;

        // Linux cloud env: 无 LiteDB catalog；自动 skip。
        // VM-side 实际跑时由 Service 层准备 ctx + 调 SystemMtPipeline.
        Skip.If(IsCloudWithoutLiteDb(),
                "Cloud environment without LiteDB catalog; this step requires " +
                "VM-side execution with App.config + populated catalog. See P5.2 remarks.");

        // 占位：VM-side 实现时取代为真实 PipelineContext 准备 + pipeline 调用
        await Task.CompletedTask;
    }

    [Then(@"the (noise-aware )?""(.*)"" assertion holds on ""(.*)""")]
    public void ThenAssertionHolds(string noiseAwarePrefix, string assertionCode, string valueName)
    {
        Skip.If(IsCloudWithoutLiteDb(),
                "Cloud environment without LiteDB catalog; see P5.2 remarks.");

        Assert.NotNull(_outcome);
        Assert.NotNull(_outcome!.AssertionResult);

        // 检查 AssertionTypeCode 与 .feature 标签一致
        var expectedCode = noiseAwarePrefix?.Trim() == "noise-aware"
            ? $"{assertionCode}-noise-aware"
            : assertionCode;
        Assert.Equal(expectedCode, _outcome.AssertionResult!.AssertionTypeCode);
        Assert.True(_outcome.AssertionResult.Passed, _outcome.AssertionResult.FailureReason);
    }

    private static bool IsCloudWithoutLiteDb()
    {
        // 环境检测：是否处于 cloud (Linux + 无 App.config) 测试场景
        if (!OperatingSystem.IsLinux()) return false;
        // 简化判断：测试时取 connectionString，找不到 / 抛异常即 cloud
        try
        {
            var cs = System.Configuration.ConfigurationManager
                .ConnectionStrings["litedb"]?.ConnectionString;
            return string.IsNullOrEmpty(cs);
        }
        catch
        {
            return true;
        }
    }
}
