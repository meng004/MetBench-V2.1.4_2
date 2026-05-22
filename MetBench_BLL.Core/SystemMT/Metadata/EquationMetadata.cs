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

    /// <summary>Per-parameter descriptions.</summary>
    public List<EquationParameter> Parameters { get; set; } = new();
}

/// <summary>One symbol of an equation's parameter set.</summary>
public sealed class EquationParameter
{
    public string Symbol { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;
}
