using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Laboratory.Application;
using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class IPDLabOrderServiceTests
{
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly ILabOrderService _labOrderService = Substitute.For<ILabOrderService>();
    private readonly IDiagnosticServiceService _diagnosticServiceService = Substitute.For<IDiagnosticServiceService>();
    private readonly IDiagnosticPackageService _diagnosticPackageService = Substitute.For<IDiagnosticPackageService>();
    private readonly IAdmissionChargeService _admissionChargeService = Substitute.For<IAdmissionChargeService>();
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly ILogger<IPDLabOrderService> _logger = Substitute.For<ILogger<IPDLabOrderService>>();
    private readonly IPDLabOrderService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public IPDLabOrderServiceTests()
    {
        _sut = new IPDLabOrderService(_admissionRepository, _labOrderService, _diagnosticServiceService, _diagnosticPackageService, _admissionChargeService, _patientService, _logger);

        var admission = Admission.Create("ADM-2026-000001", _patientId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);

        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>())
            .Returns(Result<PatientResponse>.Success(new PatientResponse { Id = _patientId, FirstName = "Aravind", LastName = "Nadar", Uhid = "NH20260001" }));

        _admissionChargeService.CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateAdmissionChargeRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AdmissionChargeResponse>.Success(new AdmissionChargeResponse()));
    }

    private static Result<LabOrderResponse> SuccessfulLabOrder() =>
        Result<LabOrderResponse>.Success(new LabOrderResponse { Id = Guid.NewGuid(), LabOrderNumber = "LAB-2026-000001" });

    [Fact]
    public async Task PlaceOrderAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreatePlaceLabOrderRequest { Lines = [new PlaceLabOrderLineRequest { ServiceId = Guid.NewGuid() }] };

        var result = await _sut.PlaceOrderAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
        await _labOrderService.DidNotReceive().CreateFromAdmissionAsync(Arg.Any<CreateLabOrderFromAdmissionRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenPatientCannotBeResolved_ReturnsInvalidPatientFailure()
    {
        _patientService.GetByIdAsync(_patientId, Arg.Any<CancellationToken>()).Returns(Result<PatientResponse>.Failure("PATIENTS.NOT_FOUND", "not found"));
        var request = new CreatePlaceLabOrderRequest { Lines = [new PlaceLabOrderLineRequest { ServiceId = Guid.NewGuid() }] };

        var result = await _sut.PlaceOrderAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidPatient);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithValidRequest_CreatesLabOrderWithResolvedPatientDetails()
    {
        _labOrderService.CreateFromAdmissionAsync(Arg.Any<CreateLabOrderFromAdmissionRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(SuccessfulLabOrder());
        var serviceId = Guid.NewGuid();
        var request = new CreatePlaceLabOrderRequest { Lines = [new PlaceLabOrderLineRequest { ServiceId = serviceId }] };

        var result = await _sut.PlaceOrderAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _labOrderService.Received(1).CreateFromAdmissionAsync(
            Arg.Is<CreateLabOrderFromAdmissionRequest>(r =>
                r.AdmissionId == _admissionId &&
                r.PatientId == _patientId &&
                r.PatientName == "Aravind Nadar" &&
                r.PatientUhid == "NH20260001" &&
                r.Lines.Single().ServiceId == serviceId),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenLabOrderServiceFails_PropagatesFailureAndDoesNotPostAnyCharge()
    {
        _labOrderService.CreateFromAdmissionAsync(Arg.Any<CreateLabOrderFromAdmissionRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<LabOrderResponse>.Failure(LaboratoryErrorCodes.EmptyOrder, "No laboratory items could be resolved."));
        var request = new CreatePlaceLabOrderRequest { Lines = [new PlaceLabOrderLineRequest { ServiceId = Guid.NewGuid() }] };

        var result = await _sut.PlaceOrderAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LaboratoryErrorCodes.EmptyOrder);
        await _admissionChargeService.DidNotReceive().CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateAdmissionChargeRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WithServiceLine_PostsOneLabChargeUsingServicePrice()
    {
        _labOrderService.CreateFromAdmissionAsync(Arg.Any<CreateLabOrderFromAdmissionRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(SuccessfulLabOrder());
        var serviceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = serviceId, Name = "CBC", Price = 250m }));
        var request = new CreatePlaceLabOrderRequest { Lines = [new PlaceLabOrderLineRequest { ServiceId = serviceId }] };

        await _sut.PlaceOrderAsync(_admissionId, request, actorId: null, CancellationToken.None);

        await _admissionChargeService.Received(1).CreateAsync(
            _admissionId,
            Arg.Is<CreateAdmissionChargeRequest>(c => c.ChargeType == ChargeType.LabCharge && c.Amount == 250m && c.Remarks == "Lab: CBC"),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WithPackageLine_PostsOneLabChargeUsingPackageTotalPrice()
    {
        _labOrderService.CreateFromAdmissionAsync(Arg.Any<CreateLabOrderFromAdmissionRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(SuccessfulLabOrder());
        var packageId = Guid.NewGuid();
        _diagnosticPackageService.GetByIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticPackageResponse>.Success(new DiagnosticPackageResponse { Id = packageId, Name = "Fever Panel", TotalPrice = 900m }));
        var request = new CreatePlaceLabOrderRequest { Lines = [new PlaceLabOrderLineRequest { PackageId = packageId }] };

        await _sut.PlaceOrderAsync(_admissionId, request, actorId: null, CancellationToken.None);

        await _admissionChargeService.Received(1).CreateAsync(
            _admissionId,
            Arg.Is<CreateAdmissionChargeRequest>(c => c.ChargeType == ChargeType.LabCharge && c.Amount == 900m && c.Remarks == "Lab: Fever Panel"),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenPriceLookupFails_StillReturnsSuccessWithoutPostingACharge()
    {
        _labOrderService.CreateFromAdmissionAsync(Arg.Any<CreateLabOrderFromAdmissionRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(SuccessfulLabOrder());
        var serviceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Failure("MASTERS.NOT_FOUND", "not found"));
        var request = new CreatePlaceLabOrderRequest { Lines = [new PlaceLabOrderLineRequest { ServiceId = serviceId }] };

        var result = await _sut.PlaceOrderAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _admissionChargeService.DidNotReceive().CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateAdmissionChargeRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_DelegatesToLabOrderService()
    {
        var expected = Result<IReadOnlyList<LabOrderResponse>>.Success([new LabOrderResponse { Id = Guid.NewGuid() }]);
        _labOrderService.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected.Value);
    }
}
