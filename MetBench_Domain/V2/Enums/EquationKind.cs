namespace MetBench_Domain.V2.Enums;

/// <summary>
/// 反应堆物理 / 通用 MT 平台支持的方程键（5 方程 + 通用扩展位）。
/// 与 <c>EquationMetadata.EquationKey</c> 业务字符串对齐，作为 V3 MR
/// 5D tag 的第 1 维（What equation does the MR exercise?）。
/// </summary>
public enum EquationKind
{
    /// <summary>未指定 / 自定义（v2 老数据迁移默认值）。</summary>
    Unspecified,
    /// <summary>Boltzmann 中子输运（OpenMOC / OpenMC）。</summary>
    Boltzmann,
    /// <summary>中子扩散方程（diffusion_1d / nodal 简化）。</summary>
    Diffusion,
    /// <summary>Bateman 衰变链（decay_chain）。</summary>
    Bateman,
    /// <summary>Fourier 热传导（heat_equation）。</summary>
    Fourier,
    /// <summary>Navier-Stokes 流体（subchannel_1d 等简化）。</summary>
    NavierStokes,
    /// <summary>非反应堆物理（projectile / damped-oscillator / lotka-volterra 等通用方程）。</summary>
    Other,
}
