namespace MetBench_Domain.V2.Enums;

/// <summary>
/// 反应堆物理 / 通用 MT 平台支持的方程键（5 方程 + 通用扩展位）。
/// 与 <c>EquationMetadata.EquationKey</c> 业务字符串对齐，作为 V3 MR
/// 5D tag 的第 1 维（What equation does the MR exercise?）。
/// </summary>
/// <remarks>
/// **APPEND-ONLY**：LiteDB BsonMapper 默认按底层 int 序列化。重新排序或在中间
/// 插入新值会让历史 V3 DB 行的 enum 值悄无声息映射到错误成员。新增方程只能
/// 追加到末尾，且显式赋整数值锁定。Stage 8 P5 review fix。
/// </remarks>
public enum EquationKind
{
    /// <summary>未指定 / 自定义（v2 老数据迁移默认值）。</summary>
    Unspecified = 0,
    /// <summary>Boltzmann 中子输运（OpenMOC / OpenMC）。</summary>
    Boltzmann = 1,
    /// <summary>中子扩散方程（diffusion_1d / nodal 简化）。</summary>
    Diffusion = 2,
    /// <summary>Bateman 衰变链（decay_chain）。</summary>
    Bateman = 3,
    /// <summary>Fourier 热传导（heat_equation）。</summary>
    Fourier = 4,
    /// <summary>Navier-Stokes 流体（subchannel_1d 等简化）。</summary>
    NavierStokes = 5,
    /// <summary>非反应堆物理（projectile / damped-oscillator / lotka-volterra 等通用方程）。</summary>
    Other = 6,
}
