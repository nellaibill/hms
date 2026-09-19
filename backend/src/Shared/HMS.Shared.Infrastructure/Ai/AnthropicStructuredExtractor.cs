using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>Anthropic Messages API with a forced tool-use call; config under <c>Ai:Anthropic:*</c>.</summary>
internal sealed class AnthropicStructuredExtractor : IAiStructuredExtractor
{
    private const string DefaultModel = "claude-sonnet-5";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<AnthropicStructuredExtractor> _logger;

    public AnthropicStructuredExtractor(HttpClient httpClient, IConfiguration configuration, ILogger<AnthropicStructuredExtractor> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Ai:Anthropic:ApiKey"];
        _model = configuration["Ai:Anthropic:Model"] is { Length: > 0 } configured ? configured : DefaultModel;
        _logger = logger;
    }

    public async Task<Result<JsonElement>> ExtractAsync(AiStructuredExtractionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Ai:Anthropic:ApiKey is not configured — AI extraction was not attempted.");
            return Result<JsonElement>.Failure(AiErrorCodes.NotConfigured, "AI generation isn't configured for this environment.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Headers =
            {
                { "x-api-key", _apiKey },
                { "anthropic-version", AnthropicVersion },
            },
            Content = JsonContent.Create(new
            {
                model = _model,
                max_tokens = request.MaxTokens,
                system = request.SystemPrompt,
                messages = new[] { new { role = "user", content = request.UserContent } },
                tools = new[] { new { name = request.ToolName, description = request.ToolDescription, input_schema = request.InputSchema } },
                tool_choice = new { type = "tool", name = request.ToolName },
            }),
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Anthropic Messages API call failed.");
            return Failed();
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic Messages API returned {StatusCode}: {Body}", (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            return Failed();
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<MessageResponse>(JsonOptions, cancellationToken);
            var input = payload?.Content?.FirstOrDefault(block => block.Type == "tool_use" && block.Name == request.ToolName)?.Input;
            if (input is null)
            {
                _logger.LogError("Anthropic Messages API response did not contain the expected {ToolName} tool_use block.", request.ToolName);
                return Failed();
            }

            return Result<JsonElement>.Success(input.Value.Clone());
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _logger.LogError(ex, "Failed to parse the Anthropic Messages API response.");
            return Failed();
        }
    }

    private static Result<JsonElement> Failed() => Result<JsonElement>.Failure(AiErrorCodes.RequestFailed, "The AI generation request failed.");

    private sealed class MessageResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock>? Content { get; init; }
    }

    private sealed class ContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("input")]
        public JsonElement? Input { get; init; }
    }
}
