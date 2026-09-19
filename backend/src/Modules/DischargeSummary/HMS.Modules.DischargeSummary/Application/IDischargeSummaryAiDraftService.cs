using System.Text.Json;
using HMS.Modules.DischargeSummary.Application.Abstractions;
using HMS.Modules.DischargeSummary.Contracts;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.DischargeSummary.Application;

/// <summary>
/// Public (not internal) for the same CS0051 reason as <see cref="IDischargeSummaryService"/>:
/// DischargeSummariesController takes it as a constructor dependency.
/// </summary>
public interface IDischargeSummaryAiDraftService
{
    /// <summary>Drafts narrative fields for a Draft summary from its admission's IPD data via the
    /// configured AI provider, and copies vitals from the last IPD reading. Nothing is saved.
    /// Fails with NotFound, NotDraft once Finalized, InvalidAdmission, AiNotConfigured or
    /// AiRequestFailed.</summary>
    Task<Result<DischargeSummaryDraftSuggestion>> DraftAsync(Guid id, CancellationToken cancellationToken);
}

internal sealed class DischargeSummaryAiDraftService : IDischargeSummaryAiDraftService
{
    private const string ToolName = "draft_discharge_summary";

    private const string SystemPrompt =
        "You are a clinical documentation assistant drafting the narrative sections of a hospital " +
        "discharge summary for the treating doctor to review. You are given the admission record, " +
        "progress notes, vitals, doctor orders and inpatient medication orders as plain text. " +
        "Write ONLY what that record supports: do not invent findings, diagnoses, investigations, " +
        "procedures, doses or dates. Use concise clinical prose suited to a discharge summary. " +
        "For advice fields (diet, wound care, activity, physiotherapy, review, emergency) fill a " +
        "field only if the record states or clearly implies that advice; otherwise leave it empty. " +
        "Never list discharge medications — inpatient orders are not discharge prescriptions. " +
        "Leave any field empty when the record has nothing relevant to it.";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly object InputSchema = new
    {
        type = "object",
        properties = new
        {
            chiefComplaints = new { type = "string", description = "Presenting complaints on admission." },
            historyOfPresentingIllness = new { type = "string", description = "History of the presenting illness, from the admission reason and early notes." },
            courseInHospital = new { type = "string", description = "Summary of the hospital course: key events, treatment given, response, complications." },
            conditionAtDischarge = new { type = "string", description = "Patient's condition at the time of discharge." },
            diet = new { type = "string", description = "Dietary advice at discharge, only if documented." },
            woundCare = new { type = "string", description = "Wound/dressing care advice, only if documented." },
            activity = new { type = "string", description = "Activity/mobility advice, only if documented." },
            physiotherapy = new { type = "string", description = "Physiotherapy advice, only if documented." },
            reviewInstructions = new { type = "string", description = "Follow-up/review instructions, only if documented." },
            emergencyInstructions = new { type = "string", description = "When to seek urgent care, only if documented." },
        },
    };

    private readonly IDischargeSummaryRepository _repository;
    private readonly IAdmissionService _admissionService;
    private readonly IProgressNoteService _progressNoteService;
    private readonly IVitalsReadingService _vitalsReadingService;
    private readonly IDoctorOrderService _doctorOrderService;
    private readonly IMedicationOrderService _medicationOrderService;
    private readonly IAiStructuredExtractor _extractor;
    private readonly ILogger<DischargeSummaryAiDraftService> _logger;

    public DischargeSummaryAiDraftService(
        IDischargeSummaryRepository repository,
        IAdmissionService admissionService,
        IProgressNoteService progressNoteService,
        IVitalsReadingService vitalsReadingService,
        IDoctorOrderService doctorOrderService,
        IMedicationOrderService medicationOrderService,
        IAiStructuredExtractor extractor,
        ILogger<DischargeSummaryAiDraftService> logger)
    {
        _repository = repository;
        _admissionService = admissionService;
        _progressNoteService = progressNoteService;
        _vitalsReadingService = vitalsReadingService;
        _doctorOrderService = doctorOrderService;
        _medicationOrderService = medicationOrderService;
        _extractor = extractor;
        _logger = logger;
    }

