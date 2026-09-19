using System.Text.Json;
using FluentAssertions;
using HMS.Modules.DischargeSummary.Application;
using HMS.Modules.DischargeSummary.Application.Abstractions;
using HMS.Modules.DischargeSummary.Contracts;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using DomainDischargeSummary = HMS.Modules.DischargeSummary.Domain.DischargeSummary;

namespace HMS.UnitTests.Modules.DischargeSummary.Application;

public class DischargeSummaryAiDraftServiceTests
{
    private readonly IDischargeSummaryRepository _repository = Substitute.For<IDischargeSummaryRepository>();
    private readonly IAdmissionService _admissionService = Substitute.For<IAdmissionService>();
    private readonly IProgressNoteService _progressNoteService = Substitute.For<IProgressNoteService>();
    private readonly IVitalsReadingService _vitalsReadingService = Substitute.For<IVitalsReadingService>();
    private readonly IDoctorOrderService _doctorOrderService = Substitute.For<IDoctorOrderService>();
    private readonly IMedicationOrderService _medicationOrderService = Substitute.For<IMedicationOrderService>();
    private readonly IAiStructuredExtractor _extractor = Substitute.For<IAiStructuredExtractor>();
    private readonly IDischargeSummaryAiDraftService _sut;

    private readonly Guid _admissionId = Guid.NewGuid();
    private readonly DomainDischargeSummary _summary;

    public DischargeSummaryAiDraftServiceTests()
    {
        _sut = new DischargeSummaryAiDraftService(
            _repository, _admissionService, _progressNoteService, _vitalsReadingService,
            _doctorOrderService, _medicationOrderService, _extractor, NullLogger<DischargeSummaryAiDraftService>.Instance);

        _summary = DomainDischargeSummary.Create(_admissionId, Guid.NewGuid(), "Acute appendicitis", createdBy: null);
        _repository.GetByIdAsync(_summary.Id, Arg.Any<CancellationToken>()).Returns(_summary);

        _admissionService.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<AdmissionResponse>.Success(new AdmissionResponse { Id = _admissionId, PatientName = "Jane Roe", Age = 54, Gender = "Female" }));
        _progressNoteService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ProgressNoteResponse>>.Success([new ProgressNoteResponse { NoteDateTime = DateTime.UtcNow, Progress = "Recovering well" }]));
        _vitalsReadingService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<VitalsReadingResponse>>.Success(
            [
                new VitalsReadingResponse { RecordedAt = new DateTime(2026, 9, 1), PulseRate = 110 },
                new VitalsReadingResponse { RecordedAt = new DateTime(2026, 9, 5), PulseRate = 76, SpO2Percent = 98, BloodPressureSystolic = 118, BloodPressureDiastolic = 76 },
            ]));
        _doctorOrderService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<DoctorOrderResponse>>.Success([]));
        _medicationOrderService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<MedicationOrderResponse>>.Success([]));
    }

    private static Result<JsonElement> Json(string json) => Result<JsonElement>.Success(JsonDocument.Parse(json).RootElement.Clone());

    [Fact]
    public async Task DraftAsync_WhenSummaryDoesNotExist_ReturnsNotFound()
    {
        var result = await _sut.DraftAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.NotFound);
        await _extractor.DidNotReceive().ExtractAsync(Arg.Any<AiStructuredExtractionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DraftAsync_WhenAlreadyFinalized_ReturnsNotDraftWithoutCallingTheAi()
    {
        _summary.Finalize(null, null, null, DateTime.UtcNow, finalizedByUserId: null);

        var result = await _sut.DraftAsync(_summary.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.NotDraft);
        await _extractor.DidNotReceive().ExtractAsync(Arg.Any<AiStructuredExtractionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DraftAsync_WhenAdmissionNoLongerResolves_ReturnsInvalidAdmission()
    {
        _admissionService.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<AdmissionResponse>.Failure("IPD.NOT_FOUND", "not found"));

        var result = await _sut.DraftAsync(_summary.Id, CancellationToken.None);

        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.InvalidAdmission);
    }

    [Fact]
    public async Task DraftAsync_OnSuccess_MapsNarrativeFieldsAndCopiesVitalsFromTheLastReading()
    {
        AiStructuredExtractionRequest? sent = null;
        _extractor.ExtractAsync(Arg.Do<AiStructuredExtractionRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(Json("""{"chiefComplaints":"  Abdominal pain ","courseInHospital":"Uneventful recovery.","diet":"   "}"""));

        var result = await _sut.DraftAsync(_summary.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChiefComplaints.Should().Be("Abdominal pain");
        result.Value.CourseInHospital.Should().Be("Uneventful recovery.");
        result.Value.Diet.Should().BeNull("blank AI fields must not overwrite anything downstream");
        result.Value.PulseRate.Should().Be(76, "vitals come from the most recent reading, not the first");
        result.Value.SpO2Percent.Should().Be(98);
        result.Value.BloodPressure.Should().Be("118/76");
        sent!.UserContent.Should().Contain("Recovering well").And.NotContain("Jane Roe");
    }

    [Fact]
    public async Task DraftAsync_WhenAiIsNotConfigured_ReturnsAiNotConfigured()
    {
        _extractor.ExtractAsync(Arg.Any<AiStructuredExtractionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(AiErrorCodes.NotConfigured, "not configured"));

        var result = await _sut.DraftAsync(_summary.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.AiNotConfigured);
    }

    [Fact]
    public async Task DraftAsync_WhenTheAiCallFails_ReturnsAiRequestFailed()
    {
        _extractor.ExtractAsync(Arg.Any<AiStructuredExtractionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(AiErrorCodes.RequestFailed, "boom"));

        var result = await _sut.DraftAsync(_summary.Id, CancellationToken.None);

        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.AiRequestFailed);
    }
}
