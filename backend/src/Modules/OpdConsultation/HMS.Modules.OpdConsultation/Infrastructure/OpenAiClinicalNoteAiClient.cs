using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Application.Abstractions;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.OpdConsultation.Infrastructure;

/// <summary>
/// Calls OpenAI's Chat Completions API with a forced function call, config-driven under
/// <c>Ai:OpenAI:*</c> — the OpenAI counterpart to AnthropicClinicalNoteAiClient, selected
/// instead of it by OpdConsultationModule based on <c>Ai:Provider</c> (see that module's own
/// comment). Shares the same SystemPrompt/field-schema shape as the Anthropic client so both
/// providers extract the same StructuredConsultationNoteResponse fields — only the wire format
/// (OpenAI's "tools"/"tool_choice" function-calling shape vs Anthropic's "tools"/tool_choice
/// shape, and OpenAI's arguments being a JSON-encoded *string* rather than an inline object)
/// differs.
///
/// Missing configuration or a failed call always returns a Result.Failure rather than throwing
/// or silently no-oping — see IClinicalNoteAiClient's own doc comment for why this differs from
/// HttpSmsSender's best-effort convention.
/// </summary>
internal sealed class OpenAiClinicalNoteAiClient : IClinicalNoteAiClient
{
    private const string DefaultModel = "gpt-4o-mini";
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    private const string SystemPrompt =
        "You are a clinical scribe. You are given a raw transcript of an outpatient consultation " +
        "(dictated by the consultant, or a conversation between consultant and patient). Extract " +
        "only what the transcript actually states into the structure_note function's arguments — " +
        "do not invent findings, diagnoses, or medications that are not mentioned. Write each " +
        "field as concise clinical shorthand suitable for a paper/EMR note, not full prose. Leave " +
        "a field empty if the transcript says nothing relevant to it.";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<OpenAiClinicalNoteAiClient> _logger;

    public OpenAiClinicalNoteAiClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiClinicalNoteAiClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Ai:OpenAI:ApiKey"];
        _model = configuration["Ai:OpenAI:Model"] is { Length: > 0 } configuredModel ? configuredModel : DefaultModel;
        _logger = logger;
    }

    public async Task<Result<StructuredConsultationNoteResponse>> StructureAsync(string transcript, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Ai:OpenAI:ApiKey is not configured — cannot structure a consultation transcript.");
            return Result<StructuredConsultationNoteResponse>.Failure(
                OpdConsultationErrorCodes.AiNotConfigured,
                "AI note generation isn't configured for this environment.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _apiKey) },
            Content = JsonContent.Create(BuildRequestBody(transcript)),
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "OpenAI Chat Completions API call failed.");
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI Chat Completions API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<OpenAiChatCompletionResponse>(JsonOptions, cancellationToken);
            var toolCall = payload?.Choices?.FirstOrDefault()?.Message?.ToolCalls?.FirstOrDefault(call => call.Function?.Name == "structure_note");
            var argumentsJson = toolCall?.Function?.Arguments;
            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                _logger.LogError("OpenAI Chat Completions API response did not contain the expected structure_note function call.");
                return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
            }

            var fields = JsonSerializer.Deserialize<StructuredConsultationNoteResponse>(argumentsJson, JsonOptions) ?? new StructuredConsultationNoteResponse();
            return Result<StructuredConsultationNoteResponse>.Success(fields);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _logger.LogError(ex, "Failed to parse the OpenAI Chat Completions API response.");
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }
    }

    private object BuildRequestBody(string transcript) => new
    {
        model = _model,
        messages = new[]
        {
            new { role = "system", content = SystemPrompt },
            new { role = "user", content = transcript },
        },
        tools = new[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "structure_note",
                    description = "Records the structured clinical note fields extracted from the transcript.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            presentingComplaints = new { type = "string", description = "Chief complaint(s), as stated." },
                            clinicalHistory = new { type = "string", description = "History of present illness / relevant past history." },
                            examinationFindings = new { type = "string", description = "Findings from physical examination." },
                            planOfManagement = new { type = "string", description = "Treatment/management plan, e.g. medications, procedures, advice." },
                            followUpInstructions = new { type = "string", description = "Routine follow-up/review instructions." },
                            emergencyReviewInstructions = new { type = "string", description = "Instructions for when the patient should seek urgent/emergency review." },
                        },
                    },
                },
            },
        },
        tool_choice = new { type = "function", function = new { name = "structure_note" } },
    };

    private sealed class OpenAiChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; init; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; init; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("tool_calls")]
        public List<OpenAiToolCall>? ToolCalls { get; init; }
    }

    private sealed class OpenAiToolCall
    {
        [JsonPropertyName("function")]
        public OpenAiFunctionCall? Function { get; init; }
    }

    private sealed class OpenAiFunctionCall
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; init; }
    }
}
