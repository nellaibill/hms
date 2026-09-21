using System.Text.Json;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>
/// Azure OpenAI Chat Completions; config under <c>Ai:AzureOpenAI:*</c> — <c>Endpoint</c> (the
/// resource URL, https only), <c>ApiKey</c>, <c>Deployment</c> (the deployment's name, which is
/// what selects the model on Azure) and <c>ApiVersion</c>. Same wire format as OpenAI; only the
/// URL, the <c>api-key</c> header and the absence of a "model" body field differ.
/// </summary>
internal sealed class AzureOpenAiStructuredExtractor : IAiStructuredExtractor
{
    internal const string DefaultApiVersion = "2024-10-21";

    private readonly HttpClient _httpClient;
    private readonly string? _endpoint;
    private readonly string? _apiKey;
    private readonly string? _deployment;
    private readonly string _apiVersion;
    private readonly ILogger<AzureOpenAiStructuredExtractor> _logger;

    public AzureOpenAiStructuredExtractor(HttpClient httpClient, IConfiguration configuration, ILogger<AzureOpenAiStructuredExtractor> logger)
    {
        _httpClient = httpClient;
        _endpoint = configuration["Ai:AzureOpenAI:Endpoint"];
        _apiKey = configuration["Ai:AzureOpenAI:ApiKey"];
        _deployment = configuration["Ai:AzureOpenAI:Deployment"];
        _apiVersion = configuration["Ai:AzureOpenAI:ApiVersion"] is { Length: > 0 } configured ? configured : DefaultApiVersion;
        _logger = logger;
    }

    public async Task<Result<JsonElement>> ExtractAsync(AiStructuredExtractionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_deployment) || string.IsNullOrWhiteSpace(_endpoint))
        {
            _logger.LogWarning("Ai:AzureOpenAI:Endpoint/ApiKey/Deployment is not fully configured — AI extraction was not attempted.");
            return NotConfigured();
        }

        // https only: the key is sent in a header, so never allow a plaintext or malformed endpoint.
        if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogWarning("Ai:AzureOpenAI:Endpoint must be an absolute https URL — AI extraction was not attempted.");
            return NotConfigured();
        }

        var url = $"{endpoint.GetLeftPart(UriPartial.Authority)}/openai/deployments/{Uri.EscapeDataString(_deployment)}/chat/completions?api-version={Uri.EscapeDataString(_apiVersion)}";

        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Headers = { { "api-key", _apiKey } },
            Content = OpenAiChatProtocol.BuildBody(request, model: null),
        };

        return await OpenAiChatProtocol.SendAsync(_httpClient, message, request.ToolName, "Azure OpenAI", _logger, cancellationToken);
    }

    private static Result<JsonElement> NotConfigured() =>
        Result<JsonElement>.Failure(AiErrorCodes.NotConfigured, "AI generation isn't configured for this environment.");
}
