using System.Net.Http.Headers;
using System.Text.Json;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>
/// Hugging Face Inference Providers via its OpenAI-compatible router; config under
/// <c>Ai:HuggingFace:*</c> — <c>ApiKey</c> (a HF access token) and <c>Model</c> (a repo id such as
/// "meta-llama/Llama-3.3-70B-Instruct"; it must be served by a provider that supports function
/// calling). Same wire format as OpenAI, so it reuses <see cref="OpenAiChatProtocol"/>.
/// </summary>
internal sealed class HuggingFaceStructuredExtractor : IAiStructuredExtractor
{
    internal const string DefaultModel = "meta-llama/Llama-3.3-70B-Instruct";
    private const string ApiUrl = "https://router.huggingface.co/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<HuggingFaceStructuredExtractor> _logger;

    public HuggingFaceStructuredExtractor(HttpClient httpClient, IConfiguration configuration, ILogger<HuggingFaceStructuredExtractor> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Ai:HuggingFace:ApiKey"];
        _model = configuration["Ai:HuggingFace:Model"] is { Length: > 0 } configured ? configured : DefaultModel;
        _logger = logger;
    }

    public async Task<Result<JsonElement>> ExtractAsync(AiStructuredExtractionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Ai:HuggingFace:ApiKey is not configured — AI extraction was not attempted.");
            return Result<JsonElement>.Failure(AiErrorCodes.NotConfigured, "AI generation isn't configured for this environment.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _apiKey) },
            Content = OpenAiChatProtocol.BuildBody(request, _model),
        };

        return await OpenAiChatProtocol.SendAsync(_httpClient, message, request.ToolName, "Hugging Face", _logger, cancellationToken);
    }
}
