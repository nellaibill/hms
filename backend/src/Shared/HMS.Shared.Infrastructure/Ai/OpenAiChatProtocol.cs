using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Shared.Infrastructure.Ai;

/// <summary>
/// The Chat Completions wire format that OpenAI and Azure OpenAI share (forced function call,
/// reply's <c>arguments</c> is a JSON-encoded string). The two providers differ only in URL,
/// auth header and how the model is named, so each extractor builds its own request message and
/// hands it here.
/// </summary>
internal static class OpenAiChatProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <param name="model">Sent as "model" when set; null omits it (Azure names the model by deployment in the URL).</param>
    public static JsonContent BuildBody(AiStructuredExtractionRequest request, string? model)
    {
        var body = new Dictionary<string, object?>();
        if (model is not null) body["model"] = model;
        body["messages"] = new[]
        {
            new { role = "system", content = request.SystemPrompt },
            new { role = "user", content = request.UserContent },
        };
        body["tools"] = new[]
        {
            new
            {
                type = "function",
                function = new { name = request.ToolName, description = request.ToolDescription, parameters = request.InputSchema },
            },
        };
        body["tool_choice"] = new { type = "function", function = new { name = request.ToolName } };

        return JsonContent.Create(body);
    }

    public static async Task<Result<JsonElement>> SendAsync(
        HttpClient httpClient,
        HttpRequestMessage message,
        string toolName,
        string providerName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Provider} Chat Completions API call failed.", providerName);
            return Failed();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("{Provider} Chat Completions API returned {StatusCode}: {Body}", providerName, (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            return Failed();
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<CompletionResponse>(JsonOptions, cancellationToken);
            var arguments = payload?.Choices?.FirstOrDefault()?.Message?.ToolCalls?.FirstOrDefault(call => call.Function?.Name == toolName)?.Function?.Arguments;
            if (string.IsNullOrWhiteSpace(arguments))
            {
                logger.LogError("{Provider} Chat Completions API response did not contain the expected {ToolName} function call.", providerName, toolName);
                return Failed();
            }

            using var document = JsonDocument.Parse(arguments);
            return Result<JsonElement>.Success(document.RootElement.Clone());
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            logger.LogError(ex, "Failed to parse the {Provider} Chat Completions API response.", providerName);
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
