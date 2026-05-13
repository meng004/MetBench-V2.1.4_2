namespace MetBench_BLL.Discovery;

/// <summary>
/// LLM 调用边界 —— 用于 <see cref="LlmNativeDiscoverer"/> 和 <c>TheoreticalLlmValidator</c>。
/// </summary>
/// <remarks>
/// 抽象成接口的目的：
/// <list type="bullet">
///   <item>CI 跑测试时注入 <see cref="NullLlmGateway"/>，避免真实 API 依赖。</item>
///   <item>VM 端可注入真实 provider（DeepSeek / Anthropic），prompt 模板由 provider 实现拥有。</item>
///   <item>未来要换 LLM 厂商时改 Gateway 即可，业务层不动。</item>
/// </list>
/// </remarks>
public interface ILlmGateway
{
    /// <summary>
    /// 文本补全：给 prompt → 拿模型回复（plain text）。
    /// </summary>
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
}
