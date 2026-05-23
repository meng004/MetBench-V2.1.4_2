using MetBench_Domain.V2.Enums;
using Xunit;

namespace MetBench_SystemMT.Tests.V3MrLibrary;

/// <summary>
/// S8-P5 review fix: 把 7 个 V3 5D-tag enum 的整数底层值钉死，防止后续
/// 重新排序 / 在中间插入新成员悄无声息腐蚀 LiteDB v3_metamorphic_relations
/// 历史行（BsonMapper 默认按 int 序列化 enum）。
///
/// 修改 enum 顺序时本测试会失败，提醒走"追加在末尾"路径并加配套数据迁移。
/// </summary>
public sealed class V3EnumStabilityPinningTests
{
    [Fact]
    public void EquationKind_int_values_are_pinned()
    {
        Assert.Equal(0, (int)EquationKind.Unspecified);
        Assert.Equal(1, (int)EquationKind.Boltzmann);
        Assert.Equal(2, (int)EquationKind.Diffusion);
        Assert.Equal(3, (int)EquationKind.Bateman);
        Assert.Equal(4, (int)EquationKind.Fourier);
        Assert.Equal(5, (int)EquationKind.NavierStokes);
        Assert.Equal(6, (int)EquationKind.Other);
    }

    [Fact]
    public void ProgramKind_int_values_are_pinned()
    {
        Assert.Equal(0, (int)ProgramKind.Unspecified);
        Assert.Equal(1, (int)ProgramKind.Num);
        Assert.Equal(2, (int)ProgramKind.MC);
        Assert.Equal(3, (int)ProgramKind.Surr);
        Assert.Equal(4, (int)ProgramKind.PINN);
        Assert.Equal(5, (int)ProgramKind.Analytic);
    }

    [Fact]
    public void MetaPatternKind_int_values_are_pinned()
    {
        Assert.Equal(0, (int)MetaPatternKind.Unspecified);
        Assert.Equal(1, (int)MetaPatternKind.Mono);
        Assert.Equal(2, (int)MetaPatternKind.Inv);
        Assert.Equal(3, (int)MetaPatternKind.Conv);
        Assert.Equal(4, (int)MetaPatternKind.Part);
        Assert.Equal(5, (int)MetaPatternKind.Traj);
        Assert.Equal(6, (int)MetaPatternKind.Cmp);
        Assert.Equal(7, (int)MetaPatternKind.Rev);
        Assert.Equal(8, (int)MetaPatternKind.Dyn);
        Assert.Equal(9, (int)MetaPatternKind.Adj);
        Assert.Equal(10, (int)MetaPatternKind.Rel);
    }

    [Fact]
    public void SourceLevelKind_int_values_are_pinned()
    {
        Assert.Equal(0, (int)SourceLevelKind.Unspecified);
        Assert.Equal(1, (int)SourceLevelKind.Manual);
        Assert.Equal(2, (int)SourceLevelKind.Literature);
        Assert.Equal(3, (int)SourceLevelKind.MetaPrompt);
        Assert.Equal(4, (int)SourceLevelKind.MultiLlmConsensus);
        Assert.Equal(5, (int)SourceLevelKind.ScgHeuristic);
    }

    [Fact]
    public void FailureCorrelationKind_int_values_are_pinned()
    {
        Assert.Equal(0, (int)FailureCorrelationKind.Unspecified);
        Assert.Equal(1, (int)FailureCorrelationKind.None);
        Assert.Equal(2, (int)FailureCorrelationKind.RealBug);
        Assert.Equal(3, (int)FailureCorrelationKind.MutationKill);
        Assert.Equal(4, (int)FailureCorrelationKind.Vacuous);
    }

    [Fact]
    public void RelationKind_int_values_are_pinned()
    {
        Assert.Equal(0, (int)RelationKind.Unspecified);
        Assert.Equal(1, (int)RelationKind.Ordinal);
        Assert.Equal(2, (int)RelationKind.Equality);
        Assert.Equal(3, (int)RelationKind.ScaledEquality);
        Assert.Equal(4, (int)RelationKind.DistributionEquivalence);
        Assert.Equal(5, (int)RelationKind.CrossProgramAgreement);
    }

    [Fact]
    public void RigorClassKind_int_values_are_pinned()
    {
        Assert.Equal(0, (int)RigorClassKind.Unspecified);
        Assert.Equal(1, (int)RigorClassKind.A);
        Assert.Equal(2, (int)RigorClassKind.B);
        Assert.Equal(3, (int)RigorClassKind.C);
    }
}
