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
/// Calls the Anthropic Messages API with a forced tool-use call, config-driven under
/// <c>Ai:Anthropic:*</c> — same "no vendor SDK dependency, one HTTP call, config decides the
/// endpoint" style as HMS.Modules.Notifications.Infrastructure.HttpSmsSender. Forcing
/// <c>tool_choice</c> to the one <c>structure_note</c> tool guarantees the model's reply is the
/// tool's <c>input</c> JSON matching StructuredConsultationNoteResponse's own shape, rather than
/// free-form prose that would need separate parsing/repair.
///
/// Missing configuration or a failed call always returns a Result.Failure rather than throwing
/// or silently no-oping — see IClinicalNoteAiClient's own doc comment for why this differs from
/// HttpSmsSender's best-effort convention.
/// </summary>
internal sealed class AnthropicClinicalNoteAiClient : IClinicalNoteAiClient
{
    private const string DefaultModel = "claude-sonnet-5";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private const string SystemPrompt =
        "You are a clinical scribe. You are given a raw transcript of an outpatient consultation " +
        "(dictated by the consultant, or a conversation between consultant and patient). Extract " +
        "only what the transcript actually states into the structure_note tool's fields — do not " +
        "invent findings, diagnoses, or medications that are not mentioned. Write each field as " +
        "concise clinical shorthand suitable for a paper/EMR note, not full prose. Leave a field " +
        "empty if the transcript says nothing relevant to it.";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<AnthropicClinicalNoteAiClient> _logger;

    public AnthropicClinicalNoteAiClient(HttpClient httpClient, IConfiguration configuration, ILogger<AnthropicClinicalNoteAiClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Ai:Anthropic:ApiKey"];
        _model = configuration["Ai:Anthropic:Model"] is { Length: > 0 } configuredModel ? configuredModel : DefaultModel;
        _logger = logger;
    }

    public async Task<Result<StructuredConsultationNoteResponse>> StructureAsync(string transcript, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Ai:Anthropic:ApiKey is not configured — cannot structure a consultation transcript.");
            return Result<StructuredConsultationNoteResponse>.Failure(
                OpdConsultationErrorCodes.AiNotConfigured,
                "AI note generation isn't configured for this environment.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Headers =
            {
                { "x-api-key", _apiKey },
                { "anthropic-version", AnthropicVersion },
            },
            Content = JsonContent.Create(BuildRequestBody(transcript)),
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Anthropic Messages API call failed.");
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Anthropic Messages API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(JsonOptions, cancellationToken);
            var toolUseInput = payload?.Content?.FirstOrDefault(block => block.Type == "tool_use" && block.Name == "structure_note")?.Input;
            if (toolUseInput is null)
            {
                _logger.LogError("Anthropic Messages API response did not contain the expected structure_note tool_use block.");
                return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
            }

            var fields = toolUseInput.Value.Deserialize<StructuredConsultationNoteResponse>(JsonOptions) ?? new StructuredConsultationNoteResponse();
            return Result<StructuredConsultationNoteResponse>.Success(fields);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _logger.LogError(ex, "Failed to parse the Anthropic Messages API response.");
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }
    }

    private object BuildRequestBody(string transcript) => new
    {
        model = _model,
        max_tokens = 1024,
        system = SystemPrompt,
        messages = new[] { new { role = "user", content = transcript } },
        tools = new[]
        {
            new
            {
                name = "structure_note",
                description = "Records the structured clinical note fields extracted from the transcript.",
                input_schema = new
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
        tool_choice = new { type = "tool", name = "structure_note" },
    };

    private sealed class AnthropicMessageResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContentBlock>? Content { get; init; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("input")]
        public JsonElement? Input { get; init; }
    }
}
