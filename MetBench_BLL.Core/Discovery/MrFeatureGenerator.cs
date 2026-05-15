using System.Text;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery;

/// <summary>
/// 从持久化的 MetaPattern + 用户填的少量参数 → 生成结构正确的 .feature。
/// </summary>
/// <remarks>
/// 直接闭环 cold-start C4 ("用户抄改 14 个 .feature 哪个版本" 问题)：
/// MetaPattern 在 DB 一份，scaffold 一次填 → 输出标准化骨架。
///
/// 输入靠 <see cref="MrFeatureSpec"/> DTO；输出是 .feature 文本（不直接写盘
/// 以便测试）。CLI 层负责 path resolution 和 file IO。
/// </remarks>
public sealed class MrFeatureGenerator
{
    private readonly IMetaPatternRepository _metaPatternRepo;

    public MrFeatureGenerator(IMetaPatternRepository metaPatternRepo)
    {
        _metaPatternRepo = metaPatternRepo;
    }

    /// <summary>
    /// 校验 spec → 生成 .feature 文本。失败抛 <see cref="InvalidOperationException"/>。
    /// </summary>
    public string Generate(MrFeatureSpec spec)
    {
        // ===== 校验 =====
        if (string.IsNullOrWhiteSpace(spec.Code))
            throw new InvalidOperationException("Code is required (e.g. 'MR-X' or 'MR16-inv-quarter-rot-180').");
        if (spec.Suts.Count == 0)
            throw new InvalidOperationException("At least one SUT must be specified.");
        if (string.IsNullOrWhiteSpace(spec.ParameterAbstract))
            throw new InvalidOperationException(
                "ParameterAbstract is required (e.g. 'physics.fuel.nu_sigma_f'). " +
                "Hint: see the chosen MetaPattern's ExampleParamHints.");

        var mp = _metaPatternRepo.Get(spec.MetaPatternCode)
            ?? throw new InvalidOperationException(
                $"MetaPattern '{spec.MetaPatternCode}' not found in DB. " +
                $"Active: {string.Join(", ", _metaPatternRepo.GetActive().Select(p => p.Code))}.");

        if (mp.Status == "out-of-scope")
            throw new InvalidOperationException(
                $"MetaPattern '{spec.MetaPatternCode}' is out-of-scope ({mp.OutOfScopeReason}). " +
                "Pick an active one.");

        // ===== 用 MetaPattern 默认值补全空字段（让用户少填）=====
        var assertion = spec.Assertion ?? mp.DefaultAssertionTypeCode;
        var tolerance = spec.ToleranceRel ?? mp.DefaultToleranceRel;
        var noiseAware = spec.NoiseAware ?? mp.NoiseAware;
        var valueName = spec.ValueName ?? "k_eff";
        var factors = spec.Factors.Count > 0 ? spec.Factors : new[] { "0.5", "1.0", "1.5" };

        // ===== 生成 .feature =====
        var sb = new StringBuilder();
        sb.AppendLine($"@metapattern:{mp.Code} @assertion:{assertion} @value:{valueName}");
        sb.AppendLine($"@noise_aware:{noiseAware.ToString().ToLowerInvariant()} @tolerance_rel:{tolerance.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Feature: {spec.Code} — {spec.Title ?? spec.Code}");
        sb.AppendLine();
        sb.AppendLine("  Background:");
        sb.AppendLine($"    {mp.HypothesisTemplate}");
        if (!string.IsNullOrEmpty(spec.RationaleNote))
        {
            sb.AppendLine($"    Note: {spec.RationaleNote}");
        }
        sb.AppendLine();
        sb.AppendLine($"  Scenario Outline: Apply {spec.Code} to <sut> with factor <factor>");
        sb.AppendLine($"    Given the MR Schema \"{spec.Code}\" is bound to SUT \"<sut>\"");
        sb.AppendLine($"    And the binding uses sample case \"<sample>\"");
        sb.AppendLine($"    And the parameter mapping for \"{spec.ParameterAbstract}\" is configured");
        sb.AppendLine($"    When the MT pipeline runs with parameter \"factor\"=\"<factor>\"");
        sb.AppendLine($"    Then the \"{assertion}\" assertion holds on \"{valueName}\"");
        sb.AppendLine();
        sb.AppendLine("    Examples:");
        sb.AppendLine("      | sut    | sample                   | factor | is_active |");
        foreach (var sut in spec.Suts)
        {
            foreach (var factor in factors)
            {
                sb.AppendLine($"      | {sut,-6} | SUT/{sut}/sample/pincell.json | {factor,-6} | True      |");
            }
        }
        return sb.ToString();
    }

    /// <summary>计算 .feature 应放的相对路径（按 MetaPattern Code 分子目录）。</summary>
    public string TargetRelativePath(MrFeatureSpec spec)
        => $"metbench/catalog/features/{spec.MetaPatternCode}/{spec.Code}.feature";
}

/// <summary>
/// scaffold_mr CLI / API 的输入 DTO。
/// </summary>
public sealed record MrFeatureSpec(
    string Code,
    string MetaPatternCode,
    IReadOnlyList<string> Suts,
    string ParameterAbstract,
    string? Title = null,
    string? Assertion = null,
    double? ToleranceRel = null,
    bool? NoiseAware = null,
    string? ValueName = null,
    IReadOnlyList<string>? Factors = null,
    string? RationaleNote = null)
{
    /// <summary>当 ctor 参数 <c>Factors=null</c> 时，对外为空数组（generator 会用默认值替代）。</summary>
    public IReadOnlyList<string> Factors { get; init; } = Factors ?? Array.Empty<string>();
}
