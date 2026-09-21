using System.Net.Http.Headers;
using System.Text.Json;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>OpenAI Chat Completions with a forced function call; config under <c>Ai:OpenAI:*</c>.</summary>
internal sealed class OpenAiStructuredExtractor : IAiStructuredExtractor
{
    private const string DefaultModel = "gpt-4o-mini";
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<OpenAiStructuredExtractor> _logger;

    public OpenAiStructuredExtractor(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiStructuredExtractor> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Ai:OpenAI:ApiKey"];
        _model = configuration["Ai:OpenAI:Model"] is { Length: > 0 } configured ? configured : DefaultModel;
        _logger = logger;
    }

    public async Task<Result<JsonElement>> ExtractAsync(AiStructuredExtractionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Ai:OpenAI:ApiKey is not configured — AI extraction was not attempted.");
            return Result<JsonElement>.Failure(AiErrorCodes.NotConfigured, "AI generation isn't configured for this environment.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _apiKey) },
            Content = OpenAiChatProtocol.BuildBody(request, _model),
        };

        return await OpenAiChatProtocol.SendAsync(_httpClient, message, request.ToolName, "OpenAI", _logger, cancellationToken);
    }
}
