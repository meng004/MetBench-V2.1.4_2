using System.Net;
using System.Net.Http;
using System.Text;
using MetBench_BLL.Discovery;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class OpenAiCompatibleLlmGatewayTests
{
    [Fact]
    public async Task Extracts_choices_message_content_OpenAI_format()
    {
        var responseJson = """
            {
              "id":"chatcmpl-1",
              "choices":[
                {"index":0,"message":{"role":"assistant","content":"hello world"},"finish_reason":"stop"}
              ]
            }
            """;
        var gateway = MakeGateway(responseJson, HttpStatusCode.OK);

        var text = await gateway.CompleteAsync("hi");

        Assert.Equal("hello world", text);
    }

    [Fact]
    public async Task Falls_back_to_content_text_Anthropic_format()
    {
        var responseJson = """
            {"content":[{"type":"text","text":"anthropic style"}]}
            """;
        var gateway = MakeGateway(responseJson, HttpStatusCode.OK);

        var text = await gateway.CompleteAsync("hi");

        Assert.Equal("anthropic style", text);
    }

    [Fact]
    public async Task Non_2xx_response_throws_HttpRequestException()
    {
        var gateway = MakeGateway("""{"error":"insufficient_quota"}""", HttpStatusCode.PaymentRequired);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => gateway.CompleteAsync("hi"));
        Assert.Contains("402", ex.Message);
        Assert.Contains("insufficient_quota", ex.Message);
    }

    [Fact]
    public async Task Malformed_response_throws()
    {
        var gateway = MakeGateway("not json at all", HttpStatusCode.OK);

        await Assert.ThrowsAnyAsync<Exception>(() => gateway.CompleteAsync("hi"));
    }

    [Fact]
    public async Task Response_without_known_text_field_throws()
    {
        var gateway = MakeGateway("""{"choices":[{"index":0,"finish_reason":"stop"}]}""", HttpStatusCode.OK);

        await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.CompleteAsync("hi"));
    }

    [Fact]
    public async Task Request_uses_correct_endpoint_and_includes_prompt()
    {
        var handler = new CapturingHandler(
            statusCode: HttpStatusCode.OK,
            responseBody: """{"choices":[{"message":{"content":"ok"}}]}""");
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var gateway = new OpenAiCompatibleLlmGateway(http, model: "gpt-fake");

        await gateway.CompleteAsync("ping-prompt");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("v1/chat/completions", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("ping-prompt", handler.LastRequestBody);
        Assert.Contains("\"model\":\"gpt-fake\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task Cancellation_propagates_during_send()
    {
        var handler = new BlockingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var gateway = new OpenAiCompatibleLlmGateway(http, model: "fake");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gateway.CompleteAsync("hi", cts.Token));
    }

    // ===== helpers =====

    private static OpenAiCompatibleLlmGateway MakeGateway(string responseBody, HttpStatusCode status)
    {
        var handler = new StaticHandler(status, responseBody);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        return new OpenAiCompatibleLlmGateway(http, model: "fake-model");
    }

    private sealed class StaticHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public StaticHandler(HttpStatusCode status, string body) { _status = status; _body = body; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;
        public CapturingHandler(HttpStatusCode statusCode, string responseBody)
        {
            _status = statusCode; _body = responseBody;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class BlockingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
