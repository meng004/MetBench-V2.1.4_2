using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Anomaly;

/// <summary>
/// debt #5 — <see cref="AnomalyStatus"/> 的 kebab 双向映射与状态机转移表。
/// 这些是确定性逻辑（CLAUDE.md §1.3），必须有真断言守护。
/// </summary>
public sealed class AnomalyStatusTests
{
    [Theory]
    [InlineData(AnomalyStatus.Unspecified, "unspecified")]
    [InlineData(AnomalyStatus.New, "new")]
    [InlineData(AnomalyStatus.Investigating, "investigating")]
    [InlineData(AnomalyStatus.Known, "known")]
    [InlineData(AnomalyStatus.ConfirmedBug, "confirmed-bug")]
    [InlineData(AnomalyStatus.FalsePositive, "false-positive")]
    [InlineData(AnomalyStatus.FixedUpstream, "fixed-upstream")]
    public void ToKebab_and_TryParseKebab_roundtrip(AnomalyStatus status, string kebab)
    {
        Assert.Equal(kebab, status.ToKebab());
        Assert.True(AnomalyStatuses.TryParseKebab(kebab, out var parsed));
        Assert.Equal(status, parsed);
    }

    [Fact]
    public void Enum_int_values_are_pinned_for_litedb_storage()
    {
        // LiteDB 存 int；重排会破坏既有数据。锁死取值。
        Assert.Equal(0, (int)AnomalyStatus.Unspecified);
        Assert.Equal(1, (int)AnomalyStatus.New);
        Assert.Equal(2, (int)AnomalyStatus.Investigating);
        Assert.Equal(3, (int)AnomalyStatus.Known);
        Assert.Equal(4, (int)AnomalyStatus.ConfirmedBug);
        Assert.Equal(5, (int)AnomalyStatus.FalsePositive);
        Assert.Equal(6, (int)AnomalyStatus.FixedUpstream);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bogus")]
    [InlineData("confirmedbug")] // 非 kebab 形态
    public void TryParseKebab_unknown_returns_false_and_unspecified(string? raw)
    {
        Assert.False(AnomalyStatuses.TryParseKebab(raw, out var status));
        Assert.Equal(AnomalyStatus.Unspecified, status);
    }

    [Fact]
    public void TryParseKebab_is_case_insensitive()
    {
        Assert.True(AnomalyStatuses.TryParseKebab("Confirmed-Bug", out var status));
        Assert.Equal(AnomalyStatus.ConfirmedBug, status);
    }

    [Fact]
    public void ParseKebab_throws_on_unknown()
    {
        Assert.Throws<ArgumentException>(() => AnomalyStatuses.ParseKebab("nope"));
    }

    [Theory]
    [InlineData("new", AnomalyStatus.New)]
    [InlineData("confirmed-bug", AnomalyStatus.ConfirmedBug)]
    [InlineData("ConfirmedBug", AnomalyStatus.ConfirmedBug)] // 防御历史 enum-name 写法
    [InlineData("garbage", AnomalyStatus.Unspecified)]
    [InlineData(null, AnomalyStatus.Unspecified)]
    public void ParseLenient_handles_kebab_enumname_and_garbage(string? raw, AnomalyStatus expected)
    {
        Assert.Equal(expected, AnomalyStatuses.ParseLenient(raw));
    }

    // ===== 状态机 =====

    [Theory]
    [InlineData(AnomalyStatus.New, AnomalyStatus.Investigating)]
    [InlineData(AnomalyStatus.Investigating, AnomalyStatus.Known)]
    [InlineData(AnomalyStatus.Investigating, AnomalyStatus.ConfirmedBug)]
    [InlineData(AnomalyStatus.Investigating, AnomalyStatus.FalsePositive)]
    [InlineData(AnomalyStatus.Investigating, AnomalyStatus.FixedUpstream)]
    public void CanTransition_allows_legal_moves(AnomalyStatus from, AnomalyStatus to)
    {
        Assert.True(from.CanTransition(to));
    }

    [Theory]
    [InlineData(AnomalyStatus.New, AnomalyStatus.ConfirmedBug)]      // 必须先 investigating
    [InlineData(AnomalyStatus.New, AnomalyStatus.New)]               // 自环非法
    [InlineData(AnomalyStatus.Investigating, AnomalyStatus.New)]     // 不可回退
    [InlineData(AnomalyStatus.ConfirmedBug, AnomalyStatus.Known)]    // 终态不可再转
    [InlineData(AnomalyStatus.Known, AnomalyStatus.Investigating)]   // 终态不可再转
    [InlineData(AnomalyStatus.Unspecified, AnomalyStatus.New)]       // 未指定无出边
    [InlineData(AnomalyStatus.New, AnomalyStatus.Unspecified)]       // 不可转向未指定
    public void CanTransition_rejects_illegal_moves(AnomalyStatus from, AnomalyStatus to)
    {
        Assert.False(from.CanTransition(to));
    }

    [Fact]
    public void AllowedNext_matches_transition_table()
    {
        Assert.Equal(new[] { AnomalyStatus.Investigating }, AnomalyStatus.New.AllowedNext());
        Assert.Equal(
            new[]
            {
                AnomalyStatus.Known,
                AnomalyStatus.ConfirmedBug,
                AnomalyStatus.FalsePositive,
                AnomalyStatus.FixedUpstream,
            },
            AnomalyStatus.Investigating.AllowedNext());
        Assert.Empty(AnomalyStatus.ConfirmedBug.AllowedNext());
        Assert.Empty(AnomalyStatus.Unspecified.AllowedNext());
    }
}
