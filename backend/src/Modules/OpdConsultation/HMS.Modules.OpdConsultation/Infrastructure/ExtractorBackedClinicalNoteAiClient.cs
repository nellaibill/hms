using System.Text.Json;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.OpdConsultation.Application.Abstractions;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;

namespace HMS.Modules.OpdConsultation.Infrastructure;

/// <summary>
/// IClinicalNoteAiClient on top of the shared IAiStructuredExtractor — used for the providers
/// that only exist there (Azure OpenAI), so OPD doesn't grow a third copy of the HTTP client
/// code. Anthropic and OpenAI keep their own dedicated OPD clients, unchanged.
/// </summary>
internal sealed class ExtractorBackedClinicalNoteAiClient : IClinicalNoteAiClient
{
    private const string ToolName = "structure_note";

    private const string SystemPrompt =
        "You are a clinical scribe. You are given a raw transcript of an outpatient consultation " +
        "(dictated by the consultant, or a conversation between consultant and patient). Extract " +
        "only what the transcript actually states into the structure_note function's arguments — " +
        "do not invent findings, diagnoses, or medications that are not mentioned. Write each " +
        "field as concise clinical shorthand suitable for a paper/EMR note, not full prose. Leave " +
        "a field empty if the transcript says nothing relevant to it.";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly object InputSchema = new
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
    };

    private readonly IAiStructuredExtractor _extractor;

    public ExtractorBackedClinicalNoteAiClient(IAiStructuredExtractor extractor)
    {
        _extractor = extractor;
    }

    public async Task<Result<StructuredConsultationNoteResponse>> StructureAsync(string transcript, CancellationToken cancellationToken)
    {
        var extraction = await _extractor.ExtractAsync(
            new AiStructuredExtractionRequest(
                SystemPrompt,
                transcript,
                ToolName,
                "Records the structured clinical note fields extracted from the transcript.",
                InputSchema,
                MaxTokens: 1024),
            cancellationToken);

        if (!extraction.IsSuccess)
        {
            return extraction.ErrorCode == AiErrorCodes.NotConfigured
                ? Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiNotConfigured, "AI note generation isn't configured for this environment.")
                : Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }

        try
        {
            return Result<StructuredConsultationNoteResponse>.Success(
                extraction.Value.Deserialize<StructuredConsultationNoteResponse>(JsonOptions) ?? new StructuredConsultationNoteResponse());
        }
        catch (JsonException)
        {
            return Result<StructuredConsultationNoteResponse>.Failure(OpdConsultationErrorCodes.AiRequestFailed, "The AI note generation request failed.");
        }
    }
}
