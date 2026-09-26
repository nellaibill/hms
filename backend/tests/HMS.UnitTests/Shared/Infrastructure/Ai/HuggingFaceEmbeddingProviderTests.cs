using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HMS.Shared.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HMS.UnitTests.Shared.Infrastructure.Ai;

public class HuggingFaceEmbeddingProviderTests
{
    /// <summary>Answers each request with one vector of <paramref name="dimensions"/> per input
    /// (value = the input's position), recording what was sent.</summary>
    private sealed class EmbeddingHandler(int dimensions, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<(Uri Uri, string? Auth, JsonElement Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement.Clone();
            Requests.Add((request.RequestUri!, request.Headers.Authorization?.ToString(), body));

            if (status != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(status) { Content = new StringContent("""{"error":"nope"}""") };
            }

            var inputs = body.GetProperty("inputs").GetArrayLength();
            var vectors = Enumerable.Range(0, inputs).Select(i => Enumerable.Repeat((float)i, dimensions).ToArray());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(vectors), Encoding.UTF8, "application/json"),
            };
        }
    }

    private static HuggingFaceEmbeddingProvider Provider(HttpMessageHandler handler, string? apiKey = "hf_test", params (string Key, string Value)[] extra)
    {
        var values = new Dictionary<string, string?> { ["Ai:HuggingFace:ApiKey"] = apiKey };
        foreach (var (key, value) in extra)
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new HuggingFaceEmbeddingProvider(new HttpClient(handler), configuration, NullLogger<HuggingFaceEmbeddingProvider>.Instance);
    }

    [Fact]
    public async Task WithoutApiKey_ReturnsNotConfiguredWithoutCallingOut()
    {
        var handler = new EmbeddingHandler(1024);

        var result = await Provider(handler, apiKey: null).EmbedAsync(["text"], EmbeddingInputKind.Document, CancellationToken.None);

        result.ErrorCode.Should().Be(AiErrorCodes.NotConfigured);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task PostsToTheModelsFeatureExtractionPipeline_WithTheToken()
    {
        var handler = new EmbeddingHandler(1024);

        var result = await Provider(handler).EmbedAsync(["a", "b"], EmbeddingInputKind.Document, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Model.Should().Be("BAAI/bge-m3");
        result.Value.Vectors.Should().HaveCount(2).And.OnlyContain(v => v.Length == 1024);
        var request = handler.Requests.Single();
        request.Uri.ToString().Should().Be("https://router.huggingface.co/hf-inference/models/BAAI/bge-m3/pipeline/feature-extraction");
        request.Auth.Should().Be("Bearer hf_test");
        request.Body.GetProperty("normalize").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SplitsIntoBatches_AndKeepsInputOrder()
    {
        var handler = new EmbeddingHandler(1024);

        var result = await Provider(handler, "hf_test", ("Ai:Embeddings:BatchSize", "2"))
            .EmbedAsync(["a", "b", "c", "d", "e"], EmbeddingInputKind.Document, CancellationToken.None);

        handler.Requests.Select(r => r.Body.GetProperty("inputs").GetArrayLength()).Should().Equal(2, 2, 1);
        // Each batch's vectors are valued by position within that batch.
        result.Value!.Vectors.Select(v => v[0]).Should().Equal(0, 1, 0, 1, 0);
    }

    [Fact]
    public async Task AppliesTheConfiguredQueryPrefixOnlyToQueries()
    {
        var handler = new EmbeddingHandler(1024);
        var provider = Provider(handler, "hf_test", ("Ai:Embeddings:QueryPrefix", "query: "), ("Ai:Embeddings:DocumentPrefix", "passage: "));

        await provider.EmbedAsync(["sugar"], EmbeddingInputKind.Query, CancellationToken.None);
        await provider.EmbedAsync(["sugar"], EmbeddingInputKind.Document, CancellationToken.None);

        handler.Requests.Select(r => r.Body.GetProperty("inputs")[0].GetString()).Should().Equal("query: sugar", "passage: sugar");
    }

    [Fact]
    public async Task RejectsVectorsOfTheWrongSize()
    {
        var result = await Provider(new EmbeddingHandler(384)).EmbedAsync(["a"], EmbeddingInputKind.Document, CancellationToken.None);

        result.ErrorCode.Should().Be(AiErrorCodes.RequestFailed);
        result.Error.Should().Contain("384");
    }

    [Fact]
    public async Task ReportsAnErrorStatusAsRequestFailed()
    {
        var result = await Provider(new EmbeddingHandler(1024, HttpStatusCode.Unauthorized)).EmbedAsync(["a"], EmbeddingInputKind.Document, CancellationToken.None);

        result.ErrorCode.Should().Be(AiErrorCodes.RequestFailed);
    }

    [Theory]
    [InlineData("HuggingFace", typeof(HuggingFaceEmbeddingProvider))]
    [InlineData(null, typeof(DisabledEmbeddingProvider))]
    public void Registration_FollowsAiEmbeddingsProvider(string? provider, Type expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:Embeddings:Provider"] = provider })
            .Build();
        var services = new ServiceCollection().AddLogging().AddSingleton<IConfiguration>(configuration);

        services.AddHmsAiEmbeddings(configuration);

        services.BuildServiceProvider().GetRequiredService<IAiEmbeddingProvider>().Should().BeOfType(expected);
    }
}
