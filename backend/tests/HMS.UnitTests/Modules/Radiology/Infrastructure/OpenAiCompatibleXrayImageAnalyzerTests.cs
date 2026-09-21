using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HMS.Modules.Radiology.Application;
using HMS.Modules.Radiology.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HMS.UnitTests.Modules.Radiology.Infrastructure;

public class OpenAiCompatibleXrayImageAnalyzerTests
{
    private static OpenAiCompatibleXrayImageAnalyzer Create(HttpMessageHandler handler, params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

        return new OpenAiCompatibleXrayImageAnalyzer(new HttpClient(handler), configuration, NullLogger<OpenAiCompatibleXrayImageAnalyzer>.Instance);
    }

    [Fact]
    public async Task Analyze_WithoutBaseUrl_FailsAsNotConfigured_AndMakesNoRequest()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var sut = Create(handler);

        var result = await sut.AnalyzeAsync([1], "image/png", CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.NotConfigured);
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task Analyze_PostsImageAsDataUrl_WithBearerToken_AndReturnsMessageText()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"choices":[{"message":{"role":"assistant","content":"  ANATOMICAL REGION:\nKnee  "}}]}""");
        var sut = Create(handler, ("Ai:Radiology:BaseUrl", "https://endpoint.example/v1/"), ("Ai:Radiology:ApiKey", "hf_test"));

        var result = await sut.AnalyzeAsync([1, 2, 3], "image/png", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().Be("ANATOMICAL REGION:\nKnee");
        result.Value.Model.Should().Be(OpenAiCompatibleXrayImageAnalyzer.DefaultModel);

        handler.Request!.RequestUri!.ToString().Should().Be("https://endpoint.example/v1/chat/completions");
        handler.Request.Headers.Authorization!.ToString().Should().Be("Bearer hf_test");

        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.GetProperty("model").GetString().Should().Be(OpenAiCompatibleXrayImageAnalyzer.DefaultModel);
        var user = body.RootElement.GetProperty("messages")[1].GetProperty("content");
        user[1].GetProperty("image_url").GetProperty("url").GetString().Should().Be("data:image/png;base64," + Convert.ToBase64String([1, 2, 3]));
    }

    [Fact]
    public async Task Analyze_WithoutApiKey_SendsNoAuthorizationHeader()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"choices":[{"message":{"content":"ok"}}]}""");
        var sut = Create(handler, ("Ai:Radiology:BaseUrl", "http://localhost:11434/v1"));

        await sut.AnalyzeAsync([1], "image/jpeg", CancellationToken.None);

        handler.Request!.Headers.Authorization.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, """{"choices":[]}""")]
    [InlineData(HttpStatusCode.OK, "not json")]
    public async Task Analyze_OnHttpErrorOrUnusableReply_FailsAsRequestFailed(HttpStatusCode status, string payload)
    {
        var sut = Create(new StubHandler(status, payload), ("Ai:Radiology:BaseUrl", "https://endpoint.example/v1"));

        var result = await sut.AnalyzeAsync([1], "image/png", CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.RequestFailed);
    }

    private sealed class StubHandler(HttpStatusCode status, string payload) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        }
    }
}
