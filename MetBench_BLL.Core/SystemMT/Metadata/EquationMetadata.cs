namespace MetBench_BLL.SystemMT.Metadata;

/// <summary>
/// Structured, persistable description of the mathematical-physical equation a
/// SUT solves — the "program metadata" called for in the MR/program metadata
/// plan. Replaces source-only docstrings with a queryable record: equation
/// name, canonical form, symbol system, and per-parameter descriptions.
/// </summary>
/// <remarks>
/// Keyed by the stable business slug <see cref="EquationKey"/> (e.g.
/// <c>bateman</c>, <c>heat-equation-1d</c>). Multiple SUTs may solve the same
/// equation (OpenMOC and OpenMC both solve neutron transport); the SUT ↔
/// equation linkage is owned by the MR catalog and <see cref="MrMetadata"/>,
/// not duplicated here.
/// </remarks>
public sealed class EquationMetadata
{
    /// <summary>
    /// LiteDB document identity. Left <see cref="Guid.Empty"/> by callers;
    /// the repository assigns a fresh Guid on first insert.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Stable business key — unique across all equations.</summary>
    public string EquationKey { get; set; } = string.Empty;

    /// <summary>Human-readable equation name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The equation in canonical (plain-text or LaTeX-ish) form.</summary>
    public string CanonicalForm { get; set; } = string.Empty;

    /// <summary>Prose description of the symbol system used by the equation.</summary>
    public string SymbolSystem { get; set; } = string.Empty;

    /// <summary>Broad equation kind (for example ODE, PDE, algebraic, or synthetic).</summary>
    public string EquationClass { get; set; } = string.Empty;

    /// <summary>More specific mathematical family such as elliptic, parabolic, or hyperbolic.</summary>
    public string EquationFamily { get; set; } = string.Empty;

    /// <summary>Primary unknowns / state variables users should inspect in reports and UI.</summary>
    public List<string> PrimaryVariables { get; set; } = new();

    /// <summary>Auditable explanation of what physical or mathematical state the equation represents.</summary>
    public string PhysicalMeaning { get; set; } = string.Empty;

    /// <summary>Why this equation is useful as a System-MT benchmark target.</summary>
    public string BenchmarkRationale { get; set; } = string.Empty;

    /// <summary>Expected laws or invariants that motivate associated metamorphic checks.</summary>
    public List<string> ExpectedLaws { get; set; } = new();

    /// <summary>Per-parameter descriptions.</summary>
    public List<EquationParameter> Parameters { get; set; } = new();

    /// <summary>
    /// 本方程提供的数学函数目录(L1 Recipe + L2 C# 各自的描述符)。
    /// 设计见 docs/design/mr-architecture.md §4.2 / §5.1。默认空集 —— 既有数据
    /// 反序列化时此字段会回退到空列表,不影响兼容。
    /// </summary>
    public List<EquationFunctionDescriptor> Functions { get; set; } = new();
}

/// <summary>One symbol of an equation's parameter set.</summary>
public sealed class EquationParameter
{
    public string Symbol { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;
}
