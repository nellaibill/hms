using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>OpenAI Chat Completions with a forced function call; config under <c>Ai:OpenAI:*</c>.</summary>
internal sealed class OpenAiStructuredExtractor : IAiStructuredExtractor
{
    private const string DefaultModel = "gpt-4o-mini";
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
            Content = JsonContent.Create(new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserContent },
                },
                tools = new[]
                {
                    new
                    {
                        type = "function",
                        function = new { name = request.ToolName, description = request.ToolDescription, parameters = request.InputSchema },
                    },
                },
                tool_choice = new { type = "function", function = new { name = request.ToolName } },
            }),
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "OpenAI Chat Completions API call failed.");
            return Failed();
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI Chat Completions API returned {StatusCode}: {Body}", (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            return Failed();
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<CompletionResponse>(JsonOptions, cancellationToken);
            var arguments = payload?.Choices?.FirstOrDefault()?.Message?.ToolCalls?.FirstOrDefault(call => call.Function?.Name == request.ToolName)?.Function?.Arguments;
            if (string.IsNullOrWhiteSpace(arguments))
            {
                _logger.LogError("OpenAI Chat Completions API response did not contain the expected {ToolName} function call.", request.ToolName);
                return Failed();
            }

            using var document = JsonDocument.Parse(arguments);
            return Result<JsonElement>.Success(document.RootElement.Clone());
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _logger.LogError(ex, "Failed to parse the OpenAI Chat Completions API response.");
            return Failed();
        }
    }

    private static Result<JsonElement> Failed() => Result<JsonElement>.Failure(AiErrorCodes.RequestFailed, "The AI generation request failed.");

    private sealed class CompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; init; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public ChoiceMessage? Message { get; init; }
    }

    private sealed class ChoiceMessage
    {
        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; init; }
    }

    private sealed class ToolCall
    {
        [JsonPropertyName("function")]
        public FunctionCall? Function { get; init; }
    }

    private sealed class FunctionCall
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; init; }
    }
}