    public async Task<Result<DischargeSummaryDraftSuggestion>> DraftAsync(Guid id, CancellationToken cancellationToken)
    {
        var summary = await _repository.GetByIdAsync(id, cancellationToken);
        if (summary is null)
        {
            return Result<DischargeSummaryDraftSuggestion>.Failure(DischargeSummaryErrorCodes.NotFound, $"Discharge summary '{id}' was not found.");
        }

        if (summary.Status != DischargeSummaryStatus.Draft)
        {
            return Result<DischargeSummaryDraftSuggestion>.Failure(
                DischargeSummaryErrorCodes.NotDraft,
                $"Discharge summary '{id}' can no longer be edited once finalized.");
        }

        var admissionResult = await _admissionService.GetByIdAsync(summary.AdmissionId, cancellationToken);
        if (!admissionResult.IsSuccess)
        {
            return Result<DischargeSummaryDraftSuggestion>.Failure(
                DischargeSummaryErrorCodes.InvalidAdmission,
                $"Admission '{summary.AdmissionId}' was not found.");
        }

        // Sequential on purpose: these IPD services share one scoped DbContext.
        var progressNotes = ValueOrEmpty(await _progressNoteService.GetByAdmissionIdAsync(summary.AdmissionId, cancellationToken));
        var vitals = ValueOrEmpty(await _vitalsReadingService.GetByAdmissionIdAsync(summary.AdmissionId, cancellationToken));
        var doctorOrders = ValueOrEmpty(await _doctorOrderService.GetByAdmissionIdAsync(summary.AdmissionId, cancellationToken));
        var medicationOrders = ValueOrEmpty(await _medicationOrderService.GetByAdmissionIdAsync(summary.AdmissionId, cancellationToken));

        var context = DischargeSummaryContextBuilder.Build(admissionResult.Value!, progressNotes, vitals, doctorOrders, medicationOrders);

        var extraction = await _extractor.ExtractAsync(
            new AiStructuredExtractionRequest(
                SystemPrompt,
                context,
                ToolName,
                "Records the drafted narrative sections of the discharge summary.",
                InputSchema),
            cancellationToken);

        if (!extraction.IsSuccess)
        {
            return Result<DischargeSummaryDraftSuggestion>.Failure(
                extraction.ErrorCode == AiErrorCodes.NotConfigured ? DischargeSummaryErrorCodes.AiNotConfigured : DischargeSummaryErrorCodes.AiRequestFailed,
                extraction.Error!);
        }

        NarrativeFields narrative;
        try
        {
            narrative = extraction.Value.Deserialize<NarrativeFields>(JsonOptions) ?? new NarrativeFields();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "AI discharge summary draft for {DischargeSummaryId} was not the expected shape.", id);
            return Result<DischargeSummaryDraftSuggestion>.Failure(DischargeSummaryErrorCodes.AiRequestFailed, "The AI generation request failed.");
        }

        _logger.LogInformation("Drafted discharge summary {DischargeSummaryId} with AI", id);

        var lastVitals = vitals.OrderBy(v => v.RecordedAt).LastOrDefault();

        return Result<DischargeSummaryDraftSuggestion>.Success(new DischargeSummaryDraftSuggestion
        {
            ChiefComplaints = Clean(narrative.ChiefComplaints),
            HistoryOfPresentingIllness = Clean(narrative.HistoryOfPresentingIllness),
            CourseInHospital = Clean(narrative.CourseInHospital),
            ConditionAtDischarge = Clean(narrative.ConditionAtDischarge),
            Diet = Clean(narrative.Diet),
            WoundCare = Clean(narrative.WoundCare),
            Activity = Clean(narrative.Activity),
            Physiotherapy = Clean(narrative.Physiotherapy),
            ReviewInstructions = Clean(narrative.ReviewInstructions),
            EmergencyInstructions = Clean(narrative.EmergencyInstructions),

            HeightCm = lastVitals?.HeightCm,
            WeightKg = lastVitals?.WeightKg,
            PulseRate = lastVitals?.PulseRate,
            RespiratoryRate = lastVitals?.RespiratoryRate,
            TemperatureF = lastVitals?.TemperatureF,
            SpO2Percent = lastVitals?.SpO2Percent,
            BloodPressure = lastVitals is { BloodPressureSystolic: not null, BloodPressureDiastolic: not null }
                ? $"{lastVitals.BloodPressureSystolic}/{lastVitals.BloodPressureDiastolic}"
                : null,
        });
    }

    private static IReadOnlyList<T> ValueOrEmpty<T>(Result<IReadOnlyList<T>> result) => result.IsSuccess ? result.Value! : [];

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class NarrativeFields
    {
        public string? ChiefComplaints { get; init; }
        public string? HistoryOfPresentingIllness { get; init; }
        public string? CourseInHospital { get; init; }
        public string? ConditionAtDischarge { get; init; }
        public string? Diet { get; init; }
        public string? WoundCare { get; init; }
        public string? Activity { get; init; }
        public string? Physiotherapy { get; init; }
        public string? ReviewInstructions { get; init; }
        public string? EmergencyInstructions { get; init; }
    }
}
