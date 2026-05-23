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
