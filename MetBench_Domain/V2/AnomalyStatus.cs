using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 异常调查状态机的强类型状态。LiteDB 持久化为 <see cref="int"/>（见
/// <see cref="AnomalyStatuses.RegisterBsonMapping"/>）；UI / 报表 / 审计走 kebab 字符串
/// （见 <see cref="AnomalyStatuses.ToKebab"/>）。
/// </summary>
/// <remarks>
/// 取值固定为显式整数，禁止重排——LiteDB 存的是 int，重排会破坏既有数据。
/// 状态机合法转移见 <see cref="AnomalyStatuses.CanTransition"/>。
/// </remarks>
public enum AnomalyStatus
{
    /// <summary>未指定 / 未知（防御性默认，迁移时无法识别的旧值落到这里）。</summary>
    Unspecified = 0,

    /// <summary>新发现，尚未开始调查。</summary>
    New = 1,

    /// <summary>调查中。</summary>
    Investigating = 2,

    /// <summary>已知现象（非缺陷或已记录）。</summary>
    Known = 3,

    /// <summary>已确认缺陷。</summary>
    ConfirmedBug = 4,

    /// <summary>误报。</summary>
    FalsePositive = 5,

    /// <summary>上游已修复。</summary>
    FixedUpstream = 6,
}

/// <summary>
/// <see cref="AnomalyStatus"/> 的 kebab 字符串映射、状态机转移表与 LiteDB 序列化注册。
/// </summary>
/// <remarks>
/// 这些都是确定性逻辑，必须由代码裁决（CLAUDE.md §1.3），不交给模型 / 散落字符串判断。
/// </remarks>
public static class AnomalyStatuses
{
    // enum → kebab（双向映射的单一真相源）
    private static readonly IReadOnlyDictionary<AnomalyStatus, string> s_toKebab =
        new Dictionary<AnomalyStatus, string>
        {
            [AnomalyStatus.Unspecified] = "unspecified",
            [AnomalyStatus.New] = "new",
            [AnomalyStatus.Investigating] = "investigating",
            [AnomalyStatus.Known] = "known",
            [AnomalyStatus.ConfirmedBug] = "confirmed-bug",
            [AnomalyStatus.FalsePositive] = "false-positive",
            [AnomalyStatus.FixedUpstream] = "fixed-upstream",
        };

    private static readonly IReadOnlyDictionary<string, AnomalyStatus> s_fromKebab =
        s_toKebab.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    // 合法转移表（状态机）。未列入的 from/to 组合一律非法。
    private static readonly IReadOnlyDictionary<AnomalyStatus, IReadOnlyList<AnomalyStatus>> s_transitions =
        new Dictionary<AnomalyStatus, IReadOnlyList<AnomalyStatus>>
        {
            [AnomalyStatus.New] = new[] { AnomalyStatus.Investigating },
            [AnomalyStatus.Investigating] = new[]
            {
                AnomalyStatus.Known,
                AnomalyStatus.ConfirmedBug,
                AnomalyStatus.FalsePositive,
                AnomalyStatus.FixedUpstream,
            },
        };

    /// <summary>enum → kebab 字符串（UI / 报表 / 审计用）。</summary>
    public static string ToKebab(this AnomalyStatus status) =>
        s_toKebab.TryGetValue(status, out var kebab) ? kebab : "unspecified";

    /// <summary>kebab 字符串 → enum。识别不了返回 false（不抛）。</summary>
    public static bool TryParseKebab(string? kebab, out AnomalyStatus status)
    {
        if (!string.IsNullOrWhiteSpace(kebab) && s_fromKebab.TryGetValue(kebab.Trim(), out status))
        {
            return true;
        }
        status = AnomalyStatus.Unspecified;
        return false;
    }

    /// <summary>kebab 字符串 → enum；识别不了抛 <see cref="ArgumentException"/>。</summary>
    public static AnomalyStatus ParseKebab(string kebab) =>
        TryParseKebab(kebab, out var status)
            ? status
            : throw new ArgumentException($"Unknown anomaly status kebab '{kebab}'.", nameof(kebab));

    /// <summary>
    /// 反序列化容错版：先按 kebab 解析，失败再按 enum 名解析（防御历史写法），
    /// 仍失败落到 <see cref="AnomalyStatus.Unspecified"/>。供 BSON 反序列化与迁移共用。
    /// </summary>
    public static AnomalyStatus ParseLenient(string? raw)
    {
        if (TryParseKebab(raw, out var status))
        {
            return status;
        }
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), ignoreCase: true, out AnomalyStatus byName))
        {
            return byName;
        }
        return AnomalyStatus.Unspecified;
    }

    /// <summary>该状态允许转移到的目标状态集合（不可转移返回空）。</summary>
    public static IReadOnlyList<AnomalyStatus> AllowedNext(this AnomalyStatus from) =>
        s_transitions.TryGetValue(from, out var next) ? next : Array.Empty<AnomalyStatus>();

    /// <summary>是否允许从 <paramref name="from"/> 转移到 <paramref name="to"/>。</summary>
    public static bool CanTransition(this AnomalyStatus from, AnomalyStatus to) =>
        AllowedNext(from).Contains(to);

    /// <summary>
    /// 把 <see cref="AnomalyStatus"/> 注册为 LiteDB 的 int 序列化（而非默认 enum-name 字符串）。
    /// 反序列化兼容历史 string（kebab / enum 名）值，便于无损读取旧数据。
    /// 幂等：重复注册仅覆盖同一 type handler。
    /// 生产路径由 <c>DbConfig</c> 调用；不经 DbConfig 的进程（如 seeder）需自行调用。
    /// </summary>
    public static void RegisterBsonMapping(BsonMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        mapper.RegisterType<AnomalyStatus>(
            serialize: status => new BsonValue((int)status),
            deserialize: bson => bson.IsNumber
                ? (AnomalyStatus)bson.AsInt32
                : bson.IsString
                    ? ParseLenient(bson.AsString)
                    : AnomalyStatus.Unspecified);
    }
}
