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

    [Theory]
    [InlineData(null, "AnthropicStructuredExtractor")]
    [InlineData("Anthropic", "AnthropicStructuredExtractor")]
    [InlineData("OpenAI", "OpenAiStructuredExtractor")]
    [InlineData("openai", "OpenAiStructuredExtractor")]
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
