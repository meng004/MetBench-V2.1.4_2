namespace MetBench_BLL.SystemMT.Metadata;

/// <summary>
/// Seed data for the System-MT reference metadata: structured descriptions of
/// the equations the SUT catalog solves and the metamorphic relations it
/// exposes. Lifts the equation/MR documentation out of source-only docstrings
/// into a queryable, persistable form.
/// </summary>
/// <remarks>
/// The MR set here is kept in lock-step with the launcher's hard-coded MR
/// catalog — <c>SystemMtMetadataCatalogTests</c> asserts the two id sets are
/// equal, so adding a catalog MR without metadata fails the build.
/// </remarks>
public static class SystemMtMetadataCatalog
{
    /// <summary>Structured metadata for every equation a catalog SUT solves.</summary>
    public static IReadOnlyList<EquationMetadata> Equations { get; } = new[]
    {
        new EquationMetadata
        {
            EquationKey = "neutron-transport",
            Name = "中子输运方程（Boltzmann 输运方程）",
            CanonicalForm = "Ω·∇ψ + Σ_t·ψ = ∫Σ_s·ψ dΩ' + (1/k)·χ·∫νΣ_f·ψ dΩ'",
            SymbolSystem =
                "ψ 角通量；Ω 中子飞行方向；Σ_t/Σ_s/Σ_f 总/散射/裂变宏观截面；" +
                "Σ_a 吸收截面；ν 每次裂变中子数；χ 裂变能谱；k 有效增殖因子 k_eff。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "Σ_a", Description = "燃料宏观吸收截面", Unit = "1/cm" },
                new() { Symbol = "νΣ_f", Description = "裂变中子产生截面", Unit = "1/cm" },
                new() { Symbol = "k_eff", Description = "有效增殖因子（本征值）", Unit = "无量纲" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "heat-equation-1d",
            Name = "一维热传导方程（Fourier 扩散方程）",
            CanonicalForm = "∂u/∂t = α·∂²u/∂x²",
            SymbolSystem = "u(x,t) 温度场；α 热扩散系数；x 空间坐标；t 时间。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "u", Description = "温度场（初值即初始温度分布）", Unit = "K" },
                new() { Symbol = "α", Description = "热扩散系数", Unit = "m²/s" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "bateman",
            Name = "Bateman 衰变链方程",
            CanonicalForm = "dN_i/dt = λ_{i-1}·N_{i-1} − λ_i·N_i",
            SymbolSystem = "N_i 核素 i 的原子数；λ_i 核素 i 的衰变常数；i 衰变链上的核素序号。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "N_i", Description = "核素 i 的（初始）原子数", Unit = "atoms" },
                new() { Symbol = "λ_i", Description = "核素 i 的衰变常数", Unit = "1/s" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "damped-oscillator",
            Name = "阻尼谐振子方程",
            CanonicalForm = "x'' + 2ζω·x' + ω²·x = 0",
            SymbolSystem = "x(t) 位移；ζ 阻尼比；ω 固有角频率；(x0, v0) 初始位移与速度。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "x0", Description = "初始位移", Unit = "m" },
                new() { Symbol = "v0", Description = "初始速度", Unit = "m/s" },
                new() { Symbol = "ζ", Description = "阻尼比", Unit = "无量纲" },
                new() { Symbol = "ω", Description = "固有角频率", Unit = "rad/s" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "lotka-volterra",
            Name = "Lotka-Volterra 捕食者-猎物方程",
            CanonicalForm = "dx/dt = α·x − β·x·y;  dy/dt = δ·x·y − γ·y",
            SymbolSystem =
                "x 猎物数量；y 捕食者数量；α 猎物自然增长率；β 捕食率；" +
                "δ 捕食转化率；γ 捕食者死亡率。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "α", Description = "猎物自然增长率", Unit = "1/t" },
                new() { Symbol = "β", Description = "捕食率", Unit = "1/t" },
                new() { Symbol = "δ", Description = "捕食转化率", Unit = "1/t" },
                new() { Symbol = "γ", Description = "捕食者死亡率", Unit = "1/t" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "diffusion",
            Name = "中子扩散方程（1D 单群稳态简化）",
            CanonicalForm = "-D·∂²φ/∂x² + Σ_a·φ = S",
            SymbolSystem =
                "φ(x) 中子通量 [n/(cm²·s)]；D 扩散系数 [cm]；Σ_a 宏观吸收截面 [1/cm]；" +
                "S(x) 外中子源 [n/(cm³·s)]；L_dif = √(D/Σ_a) 扩散长度 [cm]。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "D", Description = "扩散系数", Unit = "cm" },
                new() { Symbol = "Σ_a", Description = "宏观吸收截面", Unit = "1/cm" },
                new() { Symbol = "S", Description = "外中子源", Unit = "n/(cm³·s)" },
                new() { Symbol = "φ_max", Description = "峰值通量（输出）", Unit = "n/(cm²·s)" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "navier-stokes",
            Name = "Navier-Stokes 方程（1D 单通道稳态简化）",
            CanonicalForm =
                "质量：G = const；动量：dp/dz = -f·G²/(2ρ·D_h)；能量：G·c_p·dT/dz = q''·P_h/A_xs",
            SymbolSystem =
                "G 质量流密度 [kg/(m²·s)]；p 压力 [Pa]；T 温度 [K]；ρ 密度 [kg/m³]；" +
                "c_p 比热 [J/(kg·K)]；f Darcy 摩擦因子；D_h 水力直径 [m]；" +
                "q'' 壁面热流密度 [W/m²]；P_h 加热周长 [m]；A_xs 横截面积 [m²]。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "G", Description = "质量流密度", Unit = "kg/(m²·s)" },
                new() { Symbol = "q''", Description = "壁面热流密度", Unit = "W/m²" },
                new() { Symbol = "f", Description = "Darcy 摩擦因子（闭式输入）", Unit = "无量纲" },
                new() { Symbol = "ΔT", Description = "出入口温升（输出）", Unit = "K" },
                new() { Symbol = "Δp", Description = "通道压降（输出）", Unit = "Pa" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "poisson",
            Name = "Poisson 方程（1D 静态、Dirichlet 零边界）",
            CanonicalForm = "-u''(x) = f,  x ∈ [0, L],  u(0) = u(L) = 0",
            SymbolSystem =
                "u(x) 解（如静电势、稳态位移、稳态温度场等通用椭圆 PDE 解）；" +
                "f 体源（常数或分布）；L 区域长度；u_max 内部峰值（输出）。" +
                "常数源 f 时解析解 u(x) = f·x·(L-x)/2，u_max = f·L²/8。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "f", Description = "体源强度", Unit = "无量纲" },
                new() { Symbol = "L", Description = "区域长度", Unit = "无量纲" },
                new() { Symbol = "u_max", Description = "内部峰值（输出）", Unit = "无量纲" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "advection",
            Name = "1D 线性输运方程（周期边界）",
            CanonicalForm = "∂u/∂t + v·∂u/∂x = 0,  x ∈ [0, L],  周期 BC",
            SymbolSystem =
                "u(x,t) 输运标量（如浓度、温度被动场、密度等）；v 输运速度（标量，符号决定方向）；" +
                "L 区域长度；解析解为纯平移 u(x,t) = u₀(x - v·t mod L)。" +
                "对周期边界条件，∫u dx 严格守恒（独立于 dx / dt）；峰值幅度在一阶迎风格式下因数值耗散而减小。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "v", Description = "输运速度", Unit = "无量纲" },
                new() { Symbol = "u₀(x)", Description = "初始 Gaussian 脉冲（amplitude / center / sigma 参数化）", Unit = "无量纲" },
                new() { Symbol = "peak_amplitude", Description = "末时刻峰值幅度（输出）", Unit = "无量纲" },
                new() { Symbol = "mass_integral", Description = "∫u dx 数值守恒量（输出）", Unit = "无量纲" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "wave",
            Name = "1D 线性波动方程（齐次 Dirichlet 边界、零初始速度）",
            CanonicalForm = "∂²u/∂t² = c²·∂²u/∂x²,  x ∈ [0, L],  u(0,t)=u(L,t)=0,  u_t(x,0)=0",
            SymbolSystem =
                "u(x,t) 位移场；c 波速；L 区域长度；初始 Gaussian u₀(x)。" +
                "d'Alembert 解将零初始速度的 Gaussian 拆成两半幅度对称行波；" +
                "在 c=const 与 Dirichlet 反射下能量泛函 0.5·∫(u_t² + c²·u_x²)dx 严格守恒，" +
                "本 SUT 用代理 0.5·∫u² dx（L² 能量代理）作为输出。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "c", Description = "波速", Unit = "无量纲" },
                new() { Symbol = "u₀(x)", Description = "初始 Gaussian 脉冲（amplitude / center / sigma 参数化）", Unit = "无量纲" },
                new() { Symbol = "peak_amplitude", Description = "末时刻 |u| 峰值（输出）", Unit = "无量纲" },
                new() { Symbol = "energy_proxy", Description = "0.5·∫u² dx 数值能量代理（输出）", Unit = "无量纲" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "burgers",
            Name = "1D 无粘 Burgers 方程（守恒形式、周期边界）",
            CanonicalForm = "∂u/∂t + ∂/∂x(u²/2) = 0,  x ∈ [0, L],  周期 BC",
            SymbolSystem =
                "u(x,t) 守恒变量（标量速度）；F(u)=u²/2 通量；L 区域长度；初始正向 Gaussian u₀(x)。" +
                "代表性的标量非线性双曲守恒律：光滑初值在迎风侧自陡化、有限时间内形成激波；" +
                "激波形成后熵解失去光滑性、L² 范数因激波耗散严格递减，但通量差分（Lax-Friedrichs）" +
                "格式 + 周期边界对总质量 ∫u dx 仍是严格守恒的（与 dx / dt 无关）。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "F(u)", Description = "守恒通量 u²/2", Unit = "无量纲" },
                new() { Symbol = "u₀(x)", Description = "初始正向 Gaussian 脉冲（amplitude / center / sigma 参数化）", Unit = "无量纲" },
                new() { Symbol = "peak_amplitude", Description = "末时刻峰值幅度（输出，受 LxF 数值耗散影响）", Unit = "无量纲" },
                new() { Symbol = "mass_integral", Description = "∫u dx 数值守恒量（输出）", Unit = "无量纲" },
            },
        },
        new EquationMetadata
        {
            EquationKey = "projectile-motion",
            Name = "射程方程（真空、平面、点抛体）",
            CanonicalForm = "R = v0²·sin(2θ)/g",
            SymbolSystem = "R 水平射程；v0 初速度大小；θ 抛射角（相对水平面）；g 重力加速度。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "v0", Description = "初速度大小", Unit = "m/s" },
                new() { Symbol = "θ", Description = "抛射角", Unit = "°" },
                new() { Symbol = "g", Description = "重力加速度", Unit = "m/s²" },
                new() { Symbol = "R", Description = "水平射程（输出）", Unit = "m" },
            },
        },
        // PR-A T1 non-JSON I/O adapter: synthetic "equation" registered solely so
        // the synthetic _test_csv SUT has an EquationKey to bind to. NOT a real
        // physics equation. Leading underscore in the key marks the synthetic nature.
        new EquationMetadata
        {
            EquationKey = "_test_csv",
            Name = "_TestCsv — 合成的 metbench_io 集成测试方程（非物理）",
            CanonicalForm = "echo_value = factor",
            SymbolSystem =
                "factor 输入标量；echo_value 输出标量。本条目仅为 _test_csv 合成 SUT 提供 EquationKey 绑定，" +
                "不代表任何真实物理过程。",
            Parameters = new List<EquationParameter>
            {
                new() { Symbol = "factor", Description = "输入标量（CSV 单字段）", Unit = "无量纲" },
                new() { Symbol = "echo_value", Description = "输出标量（与 factor 数值相同）", Unit = "无量纲" },
            },
        },
    };

    /// <summary>Structured metadata for every MR in the launcher catalog.</summary>
    public static IReadOnlyList<MrMetadata> MetamorphicRelations { get; } = new[]
    {
        new MrMetadata
        {
            MrId = "openmoc-pincell-nu-sigma-f",
            EquationKey = "neutron-transport",
            PhysicalMeaning = "放大裂变中子产生截面 νΣ_f 增强裂变源，单调抬高有效增殖因子 k_eff。",
            InputTransformation = "νΣ_f → factor·νΣ_f（factor > 1）",
            OutputRelation = "k_eff(flw) > k_eff(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "νΣ_f 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "k_eff", PhysicalMeaning = "有效增殖因子（输出）", ValueRange = "k_eff > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "openmoc-pincell-sigma-a",
            EquationKey = "neutron-transport",
            PhysicalMeaning = "放大燃料吸收截面 Σ_a 加剧中子损失，单调压低有效增殖因子 k_eff。",
            InputTransformation = "Σ_a → factor·Σ_a（factor > 1）",
            OutputRelation = "k_eff(flw) < k_eff(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "Σ_a 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "k_eff", PhysicalMeaning = "有效增殖因子（输出）", ValueRange = "k_eff > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "openmoc-pincell-ray-track-convergence",
            EquationKey = "neutron-transport",
            PhysicalMeaning =
                "OpenMOC 角度离散收敛性 (Bol-Alg-01)：细化 num_azim 与 azim_spacing_cm 三相位 " +
                "(16/0.05 → 48/0.0166667 → 128/0.00625) 使 k_eff 误差朝最细参考解收敛。",
            InputTransformation = "(num_azim, azim_spacing_cm) → (factor·num_azim, azim_spacing_cm/factor)；factor > 1",
            OutputRelation = "|k_eff(medium) − k_eff(reference)| ≤ |k_eff(coarse) − k_eff(reference)|（NormKind.Relative）",
            ComparisonType = MrComparisonType.Relative,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "角度细化倍率（per-phase）",       ValueRange = "factor ≥ 1" },
                new() { Symbol = "k_eff",  PhysicalMeaning = "OpenMOC 报告的有效增殖因子（输出）", ValueRange = "k_eff > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "openmc-pincell-nu-sigma-f",
            EquationKey = "neutron-transport",
            PhysicalMeaning =
                "OpenMOC ScaleNuSigmaF MR 的蒙特卡洛对应：放大 νΣ_f 在 OpenMC 多群本征值求解中同样单调抬高 k_eff。",
            InputTransformation = "νΣ_f → factor·νΣ_f（factor > 1）",
            OutputRelation = "k_eff(flw) > k_eff(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "νΣ_f 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "k_eff", PhysicalMeaning = "有效增殖因子（输出）", ValueRange = "k_eff > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "openmc-pincell-sigma-a",
            EquationKey = "neutron-transport",
            PhysicalMeaning =
                "OpenMOC ScaleFuelSigmaA MR 的蒙特卡洛对应：放大 Σ_a 在 OpenMC 多群本征值求解中同样单调压低 k_eff。",
            InputTransformation = "Σ_a → factor·Σ_a（factor > 1）",
            OutputRelation = "k_eff(flw) < k_eff(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "Σ_a 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "k_eff", PhysicalMeaning = "有效增殖因子（输出）", ValueRange = "k_eff > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "openmc-pincell-particle-count-convergence",
            EquationKey = "neutron-transport",
            PhysicalMeaning =
                "对同一 OpenMC pin-cell 算例放大 particles 计数。" +
                "依据 Monte-Carlo 抽样的 1/√N 定律，更大的 N 应使 k_eff 的报告 std error 单调收缩。",
            InputTransformation = "particles → factor·particles（factor > 1）",
            OutputRelation = "k_eff_std(flw) ≤ k_eff_std(src) / √factor × (1 + ToleranceRel)",
            // Relative comparison: kernel checks high.StdError against a (1 + ToleranceRel)
            // band around low.StdError / √factor — equality within a tolerated relative
            // deviation ratio (= MrComparisonType.Relative semantics).
            ComparisonType = MrComparisonType.Relative,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor",    PhysicalMeaning = "particles 放大倍率",                 ValueRange = "factor > 1" },
                new() { Symbol = "k_eff",     PhysicalMeaning = "OpenMC 报告的有效增殖因子（输出）",   ValueRange = "k_eff > 0" },
                new() { Symbol = "k_eff_std", PhysicalMeaning = "k_eff 的标准误（本 MR 校验目标输出）", ValueRange = "k_eff_std > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "heat-equation-amplitude",
            EquationKey = "heat-equation-1d",
            PhysicalMeaning =
                "齐次 Dirichlet 边界下热方程对初始温度场线性，放大初始幅值按同比例放大终态峰值温度。",
            InputTransformation = "u(x,0) → factor·u(x,0)（factor > 1）",
            OutputRelation = "max_u(flw) > max_u(src)（齐次性严格意义下 = factor·max_u(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "初始幅值缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "max_u", PhysicalMeaning = "终态峰值温度（输出）", ValueRange = "max_u > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "fourier-timestep-convergence",
            EquationKey = "heat-equation-1d",
            PhysicalMeaning =
                "Forward-Euler 时间步收敛性：步长减半（num_steps 翻倍）后 max_u 在数值容差内不变 — " +
                "若变化超出容差说明时间积分尚未收敛到细网格 plateau。",
            InputTransformation = "num_steps → factor·num_steps（factor > 1）",
            OutputRelation = "max_u(flw) ≈ max_u(src)（Euler 截断误差容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_steps 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "max_u", PhysicalMeaning = "终态峰值温度（输出）", ValueRange = "max_u > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "fourier-alpha-monotonic",
            EquationKey = "heat-equation-1d",
            PhysicalMeaning =
                "扩散系数 α 越大，定时 t_final 内的扩散平滑越强，终态峰值温度 max_u 越小。",
            InputTransformation = "α → factor·α（factor > 1）",
            OutputRelation = "max_u(flw) < max_u(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "α 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "max_u", PhysicalMeaning = "终态峰值温度（输出）", ValueRange = "max_u > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "decay-chain-scale-initial",
            EquationKey = "bateman",
            PhysicalMeaning =
                "Bateman 衰变链对初始核素数线性，放大全部初始 N 按同比例放大末端核素 C 的积累量。",
            InputTransformation = "N_i(0) → factor·N_i(0)（对所有 i，factor > 1）",
            OutputRelation = "N_C_final(flw) > N_C_final(src)（线性性严格意义下 = factor·N_C_final(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "初始核素数缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "N_C_final", PhysicalMeaning = "末端核素 C 的终态积累量（输出）", ValueRange = "N_C_final ≥ 0" },
            },
        },
        new MrMetadata
        {
            MrId = "bateman-mass-conservation",
            EquationKey = "bateman",
            PhysicalMeaning =
                "Bateman 衰变链 A→B→C 无生成无吸收，总核素数 total = N_A+N_B+N_C 守恒。" +
                "改变衰变率 λ_A 不影响总数。",
            InputTransformation = "λ_A → factor·λ_A（factor > 1）",
            OutputRelation = "total(flw) ≈ total(src)（容差内严格等）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "λ_A 缩放倍率", ValueRange = "factor > 0" },
                new() { Symbol = "total", PhysicalMeaning = "守恒总核素数（输出）", ValueRange = "total = N_A0+N_B0+N_C0" },
            },
        },
        new MrMetadata
        {
            MrId = "bateman-timestep-cauchy",
            EquationKey = "bateman",
            PhysicalMeaning =
                "RK4 时间步长 Cauchy 收敛：步长减半（num_steps 翻倍）应使数值解在 RK4 截断误差容差内不变 — " +
                "若变化超出容差说明 RK4 尚未收敛到细网格 plateau。",
            InputTransformation = "num_steps → factor·num_steps（factor > 1）",
            OutputRelation = "N_C_final(flw) ≈ N_C_final(src)（RK4 截断误差容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_steps 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "N_C_final", PhysicalMeaning = "末端核素 C 的终态积累量（输出）", ValueRange = "N_C_final ≥ 0" },
            },
        },
        new MrMetadata
        {
            MrId = "damped-oscillator-scale-state",
            EquationKey = "damped-oscillator",
            PhysicalMeaning =
                "阻尼谐振子对初始状态 (x0, v0) 线性且齐次，放大初始状态按同比例放大峰值绝对位移。",
            InputTransformation = "(x0, v0) → factor·(x0, v0)（factor > 1）",
            OutputRelation =
                "max_abs_displacement(flw) > max_abs_displacement(src)（齐次性严格意义下 = factor·max_abs_displacement(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "初始状态缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "max_abs_displacement", PhysicalMeaning = "峰值绝对位移（输出）", ValueRange = "≥ 0" },
            },
        },
        new MrMetadata
        {
            MrId = "lotka-volterra-scale-gamma",
            EquationKey = "lotka-volterra",
            PhysicalMeaning =
                "由 Lotka-Volterra 时均恒等式 ⟨prey⟩ = γ/δ，放大捕食者死亡率 γ 必然抬高时均猎物数。",
            InputTransformation = "γ → factor·γ（factor > 1）",
            OutputRelation = "mean_prey(flw) > mean_prey(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "γ（捕食者死亡率）缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "mean_prey", PhysicalMeaning = "时均猎物数（输出）", ValueRange = "mean_prey > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "scipy-ivp-lv-prey-growth-monotone",
            EquationKey = "lotka-volterra",
            PhysicalMeaning =
                "T3C-IVP（SciPy solve_ivp 求解器）：相同 Lotka-Volterra 时均恒等式 ⟨prey⟩ = γ/δ，" +
                "γ 翻倍必使 mean_prey 严格上升 — 自适应 RK45 在 rtol=1e-9/atol=1e-12 下逼真复现连续解，" +
                "误差远小于 strict greater 比较所要求的间距。",
            InputTransformation = "γ → factor·γ（factor > 1）",
            OutputRelation = "mean_prey(flw) > mean_prey(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "γ（捕食者死亡率）缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "mean_prey", PhysicalMeaning = "时均猎物数（输出）", ValueRange = "mean_prey > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "scipy-ivp-lv-step-convergence",
            EquationKey = "lotka-volterra",
            PhysicalMeaning =
                "T3C-IVP eval-grid 细化收敛性：SciPy solve_ivp 自适应步长使解的连续轨迹独立于 " +
                "num_eval_points（仅控制输出采样网格）。num_eval_points 翻倍后 trapezoidal 积分" +
                "得到的 mean_prey 应在 O(Δt²) 容差内不变，收敛到同一连续 ⟨prey⟩ = γ/δ。",
            InputTransformation = "num_eval_points → factor·num_eval_points（factor > 1）",
            OutputRelation = "mean_prey(flw) ≈ mean_prey(src)（O(Δt²) 容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_eval_points 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "mean_prey", PhysicalMeaning = "时均猎物数（输出）", ValueRange = "mean_prey > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "diffusion-source-linearity",
            EquationKey = "diffusion",
            PhysicalMeaning =
                "稳态扩散方程在源项 S 上线性：放大 S 必按同比例放大稳态通量 φ。" +
                "故 φ_max 在 S 上严格单调（且严格意义下成 S∝φ_max 线性）。",
            InputTransformation = "S → factor·S（factor > 1）",
            OutputRelation = "φ_max(flw) > φ_max(src)（严格意义下 = factor·φ_max(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "S 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "φ_max", PhysicalMeaning = "峰值通量（输出）", ValueRange = "φ_max > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "diffusion-mesh-richardson",
            EquationKey = "diffusion",
            PhysicalMeaning =
                "网格细化 Richardson 收敛性：num_points 翻倍（dx 减半）后 φ_max 应在二阶 FD " +
                "截断误差容差内不变 — 若变化超容差说明粗网格尚未收敛到细网格 plateau。",
            InputTransformation = "num_points → factor·num_points（factor > 1）",
            OutputRelation = "φ_max(flw) ≈ φ_max(src)（O(dx²) 截断误差容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_points 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "φ_max", PhysicalMeaning = "峰值通量（输出）", ValueRange = "φ_max > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "poisson-source-superposition",
            EquationKey = "poisson",
            PhysicalMeaning =
                "线性叠加：1D Poisson -u'' = f 在源 f 上严格线性。" +
                "f → factor·f（factor > 1）必使 u_max 严格放大（解析下精确按同比例 = factor·u_max(src)）。",
            InputTransformation = "f → factor·f（factor > 1）",
            OutputRelation = "u_max(flw) > u_max(src)（严格意义下 = factor·u_max(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "源缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "u_max", PhysicalMeaning = "峰值幅度（输出）", ValueRange = "u_max > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "scipy-bvp-poisson-source-superposition",
            EquationKey = "poisson",
            PhysicalMeaning =
                "T3C-BVP（SciPy solve_bvp 求解器）：相同 Poisson 线性叠加 -u'' = f 在源 f 上严格线性，" +
                "f 翻倍必使 u_max 严格放大（解析下精确按同比例 = factor·u_max(src)）。" +
                "solve_bvp 自适应网格在 tol=1e-9 下收敛到解析解，远小于 strict greater 比较所要求的间距。",
            InputTransformation = "f → factor·f（factor > 1）",
            OutputRelation = "u_max(flw) > u_max(src)（严格意义下 = factor·u_max(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "源缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "u_max", PhysicalMeaning = "峰值幅度（输出）", ValueRange = "u_max > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "scipy-bvp-poisson-seed-mesh-insensitivity",
            EquationKey = "poisson",
            PhysicalMeaning =
                "T3C-BVP 自适应 BVP 求解器对种子网格 num_points 不敏感：solve_bvp 自适应细化内部网格 " +
                "直至残差 < tol；用户提供的 num_points 仅为种子。num_points 翻倍后 u_max 应在 " +
                "ToleranceRel=1e-3 / Atol=1e-6 容差内不变，二者均收敛到同一连续解 u(x)=f·x(L−x)/2，其峰值 u_max = f·L²/8 在 x = L/2。",
            InputTransformation = "num_points → factor·num_points（factor > 1）",
            OutputRelation = "u_max(flw) ≈ u_max(src)（O(tol) 容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "种子 num_points 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "u_max", PhysicalMeaning = "峰值幅度（输出）", ValueRange = "u_max > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "poisson-mesh-richardson",
            EquationKey = "poisson",
            PhysicalMeaning =
                "网格细化 Richardson 收敛性：num_points 翻倍（dx 减半）后 u_max 应在 O(dx²) 中心差分 " +
                "截断误差容差内不变 — 若变化超容差说明粗网格尚未收敛到细网格 plateau。" +
                "由于常源 Poisson 解析解 u(x)=f·x·(L-x)/2 是 2 次多项式，2 阶 FD 在足够细网格下应等于解析解。",
            InputTransformation = "num_points → factor·num_points（factor > 1）",
            OutputRelation = "u_max(flw) ≈ u_max(src)（O(dx²) 截断误差容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_points 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "u_max", PhysicalMeaning = "峰值幅度（输出）", ValueRange = "u_max > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "advection-amplitude-linearity",
            EquationKey = "advection",
            PhysicalMeaning =
                "线性叠加：1D 线性输运方程 u_t + v·u_x = 0 关于初始条件严格线性。" +
                "初始 Gaussian amplitude → factor·amplitude（factor > 1）必使末时刻峰值幅度严格放大，" +
                "且在守恒迎风格式下精确按同比例 = factor·peak_amplitude(src)。",
            InputTransformation = "amplitude → factor·amplitude（factor > 1）",
            OutputRelation = "peak_amplitude(flw) > peak_amplitude(src)（严格意义下 = factor·peak_amplitude(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "初始幅度缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "peak_amplitude", PhysicalMeaning = "末时刻峰值幅度（输出）", ValueRange = "peak_amplitude > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "advection-mesh-conservation",
            EquationKey = "advection",
            PhysicalMeaning =
                "质量守恒不变性：1D 线性输运方程在周期边界下 ∫u dx 严格守恒；一阶迎风离散下" +
                "格式逐步守恒（独立于 dx / dt）。num_points 翻倍只改变初始条件采样误差（O(dx²)），" +
                "末时刻 mass_integral 应在 FD 截断容差内不变。",
            InputTransformation = "num_points → factor·num_points（factor > 1）",
            OutputRelation = "mass_integral(flw) ≈ mass_integral(src)（O(dx²) 初始采样误差容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_points 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "mass_integral", PhysicalMeaning = "∫u dx 守恒量（输出）", ValueRange = "mass_integral > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "wave-amplitude-linearity",
            EquationKey = "wave",
            PhysicalMeaning =
                "线性叠加：1D 线性波动方程 u_tt = c²·u_xx 关于初始条件严格线性。" +
                "初始 Gaussian amplitude → factor·amplitude（factor > 1）必使末时刻 |u| 峰值严格放大，" +
                "且在线性 leapfrog 离散下精确按同比例 = factor·peak_amplitude(src)。",
            InputTransformation = "amplitude → factor·amplitude（factor > 1）",
            OutputRelation = "peak_amplitude(flw) > peak_amplitude(src)（严格意义下 = factor·peak_amplitude(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "初始幅度缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "peak_amplitude", PhysicalMeaning = "末时刻 |u| 峰值（输出）", ValueRange = "peak_amplitude > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "wave-mesh-energy-convergence",
            EquationKey = "wave",
            PhysicalMeaning =
                "网格细化收敛：2 阶 leapfrog 格式 O(dx²,dt²) 收敛。num_points 翻倍（内部 dt 同步减半保持 Courant=0.5）" +
                "后末时刻 L² 能量代理 0.5·∫u² dx 应在 FD 截断容差内不变。光滑 Gaussian 初值下" +
                "细化效应远低于 O(dx²) 显式截断，本测试可视为 plateau 检测。",
            InputTransformation = "num_points → factor·num_points（factor > 1）",
            OutputRelation = "energy_proxy(flw) ≈ energy_proxy(src)（O(dx²) 截断容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_points 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "energy_proxy", PhysicalMeaning = "0.5·∫u² dx 能量代理（输出）", ValueRange = "energy_proxy > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "burgers-amplitude-peak-monotone",
            EquationKey = "burgers",
            PhysicalMeaning =
                "非线性单调性：无粘 Burgers 方程 u_t + (u²/2)_x = 0 是非线性的，" +
                "但严格正向地放大初始 Gaussian 振幅必使末时刻峰值严格变大。" +
                "Lax-Friedrichs 数值耗散使 peak_ratio < 2（激波被抹平），" +
                "因此断言为 GreaterThan 而非按比例相等。",
            InputTransformation = "amplitude → factor·amplitude（factor > 1）",
            OutputRelation = "peak_amplitude(flw) > peak_amplitude(src)",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "初始幅度缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "peak_amplitude", PhysicalMeaning = "末时刻峰值幅度（输出）", ValueRange = "peak_amplitude > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "burgers-mesh-conservation",
            EquationKey = "burgers",
            PhysicalMeaning =
                "质量守恒：Lax-Friedrichs 通量差分 + 周期边界严格守恒 ∫u dx（独立于激波结构）。" +
                "num_points 翻倍仅令初始 Gaussian 采样误差为 O(dx²)，逐步质量守恒精确。" +
                "末时刻 mass_integral 应在 FD 截断容差内不变。",
            InputTransformation = "num_points → factor·num_points（factor > 1）",
            OutputRelation = "mass_integral(flw) ≈ mass_integral(src)（O(dx²) 截断容差内）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "num_points 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "mass_integral", PhysicalMeaning = "∫u dx 守恒量（输出）", ValueRange = "mass_integral > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "subchannel-flow-temperature-monotone",
            EquationKey = "navier-stokes",
            PhysicalMeaning =
                "由能量守恒 ΔT = q''·P_h·L/(G·A_xs·c_p)，固定热流密度 q'' 下，质量流密度 G " +
                "越大，温升 ΔT 越小（流量越高，散热越好）。",
            InputTransformation = "G → factor·G（factor > 1）",
            OutputRelation = "ΔT(flw) < ΔT(src)（严格意义下 = ΔT(src) / factor）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "G 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "ΔT", PhysicalMeaning = "出口温升（输出）", ValueRange = "ΔT > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "subchannel-heat-flux-linearity",
            EquationKey = "navier-stokes",
            PhysicalMeaning =
                "能量守恒在 q'' 上线性：定流量下 ΔT 与 q'' 成正比，故 q'' 翻倍必使 ΔT 翻倍。",
            InputTransformation = "q'' → factor·q''（factor > 1）",
            OutputRelation = "ΔT(flw) > ΔT(src)（严格意义下 = factor·ΔT(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "q'' 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "ΔT", PhysicalMeaning = "出口温升（输出）", ValueRange = "ΔT > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "subchannel-friction-invariance",
            EquationKey = "navier-stokes",
            PhysicalMeaning =
                "解耦不变性 MP_inv：在这个 0D-轴向能量平衡里，friction factor f 只进入动量方程 " +
                "(Δp = f·L·G²/(2·ρ·D_h))；它不出现在能量方程 ΔT = q''·P_h·L/(G·A_xs·c_p) 里。" +
                "因此 f 翻倍必使 delta_T 位级不变（解析闭式不变量，无 FD 截断）。",
            InputTransformation = "f → factor·f（factor > 1）",
            OutputRelation = "delta_T(flw) ≈ delta_T(src)（解析意义下严格相等，1e-12 容差守 FP 误差）",
            ComparisonType = MrComparisonType.Absolute,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "f 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "delta_T", PhysicalMeaning = "出口温升（输出，f 不变）", ValueRange = "delta_T > 0" },
            },
        },
        new MrMetadata
        {
            MrId = "projectile-scale-v0",
            EquationKey = "projectile-motion",
            PhysicalMeaning =
                "由射程恒等式 R = v0²·sin(2θ)/g，放大初速度 v0 必单调抬高水平射程（严格意义下按 factor² 放大）。",
            InputTransformation = "v0 → factor·v0（factor > 1）",
            OutputRelation = "range(flw) > range(src)（严格意义下 = factor²·range(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "v0 缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "range", PhysicalMeaning = "水平射程（输出）", ValueRange = "range > 0" },
            },
        },
        // PR-A T1 non-JSON I/O adapter: synthetic test-SUT MR. NOT a real physics MR.
        new MrMetadata
        {
            MrId = "csv-roundtrip-identity",
            EquationKey = "_test_csv",
            PhysicalMeaning =
                "合成测试 MR：证明 metbench_io 的 csv-row wire format helper 经过未改动的 launcher / pipeline / catalog " +
                "端到端 round-trip。ScaleField 把 /params/factor 加倍 → runner echo 出加倍后的值 → echo_value 严格大于 source。",
            InputTransformation = "factor → factor·factor（factor > 1）",
            OutputRelation = "echo_value(flw) > echo_value(src)（严格意义下 = factor·echo_value(src)）",
            ComparisonType = MrComparisonType.Ordinal,
            Parameters = new List<MrParameter>
            {
                new() { Symbol = "factor", PhysicalMeaning = "输入 factor 的缩放倍率", ValueRange = "factor > 1" },
                new() { Symbol = "echo_value", PhysicalMeaning = "输出 echo_value（= factor）", ValueRange = "echo_value > 0" },
            },
        },
    };

    /// <summary>
    /// Upsert every seed equation and MR into <paramref name="repository"/>.
    /// Idempotent — upsert keys on the business slugs, so re-seeding updates
    /// in place rather than duplicating.
    /// </summary>
    public static async Task SeedAsync(
        ISystemMtMetadataRepository repository,
        CancellationToken cancellationToken = default)
    {
        if (repository is null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        foreach (var equation in Equations)
        {
            await repository.UpsertEquationAsync(equation, cancellationToken);
        }
        foreach (var mr in MetamorphicRelations)
        {
            await repository.UpsertMrAsync(mr, cancellationToken);
        }
    }
}
