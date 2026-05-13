namespace MetBench_BLL.Mutation;

/// <summary>
/// 一次 MutationCampaign 的 scope spec —— 跑哪些 (mutant × mrBinding × sample case) 组合。
/// </summary>
/// <remarks>
/// 取 Cartesian product = MutantIds × MRBindingIds × SampleCasePaths。
/// 如果 <see cref="SampleCasePaths"/> 为空，会从 <c>MRBinding.DefaultSampleCasePath</c> 取。
/// </remarks>
public sealed record MutationCampaignSpec(
    string Name,
    IReadOnlyList<int> MutantIds,
    IReadOnlyList<int> MRBindingIds,
    IReadOnlyList<string> SampleCasePaths,
    string? CreatedBy = null,
    string CatalogVersionSha = "");
