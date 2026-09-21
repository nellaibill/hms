using System.Net;
using System.Text;
using FluentAssertions;
using HMS.Shared.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure.Ai;

public class AiStructuredExtractorTests
{
    private static readonly AiStructuredExtractionRequest Request = new(
        "system", "user text", "my_tool", "desc", new { type = "object", properties = new { a = new { type = "string" } } });

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value)).Build();

    private static AnthropicStructuredExtractor Anthropic(HttpMessageHandler handler, string? apiKey = "key") =>
        new(new HttpClient(handler), Config(("Ai:Anthropic:ApiKey", apiKey ?? string.Empty)), NullLogger<AnthropicStructuredExtractor>.Instance);

    private static OpenAiStructuredExtractor OpenAi(HttpMessageHandler handler, string? apiKey = "key") =>
        new(new HttpClient(handler), Config(("Ai:OpenAI:ApiKey", apiKey ?? string.Empty)), NullLogger<OpenAiStructuredExtractor>.Instance);

    [Fact]
    public async Task Anthropic_WithoutApiKey_ReturnsNotConfiguredWithoutCallingOut()
    {
        var result = await Anthropic(new StubHandler(HttpStatusCode.OK, "{}"), apiKey: null).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.NotConfigured);
    }

    [Fact]
    public async Task OpenAi_WithoutApiKey_ReturnsNotConfiguredWithoutCallingOut()
    {
        var result = await OpenAi(new StubHandler(HttpStatusCode.OK, "{}"), apiKey: null).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.NotConfigured);
    }

    [Fact]
    public async Task Anthropic_ReturnsTheToolUseInput()
    {
        const string body = """{"content":[{"type":"text","text":"ok"},{"type":"tool_use","name":"my_tool","input":{"a":"hello"}}]}""";

        var result = await Anthropic(new StubHandler(HttpStatusCode.OK, body)).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("a").GetString().Should().Be("hello");
    }

    [Fact]
    public async Task OpenAi_ParsesTheFunctionCallArgumentsString()
    {
        const string body = """{"choices":[{"message":{"tool_calls":[{"function":{"name":"my_tool","arguments":"{\"a\":\"hello\"}"}}]}}]}""";

        var result = await OpenAi(new StubHandler(HttpStatusCode.OK, body)).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("a").GetString().Should().Be("hello");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, """{"content":[]}""")]
    [InlineData(HttpStatusCode.OK, "not json")]
    public async Task Anthropic_OnFailureOrUnexpectedBody_ReturnsRequestFailed(HttpStatusCode status, string body)
    {
        var result = await Anthropic(new StubHandler(status, body)).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.RequestFailed);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{}")]
    [InlineData(HttpStatusCode.OK, """{"choices":[{"message":{}}]}""")]
    [InlineData(HttpStatusCode.OK, """{"choices":[{"message":{"tool_calls":[{"function":{"name":"my_tool","arguments":"not json"}}]}}]}""")]
    public async Task OpenAi_OnFailureOrUnexpectedBody_ReturnsRequestFailed(HttpStatusCode status, string body)
    {
        var result = await OpenAi(new StubHandler(status, body)).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.RequestFailed);
    }

    private sealed class CapturingHandler(string body) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Uri = request.RequestUri;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private const string OpenAiToolCallBody = """{"choices":[{"message":{"tool_calls":[{"function":{"name":"my_tool","arguments":"{\"a\":\"hello\"}"}}]}}]}""";

    private static AzureOpenAiStructuredExtractor Azure(
        HttpMessageHandler handler,
        string? endpoint = "https://res.openai.azure.com",
        string? apiKey = "azure-key",
        string? deployment = "note-drafter",
        string? apiVersion = null) =>
        new(
            new HttpClient(handler),
            Config(
                ("Ai:AzureOpenAI:Endpoint", endpoint ?? string.Empty),
                ("Ai:AzureOpenAI:ApiKey", apiKey ?? string.Empty),
                ("Ai:AzureOpenAI:Deployment", deployment ?? string.Empty),
                ("Ai:AzureOpenAI:ApiVersion", apiVersion ?? string.Empty)),
            NullLogger<AzureOpenAiStructuredExtractor>.Instance);

    [Theory]
    [InlineData(null, "azure-key", "note-drafter")]
    [InlineData("https://res.openai.azure.com", null, "note-drafter")]
    [InlineData("https://res.openai.azure.com", "azure-key", null)]
    [InlineData("http://res.openai.azure.com", "azure-key", "note-drafter")]
    [InlineData("not a url", "azure-key", "note-drafter")]
    public async Task Azure_WithIncompleteOrInsecureConfig_ReturnsNotConfiguredWithoutCallingOut(string? endpoint, string? apiKey, string? deployment)
    {
        var handler = new CapturingHandler(OpenAiToolCallBody);

        var result = await Azure(handler, endpoint, apiKey, deployment).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.NotConfigured);
        handler.Request.Should().BeNull("a misconfigured provider must never send the key anywhere");
    }

    [Fact]
    public async Task Azure_SendsToTheDeploymentUrlWithApiKeyHeaderAndNoModelField()
    {
        var handler = new CapturingHandler(OpenAiToolCallBody);

        var result = await Azure(handler, endpoint: "https://res.openai.azure.com/some/path/").ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("a").GetString().Should().Be("hello");
        handler.Uri!.ToString().Should().Be("https://res.openai.azure.com/openai/deployments/note-drafter/chat/completions?api-version=2024-10-21");
        handler.Request!.Headers.GetValues("api-key").Should().ContainSingle().Which.Should().Be("azure-key");
        handler.Request.Headers.Authorization.Should().BeNull();
        handler.Body.Should().Contain("\"tools\"").And.Contain("\"tool_choice\"").And.NotContain("\"model\"");
    }

    [Fact]
    public async Task Azure_UsesTheConfiguredApiVersion()
    {
        var handler = new CapturingHandler(OpenAiToolCallBody);

        await Azure(handler, apiVersion: "2025-01-01").ExtractAsync(Request, CancellationToken.None);

        handler.Uri!.Query.Should().Be("?api-version=2025-01-01");
    }

    [Fact]
    public async Task Azure_OnHttpFailure_ReturnsRequestFailed()
    {
        var result = await Azure(new StubHandler(HttpStatusCode.Unauthorized, "{}")).ExtractAsync(Request, CancellationToken.None);

        result.ErrorCode.Should().Be(AiErrorCodes.RequestFailed);
    }

    [Fact]
    public async Task OpenAi_SendsBearerAuthAndTheModelField()
    {
        var handler = new CapturingHandler(OpenAiToolCallBody);

        await OpenAi(handler).ExtractAsync(Request, CancellationToken.None);

        handler.Uri!.ToString().Should().Be("https://api.openai.com/v1/chat/completions");
        handler.Request!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Body.Should().Contain("\"model\":\"gpt-4o-mini\"");
    }

    private static HuggingFaceStructuredExtractor HuggingFace(HttpMessageHandler handler, string? apiKey = "hf_key", string? model = null) =>
        new(new HttpClient(handler), Config(("Ai:HuggingFace:ApiKey", apiKey ?? string.Empty), ("Ai:HuggingFace:Model", model ?? string.Empty)), NullLogger<HuggingFaceStructuredExtractor>.Instance);

    [Fact]
    public async Task HuggingFace_WithoutApiKey_ReturnsNotConfiguredWithoutCallingOut()
    {
        var result = await HuggingFace(new StubHandler(HttpStatusCode.OK, "{}"), apiKey: null).ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.NotConfigured);
    }

    [Fact]
    public async Task HuggingFace_SendsToTheRouterWithBearerTokenAndConfiguredModel()
    {
        var handler = new CapturingHandler(OpenAiToolCallBody);

        var result = await HuggingFace(handler, model: "org/some-model").ExtractAsync(Request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("a").GetString().Should().Be("hello");
        handler.Uri!.ToString().Should().Be("https://router.huggingface.co/v1/chat/completions");
        handler.Request!.Headers.Authorization!.Parameter.Should().Be("hf_key");
        handler.Body.Should().Contain("\"model\":\"org/some-model\"");
    }

    [Theory]
    [InlineData(null, "AnthropicStructuredExtractor")]
    [InlineData("Anthropic", "AnthropicStructuredExtractor")]
    [InlineData("OpenAI", "OpenAiStructuredExtractor")]
    [InlineData("openai", "OpenAiStructuredExtractor")]
    [InlineData("AzureOpenAI", "AzureOpenAiStructuredExtractor")]
    [InlineData("azureopenai", "AzureOpenAiStructuredExtractor")]
    [InlineData("HuggingFace", "HuggingFaceStructuredExtractor")]
    [InlineData("huggingface", "HuggingFaceStructuredExtractor")]
    [InlineData("SomethingElse", "AnthropicStructuredExtractor")]
    public void AddHmsAiExtractor_RegistersTheProviderChosenByConfig(string? provider, string expectedType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHmsAiExtractor(provider is null ? Config() : Config(("Ai:Provider", provider)));
        services.AddSingleton<IConfiguration>(Config());

        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<IAiStructuredExtractor>().GetType().Name.Should().Be(expectedType);
    }

    [Fact]
    public void AddHmsAiExtractor_IsIdempotent()
    {
        var services = new ServiceCollection();

        services.AddHmsAiExtractor(Config());
        services.AddHmsAiExtractor(Config(("Ai:Provider", "OpenAI")));

        services.Count(d => d.ServiceType == typeof(IAiStructuredExtractor)).Should().Be(1);
    }
}
