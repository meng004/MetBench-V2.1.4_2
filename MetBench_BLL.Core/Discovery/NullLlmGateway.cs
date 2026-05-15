namespace MetBench_BLL.Discovery;

/// <summary>
/// 占位 LLM Gateway —— 永远返回固定 JSON / 空字符串。
/// </summary>
/// <remarks>
/// 用途：CI 环境 + cloud 默认 DI 绑定，让 <see cref="LlmNativeDiscoverer"/> /
/// <c>TheoreticalLlmValidator</c> 可以在没有真实 API key 时也能跑通流程。
/// 生产路径上 VM 端替换为真实 DeepSeek / Anthropic gateway。
/// </remarks>
public sealed class NullLlmGateway : ILlmGateway
{
    private readonly string _cannedReply;

    public NullLlmGateway(string cannedReply = "")
    {
        _cannedReply = cannedReply;
    }

    public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
        => Task.FromResult(_cannedReply);
}
