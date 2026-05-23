using System.Reflection;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.Architecture;

/// <summary>
/// 守卫测试 — 验证已 deprecated 的 v1/W1 路径仍带 <see cref="ObsoleteAttribute"/>。
/// 覆盖：
/// <list type="bullet">
///   <item>G-05: <c>Latextosympy</c> / <c>Latextosympy_Await</c>（LaTeX → sympy 老路径）</item>
///   <item>G-07: <c>SystemMtRunner</c>（W1 系统级 MT runner）</item>
/// </list>
/// 若有人去掉 [Obsolete]，本测试 fail，回归人工 review。
/// </summary>
public sealed class ObsoleteAttributeGuardTests
{
    [Theory]
    [InlineData(typeof(MetBench_BLL.Latextosympy))]
    [InlineData(typeof(MetBench_BLL.Latextosympy_Await))]
    [InlineData(typeof(SystemMtRunner))]
    public void Legacy_type_must_carry_ObsoleteAttribute(Type type)
    {
        var attr = type.GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(attr);
        Assert.False(string.IsNullOrWhiteSpace(attr.Message),
            $"{type.Name} [Obsolete] must carry a non-empty message explaining the replacement path.");
    }

    [Fact]
    public void Latextosympy_obsolete_message_points_to_MethodTransformationRegistry()
    {
        var attr = typeof(MetBench_BLL.Latextosympy).GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(attr);
        Assert.Contains("MethodTransformationRegistry", attr.Message);
        Assert.Contains("EquationFunction", attr.Message);
    }

    [Fact]
    public void SystemMtRunner_obsolete_message_points_to_ISystemMtPipeline()
    {
        var attr = typeof(SystemMtRunner).GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(attr);
        Assert.Contains("ISystemMtPipeline", attr.Message);
        Assert.Contains("Stage 9", attr.Message);
    }
}
