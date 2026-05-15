using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// Application × Domain junction — 替代 v1 Application.DomainName 多值字符串反模式。
/// 一个 Application 可属于多个 Domain（核物理 + 反应堆物理 + V&V），同样一个 Domain 有多个 Application。
/// </summary>
/// <remarks>
/// 索引：(ApplicationId, DomainId) 复合唯一。
/// </remarks>
public class ApplicationDomain
{
    [BsonId] public int IdJunction { get; set; }

    /// <summary>→ Applications.IdApplication。</summary>
    public int ApplicationId { get; set; }

    /// <summary>→ Domains.IdDomain。</summary>
    public int DomainId { get; set; }
}
