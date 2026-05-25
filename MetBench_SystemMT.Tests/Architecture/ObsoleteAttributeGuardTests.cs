#pragma warning disable CS0618 // tests intentionally reference the obsolete types they guard
using System.Reflection;
using Xunit;

namespace MetBench_SystemMT.Tests.Architecture;

/// <summary>
/// 守卫测试 — 验证仍标 deprecated 的 v1 路径仍带 <see cref="ObsoleteAttribute"/>。
/// 覆盖：
/// <list type="bullet">
///   <item>G-05: <c>Latextosympy</c> / <c>Latextosympy_Await</c>（LaTeX → sympy 老路径）</item>
/// </list>
/// 若有人去掉 [Obsolete]，本测试 fail，回归人工 review。
/// W1 SystemMtRunner 的守卫已随 PR-D 类型删除而失效：参见
/// <see cref="SemanticCatalogBoundaryTests"/> 对 IMrAssertion / AssertionEvaluator /
/// AssertionTypeCodes 字符串分派的生产侧守卫。
/// </summary>
public sealed class ObsoleteAttributeGuardTests
{
    [Theory]
    [InlineData(typeof(MetBench_BLL.Latextosympy))]
    [InlineData(typeof(MetBench_BLL.Latextosympy_Await))]
    [InlineData(typeof(MetBench_BLL.SystemMT.Catalog.HardcodedMrCatalogProvider))]
    public void Legacy_type_must_carry_ObsoleteAttribute(Type type)
    {
        var attr = type.GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(attr);
        Assert.False(string.IsNullOrWhiteSpace(attr.Message),
            $"{type.Name} [Obsolete] must carry a non-empty message explaining the replacement path.");
    }

    [Fact]
    public void HardcodedMrCatalogProvider_obsolete_message_points_to_ManifestMrCatalogProvider()
    {
        var attr = typeof(MetBench_BLL.SystemMT.Catalog.HardcodedMrCatalogProvider)
            .GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(attr);
        Assert.Contains("ManifestMrCatalogProvider", attr.Message);
        Assert.Contains("Task 7", attr.Message);
    }

    [Fact]
    public void Latextosympy_obsolete_message_points_to_MethodTransformationRegistry()
    {
        var attr = typeof(MetBench_BLL.Latextosympy).GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(attr);
        Assert.Contains("MethodTransformationRegistry", attr.Message);
        Assert.Contains("EquationFunction", attr.Message);
    }
}
