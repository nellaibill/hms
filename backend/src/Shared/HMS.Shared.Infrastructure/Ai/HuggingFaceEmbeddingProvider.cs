using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>
/// Embeddings from Hugging Face's hosted Inference API (the hf-inference provider's
/// feature-extraction pipeline). Uses the same <c>Ai:HuggingFace:ApiKey</c> token as the
/// Hugging Face chat extractor (it needs the "Make calls to Inference Providers" permission).
/// Model and batching come from <c>Ai:Embeddings:*</c>.
///
/// Chunk text leaves the server here — fine for sample data and testing, but real patient
/// documents need a local model before this is enabled in a hospital (see
/// docs/modules/Documents/DocumentManagement.md's RAG section).
/// </summary>
internal sealed class HuggingFaceEmbeddingProvider : IAiEmbeddingProvider
{
    internal const string DefaultModel = "BAAI/bge-m3";
    private const string ApiUrlFormat = "https://router.huggingface.co/hf-inference/models/{0}/pipeline/feature-extraction";

    /// <summary>A cold model answers 503 while it loads; retried a few times before giving up.</summary>
    private static readonly TimeSpan[] LoadingRetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)];

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly int _batchSize;
    private readonly string _documentPrefix;
    private readonly string _queryPrefix;
    private readonly ILogger<HuggingFaceEmbeddingProvider> _logger;

    public HuggingFaceEmbeddingProvider(HttpClient httpClient, IConfiguration configuration, ILogger<HuggingFaceEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(configuration.GetValue("Ai:Embeddings:TimeoutSeconds", 120));
        _apiKey = configuration["Ai:HuggingFace:ApiKey"];
        Model = configuration["Ai:Embeddings:Model"] is { Length: > 0 } model ? model : DefaultModel;
        Dimensions = configuration.GetValue("Ai:Embeddings:Dimensions", 1024);
        _batchSize = Math.Max(1, configuration.GetValue("Ai:Embeddings:BatchSize", 16));
        _documentPrefix = configuration["Ai:Embeddings:DocumentPrefix"] ?? string.Empty;
        _queryPrefix = configuration["Ai:Embeddings:QueryPrefix"] ?? string.Empty;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public string Model { get; }

    public int Dimensions { get; }

    public async Task<Result<AiEmbeddingResult>> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingInputKind kind, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return Result<AiEmbeddingResult>.Failure(AiErrorCodes.NotConfigured, "Embeddings aren't configured for this environment (Ai:HuggingFace:ApiKey).");
        }

        var prefix = kind == EmbeddingInputKind.Query ? _queryPrefix : _documentPrefix;
        var vectors = new List<float[]>(inputs.Count);

        foreach (var batch in inputs.Chunk(_batchSize))
        {
            var result = await EmbedBatchAsync(batch.Select(text => prefix + text).ToArray(), cancellationToken);
            if (!result.IsSuccess)
            {
                return Result<AiEmbeddingResult>.Failure(result.ErrorCode!, result.Error!);
            }

            vectors.AddRange(result.Value!);
        }

        return Result<AiEmbeddingResult>.Success(new AiEmbeddingResult(vectors, Model));
    }

    private async Task<Result<IReadOnlyList<float[]>>> EmbedBatchAsync(string[] batch, CancellationToken cancellationToken)
    {
        var url = string.Format(ApiUrlFormat, Model);

        for (var attempt = 0; ; attempt++)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _apiKey) },
                // normalize: unit-length vectors, so cosine distance is well-behaved;
                // truncate: over-long input is cut to the model's limit rather than rejected.
                Content = JsonContent.Create(new { inputs = batch, normalize = true, truncate = true }),
            };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(message, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Hugging Face embedding request failed.");
                return Result<IReadOnlyList<float[]>>.Failure(AiErrorCodes.RequestFailed, "The embedding service could not be reached.");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < LoadingRetryDelays.Length)
                {
                    _logger.LogInformation("Hugging Face model {Model} is loading; retrying in {Delay}.", Model, LoadingRetryDelays[attempt]);
                    await Task.Delay(LoadingRetryDelays[attempt], cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Hugging Face embedding returned {Status}: {Body}", (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
                    return Result<IReadOnlyList<float[]>>.Failure(AiErrorCodes.RequestFailed, $"The embedding service returned {(int)response.StatusCode}.");
                }

                float[][]? vectors;
                try
                {
                    vectors = await response.Content.ReadFromJsonAsync<float[][]>(cancellationToken);
                }
                catch (JsonException ex)
                {
                    // e.g. a non-sentence-transformers model returning per-token vectors.
                    _logger.LogWarning(ex, "Hugging Face model {Model} returned an unexpected embedding shape.", Model);
                    return Result<IReadOnlyList<float[]>>.Failure(AiErrorCodes.RequestFailed, $"Model {Model} did not return one vector per input.");
                }

                if (vectors is null || vectors.Length != batch.Length)
                {
                    return Result<IReadOnlyList<float[]>>.Failure(AiErrorCodes.RequestFailed, "The embedding service returned an unexpected number of vectors.");
                }

                if (vectors.FirstOrDefault(v => v.Length != Dimensions) is { } wrong)
                {
                    return Result<IReadOnlyList<float[]>>.Failure(AiErrorCodes.RequestFailed, $"Model {Model} returned {wrong.Length}-dimension vectors; {Dimensions} are configured.");
                }

                return Result<IReadOnlyList<float[]>>.Success(vectors);
            }
        }
    }
}
