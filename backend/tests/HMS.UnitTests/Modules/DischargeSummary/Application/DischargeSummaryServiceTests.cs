using FluentAssertions;
using HMS.Modules.DischargeSummary.Application;
using HMS.Modules.DischargeSummary.Application.Abstractions;
using HMS.Modules.DischargeSummary.Contracts;
using HMS.Modules.IPD.Application;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using AdmissionResponse = HMS.Modules.IPD.Contracts.AdmissionResponse;
using AdmissionStatus = HMS.Modules.IPD.Contracts.AdmissionStatus;
using PatientResponse = HMS.Modules.Patients.Contracts.PatientResponse;

namespace HMS.UnitTests.Modules.DischargeSummary.Application;

public class DischargeSummaryServiceTests
{
    private readonly IDischargeSummaryRepository _repository = Substitute.For<IDischargeSummaryRepository>();
    private readonly IAdmissionService _admissionService = Substitute.For<IAdmissionService>();
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly ILogger<DischargeSummaryService> _logger = Substitute.For<ILogger<DischargeSummaryService>>();
    private readonly DischargeSummaryService _sut;

    private readonly Guid _admissionId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public DischargeSummaryServiceTests()
    {
        _sut = new DischargeSummaryService(_repository, _admissionService, _patientService, _logger);

        // Happy-path defaults: a Discharged admission with no existing summary, and a valid
        // patient. Each failure-path test overrides the relevant substitute.
        _admissionService.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<AdmissionResponse>.Success(new AdmissionResponse
            {
                Id = _admissionId,
                PatientId = _patientId,
                Status = AdmissionStatus.Discharged,
                FinalDiagnosis = "Acute appendicitis",
            }));
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((HMS.Modules.DischargeSummary.Domain.DischargeSummary?)null);
        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse { FirstName = "John", LastName = "Doe" }));
    }

    [Fact]
    public async Task CreateDraftAsync_WithDischargedAdmissionAndNoExisting_CreatesDraftWithPrefilledFinalDiagnosis()
    {
        var result = await _sut.CreateDraftAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(DischargeSummaryStatus.Draft);
        result.Value.FinalDiagnosis.Should().Be("Acute appendicitis");
        result.Value.AdmissionId.Should().Be(_admissionId);
        result.Value.PatientId.Should().Be(_patientId);
        await _repository.Received(1).AddAsync(Arg.Any<HMS.Modules.DischargeSummary.Domain.DischargeSummary>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraftAsync_WhenAdmissionDoesNotExist_ReturnsInvalidAdmissionFailure()
    {
        _admissionService.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<AdmissionResponse>.Failure("IPD.NOT_FOUND", "not found"));

        var result = await _sut.CreateDraftAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.InvalidAdmission);
        await _repository.DidNotReceive().AddAsync(Arg.Any<HMS.Modules.DischargeSummary.Domain.DischargeSummary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraftAsync_WhenAdmissionNotDischarged_ReturnsAdmissionNotDischargedFailure()
    {
        _admissionService.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>())
            .Returns(Result<AdmissionResponse>.Success(new AdmissionResponse
            {
                Id = _admissionId,
                PatientId = _patientId,
                Status = AdmissionStatus.Admitted,
            }));

        var result = await _sut.CreateDraftAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.AdmissionNotDischarged);
        await _repository.DidNotReceive().AddAsync(Arg.Any<HMS.Modules.DischargeSummary.Domain.DischargeSummary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraftAsync_WhenSummaryAlreadyExistsForAdmission_ReturnsAlreadyExistsFailure()
    {
        var existing = HMS.Modules.DischargeSummary.Domain.DischargeSummary.Create(_admissionId, _patientId, null, null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.CreateDraftAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.AlreadyExists);
        await _repository.DidNotReceive().AddAsync(Arg.Any<HMS.Modules.DischargeSummary.Domain.DischargeSummary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraftAsync_WhenPatientDoesNotExist_ReturnsInvalidPatientFailure()
    {
        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Failure("PATIENTS.NOT_FOUND", "not found"));

        var result = await _sut.CreateDraftAsync(_admissionId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.InvalidPatient);
        await _repository.DidNotReceive().AddAsync(Arg.Any<HMS.Modules.DischargeSummary.Domain.DischargeSummary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HMS.Modules.DischargeSummary.Domain.DischargeSummary?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenDraft_AppliesClinicalDetails()
    {
        var summary = HMS.Modules.DischargeSummary.Domain.DischargeSummary.Create(_admissionId, _patientId, "Initial", null);
        _repository.GetByIdAsync(summary.Id, Arg.Any<CancellationToken>()).Returns(summary);

        var result = await _sut.UpdateAsync(
            summary.Id,
            new UpdateDischargeSummaryRequest
            {
                FinalDiagnosis = "Updated diagnosis",
                ChiefComplaints = "Fever",
                BloodPressure = "118/76",
                ConditionAtDischarge = "Stable",
            },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FinalDiagnosis.Should().Be("Updated diagnosis");
        result.Value.ChiefComplaints.Should().Be("Fever");
        result.Value.BloodPressure.Should().Be("118/76");
        result.Value.ConditionAtDischarge.Should().Be("Stable");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenAlreadyFinalized_ReturnsNotDraftFailure()
    {
        var summary = HMS.Modules.DischargeSummary.Domain.DischargeSummary.Create(_admissionId, _patientId, "Diagnosis", null);
        summary.Finalize(null, null, null, DateTime.UtcNow, null);
        _repository.GetByIdAsync(summary.Id, Arg.Any<CancellationToken>()).Returns(summary);

        var result = await _sut.UpdateAsync(summary.Id, new UpdateDischargeSummaryRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.NotDraft);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HMS.Modules.DischargeSummary.Domain.DischargeSummary?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateDischargeSummaryRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.NotFound);
    }

    [Fact]
    public async Task FinalizeAsync_WhenDraft_TransitionsToFinalizedAndStampsFields()
    {
        var summary = HMS.Modules.DischargeSummary.Domain.DischargeSummary.Create(_admissionId, _patientId, "Diagnosis", null);
        _repository.GetByIdAsync(summary.Id, Arg.Any<CancellationToken>()).Returns(summary);
        var actorId = Guid.NewGuid();
        var preparedBy = Guid.NewGuid();

        var result = await _sut.FinalizeAsync(
            summary.Id,
            new FinalizeDischargeSummaryRequest { PreparedByUserId = preparedBy },
            actorId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(DischargeSummaryStatus.Finalized);
        result.Value.PreparedByUserId.Should().Be(preparedBy);
        result.Value.FinalizedByUserId.Should().Be(actorId);
        result.Value.FinalizedAt.Should().NotBeNull();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinalizeAsync_WhenAlreadyFinalized_ReturnsAlreadyFinalizedFailure()
    {
        var summary = HMS.Modules.DischargeSummary.Domain.DischargeSummary.Create(_admissionId, _patientId, "Diagnosis", null);
        summary.Finalize(null, null, null, DateTime.UtcNow, null);
        _repository.GetByIdAsync(summary.Id, Arg.Any<CancellationToken>()).Returns(summary);

        var result = await _sut.FinalizeAsync(summary.Id, new FinalizeDischargeSummaryRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.AlreadyFinalized);
    }

    [Fact]
    public async Task FinalizeAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HMS.Modules.DischargeSummary.Domain.DischargeSummary?)null);

        var result = await _sut.FinalizeAsync(Guid.NewGuid(), new FinalizeDischargeSummaryRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DischargeSummaryErrorCodes.NotFound);
    }
}
