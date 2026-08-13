using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Shared.Kernel;
using PatientResponse = HMS.Modules.Patients.Contracts.PatientResponse;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class AdmissionServiceTests
{
    private readonly IAdmissionRepository _repository = Substitute.For<IAdmissionRepository>();
    private readonly IWardRepository _wardRepository = Substitute.For<IWardRepository>();
    private readonly IBedRepository _bedRepository = Substitute.For<IBedRepository>();
    private readonly IBedTransferHistoryRepository _transferHistoryRepository = Substitute.For<IBedTransferHistoryRepository>();
    private readonly IAdmissionBedStayRepository _bedStayRepository = Substitute.For<IAdmissionBedStayRepository>();
    private readonly IAdmissionChargeRepository _chargeRepository = Substitute.For<IAdmissionChargeRepository>();
    private readonly IAdmissionIdentifierGenerator _identifierGenerator = Substitute.For<IAdmissionIdentifierGenerator>();
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IConsultantService _consultantService = Substitute.For<IConsultantService>();
    private readonly AdmissionService _sut;

    private readonly Guid _wardId = Guid.NewGuid();
    private readonly Guid _bedId = Guid.NewGuid();

    public AdmissionServiceTests()
    {
        _sut = new AdmissionService(
            _repository,
            _wardRepository,
            _bedRepository,
            _transferHistoryRepository,
            _bedStayRepository,
            _chargeRepository,
            _identifierGenerator,
            _patientService,
            _departmentService,
            _consultantService);

        // Happy-path defaults: a valid patient, department, consultant, ward, and an
        // available bed in that ward, with no other active admission for the patient.
        // Each failure-path test overrides the relevant substitute.
        _patientService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse { Uhid = "P-2026-000001", FirstName = "John", LastName = "Doe", Age = 40 }));
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse()));
        _consultantService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ConsultantResponse>.Success(new ConsultantResponse { Name = "Dr. Smith" }));

        var ward = Ward.Create("genmed", "General Medicine Ward A", Guid.NewGuid(), WardType.General, true, null);
        _wardRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ward);

        var bed = Bed.Create(_wardId, "b-101", "Standard", BedStatus.Available, true, 1500m, null);
        _bedRepository.GetByIdAsync(_bedId, Arg.Any<CancellationToken>()).Returns(bed);

        _repository.GetActiveByPatientIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Admission?)null);
        _identifierGenerator.NextAdmissionNumberAsync(Arg.Any<CancellationToken>()).Returns("ADM-2026-000001");
        _bedStayRepository.GetByAdmissionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<AdmissionBedStay>());
    }

    private CreateAdmissionRequest NewCreateRequest() => new()
    {
        PatientId = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        ConsultantId = Guid.NewGuid(),
        WardId = _wardId,
        BedId = _bedId,
        AdmissionDateTime = new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc),
        AdmissionType = AdmissionType.Elective,
        ReasonForAdmission = "Observation",
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAdmissionAndOccupiesBed()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AdmissionNumber.Should().Be("ADM-2026-000001");
        result.Value.Uhid.Should().Be("P-2026-000001");
        result.Value.Status.Should().Be(AdmissionStatus.Admitted);
        await _repository.Received(1).AddAsync(Arg.Any<Admission>(), Arg.Any<CancellationToken>());

        var bed = await _bedRepository.GetByIdAsync(_bedId, CancellationToken.None);
        bed!.Status.Should().Be(BedStatus.Occupied);
    }

    [Fact]
    public async Task CreateAsync_WhenPatientDoesNotExist_ReturnsInvalidPatientFailure()
    {
        _patientService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Failure("PATIENTS.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidPatient);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Admission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenBedIsNotAvailable_ReturnsBedNotAvailableFailure()
    {
        var occupiedBed = Bed.Create(_wardId, "b-101", "Standard", BedStatus.Occupied, true, 1500m, null);
        _bedRepository.GetByIdAsync(_bedId, Arg.Any<CancellationToken>()).Returns(occupiedBed);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.BedNotAvailable);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Admission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenBedBelongsToADifferentWard_ReturnsInvalidBedFailure()
    {
        var otherWardBed = Bed.Create(Guid.NewGuid(), "b-999", "Standard", BedStatus.Available, true, 1500m, null);
        _bedRepository.GetByIdAsync(_bedId, Arg.Any<CancellationToken>()).Returns(otherWardBed);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidBed);
    }

    [Fact]
    public async Task CreateAsync_WhenPatientAlreadyHasAnActiveAdmission_ReturnsPatientAlreadyAdmittedFailure()
    {
        var request = NewCreateRequest();
        var existing = Admission.Create("ADM-2026-000000", request.PatientId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Emergency, "Prior admission", null);
        _repository.GetActiveByPatientIdAsync(request.PatientId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.PatientAlreadyAdmitted);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Admission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferBedAsync_WhenNewBedIsAvailable_MovesPatientAndFreesOldBed()
    {
        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _wardId, _bedId, DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _repository.GetByIdAsync(admission.Id, Arg.Any<CancellationToken>()).Returns(admission);

        var oldBed = Bed.Create(_wardId, "b-101", "Standard", BedStatus.Occupied, true, 1500m, null);
        _bedRepository.GetByIdAsync(_bedId, Arg.Any<CancellationToken>()).Returns(oldBed);

        var newBedId = Guid.NewGuid();
        var newBed = Bed.Create(_wardId, "b-102", "Standard", BedStatus.Available, true, 2500m, null);
        _bedRepository.GetByIdAsync(newBedId, Arg.Any<CancellationToken>()).Returns(newBed);

        var request = new TransferBedRequest { NewWardId = _wardId, NewBedId = newBedId, TransferReason = "Ward change" };
        var result = await _sut.TransferBedAsync(admission.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        admission.BedId.Should().Be(newBedId);
        oldBed.Status.Should().Be(BedStatus.Available);
        newBed.Status.Should().Be(BedStatus.Occupied);

        await _transferHistoryRepository.Received(1).AddAsync(
            Arg.Is<BedTransferHistory>(h =>
                h.AdmissionId == admission.Id
                && h.OldWardId == _wardId
                && h.OldBedId == _bedId
                && h.NewWardId == _wardId
                && h.NewBedId == newBedId
                && h.TransferReason == "Ward change"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferBedAsync_WhenAdmissionAlreadyDischarged_ReturnsAdmissionAlreadyDischargedFailure()
    {
        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _wardId, _bedId, DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        admission.Discharge(DateTime.UtcNow, DischargeType.Normal, null, null, null, null);
        _repository.GetByIdAsync(admission.Id, Arg.Any<CancellationToken>()).Returns(admission);

        var request = new TransferBedRequest { NewWardId = _wardId, NewBedId = Guid.NewGuid() };
        var result = await _sut.TransferBedAsync(admission.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.AdmissionAlreadyDischarged);
    }

    [Fact]
    public async Task GetTransferHistoryAsync_WhenAdmissionExists_ReturnsDenormalizedHistory()
    {
        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _wardId, _bedId, DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _repository.GetByIdAsync(admission.Id, Arg.Any<CancellationToken>()).Returns(admission);

        var newWardId = Guid.NewGuid();
        var newBedId = Guid.NewGuid();
        var newBed = Bed.Create(newWardId, "b-202", "Standard", BedStatus.Occupied, true, 1500m, null);
        _bedRepository.GetByIdAsync(newBedId, Arg.Any<CancellationToken>()).Returns(newBed);

        var history = BedTransferHistory.Create(admission.Id, _wardId, _bedId, newWardId, newBedId, "Ward change", null);
        _transferHistoryRepository.GetByAdmissionIdAsync(admission.Id, Arg.Any<CancellationToken>())
            .Returns(new List<BedTransferHistory> { history });

        var result = await _sut.GetTransferHistoryAsync(admission.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var entry = result.Value![0];
        entry.OldWardId.Should().Be(_wardId);
        entry.OldBedId.Should().Be(_bedId);
        entry.NewWardId.Should().Be(newWardId);
        entry.NewBedId.Should().Be(newBedId);
        entry.NewBedNumber.Should().Be("B-202");
        entry.TransferReason.Should().Be("Ward change");
    }

    [Fact]
    public async Task GetTransferHistoryAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Admission?)null);

        var result = await _sut.GetTransferHistoryAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
    }

    [Fact]
    public async Task DischargeAsync_WithValidRequest_DischargesAndFreesBed()
    {
        var admissionDate = new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);
        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _wardId, _bedId, admissionDate, AdmissionType.Elective, "Observation", null);
        _repository.GetByIdAsync(admission.Id, Arg.Any<CancellationToken>()).Returns(admission);

        var bed = Bed.Create(_wardId, "b-101", "Standard", BedStatus.Occupied, true, 1500m, null);
        _bedRepository.GetByIdAsync(_bedId, Arg.Any<CancellationToken>()).Returns(bed);

        var request = new DischargeAdmissionRequest
        {
            DischargeDateTime = admissionDate.AddDays(3),
            DischargeType = DischargeType.Normal,
            FinalDiagnosis = "Resolved",
        };

        var result = await _sut.DischargeAsync(admission.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        admission.Status.Should().Be(AdmissionStatus.Discharged);
        bed.Status.Should().Be(BedStatus.Available);
    }

    [Fact]
    public async Task DischargeAsync_WhenDischargeDateIsBeforeAdmissionDate_ReturnsInvalidDischargeDateFailure()
    {
        var admissionDate = new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);
        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _wardId, _bedId, admissionDate, AdmissionType.Elective, "Observation", null);
        _repository.GetByIdAsync(admission.Id, Arg.Any<CancellationToken>()).Returns(admission);

        var request = new DischargeAdmissionRequest
        {
            DischargeDateTime = admissionDate.AddDays(-1),
            DischargeType = DischargeType.Normal,
        };

        var result = await _sut.DischargeAsync(admission.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidDischargeDate);
        admission.Status.Should().Be(AdmissionStatus.Admitted);
    }

    [Fact]
    public async Task DischargeAsync_WhenAlreadyDischarged_ReturnsAdmissionAlreadyDischargedFailure()
    {
        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _wardId, _bedId, DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        admission.Discharge(DateTime.UtcNow, DischargeType.Normal, null, null, null, null);
        _repository.GetByIdAsync(admission.Id, Arg.Any<CancellationToken>()).Returns(admission);

        var request = new DischargeAdmissionRequest { DischargeDateTime = DateTime.UtcNow, DischargeType = DischargeType.Normal };
        var result = await _sut.DischargeAsync(admission.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.AdmissionAlreadyDischarged);
    }
}
