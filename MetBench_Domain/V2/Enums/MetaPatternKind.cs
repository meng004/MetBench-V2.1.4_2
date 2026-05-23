namespace MetBench_Domain.V2.Enums;

/// <summary>
/// NOETHER 元模式分类，V3 MR 5D tag 第 3 维（What pattern of relation?）。
/// 与既有 <c>MetamorphicRelation.MetaPatternCode</c> 字符串映射。
/// </summary>
public enum MetaPatternKind
{
    /// <summary>未指定 / 自定义。</summary>
    Unspecified,
    /// <summary>m_mono — 单调缩放（输入加倍，输出按某种单调关系）。</summary>
    Mono,
    /// <summary>m_inv — 不变性 / 守恒（变换前后某量保持不变）。</summary>
    Inv,
    /// <summary>m_conv — 数值收敛（mesh/timestep 细化输出在容差内不变）。</summary>
    Conv,
    /// <summary>m_part — 划分 / 跨实现一致性（同物理问题不同求解器结果一致）。</summary>
    Part,
    /// <summary>m_traj — 轨迹等价（A→B→C vs A→C 一步等价）。</summary>
    Traj,
    /// <summary>m_cmp — 比较性（A 案例输出 vs B 案例输出有序）。</summary>
    Cmp,
    /// <summary>m_rev — 时间反演 / 算子对易。</summary>
    Rev,
    /// <summary>m_dyn — 动力学性质（如稳定性、周期性）。</summary>
    Dyn,
    /// <summary>m_adj — 伴随对称（forward/adjoint 解性质）。</summary>
    Adj,
    /// <summary>m_rel — 关系型约束（多 MR 联合性质）。</summary>
    Rel,
}
