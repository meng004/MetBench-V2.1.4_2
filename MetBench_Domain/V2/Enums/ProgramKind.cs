namespace MetBench_Domain.V2.Enums;

/// <summary>
/// SUT 求解器实现类型，V3 MR 5D tag 第 2 维（What kind of solver implements it?）。
/// </summary>
public enum ProgramKind
{
    /// <summary>未指定（v2 默认值）。</summary>
    Unspecified,
    /// <summary>数值求解器（FD / FEM / RK / nodal — 确定性 PDE/ODE 数值方法）。</summary>
    Num,
    /// <summary>Monte Carlo 求解器（OpenMC / Serpent 类）。</summary>
    MC,
    /// <summary>代理模型（surrogate；ML 代理 / 多项式回归等）。</summary>
    Surr,
    /// <summary>Physics-Informed Neural Network。</summary>
    PINN,
    /// <summary>闭式解析解（Bateman / projectile / subchannel 1D 等）。</summary>
    Analytic,
}
