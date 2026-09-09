using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class DoctorOrderServiceTests
{
    private readonly IDoctorOrderRepository _repository = Substitute.For<IDoctorOrderRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly IDiagnosticServiceService _diagnosticServiceService = Substitute.For<IDiagnosticServiceService>();
    private readonly IDiagnosticTestService _diagnosticTestService = Substitute.For<IDiagnosticTestService>();
    private readonly IConsultationTypeService _consultationTypeService = Substitute.For<IConsultationTypeService>();
    private readonly IAdmissionChargeService _admissionChargeService = Substitute.For<IAdmissionChargeService>();
    private readonly ILogger<DoctorOrderService> _logger = Substitute.For<ILogger<DoctorOrderService>>();
    private readonly DoctorOrderService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public DoctorOrderServiceTests()
    {
        _sut = new DoctorOrderService(
            _repository, _admissionRepository, _diagnosticServiceService, _diagnosticTestService, _consultationTypeService, _admissionChargeService, _logger);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);

        _admissionChargeService.CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateAdmissionChargeRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AdmissionChargeResponse>.Success(new AdmissionChargeResponse()));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PlacesOrderAndReturnsSuccess()
    {
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Radiology, Description = "Chest X-ray", OrderedAt = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(DoctorOrderStatus.Ordered);
        result.Value.Description.Should().Be("Chest X-ray");
        await _repository.Received(1).AddAsync(Arg.Any<DoctorOrder>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Diet, Description = "Diabetic diet", OrderedAt = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_ReturnsMappedOrders()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Consultation, "Cardiology review", null, DateTime.UtcNow, null, null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(new List<DoctorOrder> { order });

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(o => o.Description == "Cardiology review" && o.Status == DoctorOrderStatus.Ordered);
    }

    [Fact]
    public async Task AdvanceAsync_WithValidOrder_MovesToNextStatus()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Procedure, "Wound dressing", null, DateTime.UtcNow, null, null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.AdvanceAsync(_admissionId, order.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(DoctorOrderStatus.Accepted);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdvanceAsync_WhenOrderNotFound_ReturnsDoctorOrderNotFoundFailure()
    {
        var orderId = Guid.NewGuid();
        _repository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((DoctorOrder?)null);

        var result = await _sut.AdvanceAsync(_admissionId, orderId, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.DoctorOrderNotFound);
    }

    [Fact]
    public async Task AdvanceAsync_WhenOrderBelongsToDifferentAdmission_ReturnsDoctorOrderNotFoundFailure()
    {
        var otherAdmissionId = Guid.NewGuid();
        var order = DoctorOrder.Create(otherAdmissionId, DoctorOrderType.Blood, "2 units PRBC", null, DateTime.UtcNow, null, null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.AdvanceAsync(_admissionId, order.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.DoctorOrderNotFound);
    }

    [Fact]
    public async Task AdvanceAsync_WhenAlreadyTerminal_ReturnsInvalidTransitionFailure()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Referral, "ENT referral", null, DateTime.UtcNow, null, null);
        order.Cancel(null, null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.AdvanceAsync(_admissionId, order.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidOrderStatusTransition);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WithValidOrder_CancelsAndReturnsSuccess()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Nursing, "Repositioning schedule", null, DateTime.UtcNow, null, null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.CancelAsync(_admissionId, order.Id, new CancelDoctorOrderRequest { Reason = "No longer needed" }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(DoctorOrderStatus.Cancelled);
        result.Value.CancellationReason.Should().Be("No longer needed");
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadyTerminal_ReturnsInvalidTransitionFailure()
    {
        var order = DoctorOrder.Create(_admissionId, DoctorOrderType.Diet, "Diabetic diet", null, DateTime.UtcNow, null, null);
        order.Advance(null);
        order.Advance(null);
        order.Advance(null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.CancelAsync(_admissionId, order.Id, new CancelDoctorOrderRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.InvalidOrderStatusTransition);
    }

    [Fact]
    public async Task CreateAsync_RadiologyWithCatalogItem_PostsDoctorOrderChargeWithResolvedPrice()
    {
        var serviceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Success(new DiagnosticServiceResponse { Id = serviceId, Name = "Chest X-ray", Price = 400m }));
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Radiology, Description = "Chest X-ray", OrderedAt = DateTime.UtcNow, CatalogItemId = serviceId };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _admissionChargeService.Received(1).CreateAsync(
            _admissionId,
            Arg.Is<CreateAdmissionChargeRequest>(c => c.ChargeType == ChargeType.DoctorOrderCharge && c.Amount == 400m && c.Remarks == "Radiology: Chest X-ray"),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ProcedureWithCatalogItem_PostsDoctorOrderChargeWithResolvedPrice()
    {
        var testId = Guid.NewGuid();
        _diagnosticTestService.GetByIdAsync(testId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticTestResponse>.Success(new DiagnosticTestResponse { Id = testId, Name = "Wound Dressing", Price = 150m }));
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Procedure, Description = "Wound Dressing", OrderedAt = DateTime.UtcNow, CatalogItemId = testId };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _admissionChargeService.Received(1).CreateAsync(
            _admissionId,
            Arg.Is<CreateAdmissionChargeRequest>(c => c.ChargeType == ChargeType.DoctorOrderCharge && c.Amount == 150m && c.Remarks == "Procedure: Wound Dressing"),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ConsultationWithCatalogItem_PostsDoctorOrderChargeWithResolvedAmount()
    {
        var consultationTypeId = Guid.NewGuid();
        _consultationTypeService.GetByIdAsync(consultationTypeId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultationTypeResponse>.Success(new ConsultationTypeResponse { Id = consultationTypeId, Name = "Cardiology", Amount = 600m }));
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Consultation, Description = "Cardiology review", OrderedAt = DateTime.UtcNow, CatalogItemId = consultationTypeId };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _admissionChargeService.Received(1).CreateAsync(
            _admissionId,
            Arg.Is<CreateAdmissionChargeRequest>(c => c.ChargeType == ChargeType.DoctorOrderCharge && c.Amount == 600m && c.Remarks == "Consultation: Cardiology"),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ConsultationWithNullAmountCatalogItem_DoesNotPostChargeButStillSucceeds()
    {
        var consultationTypeId = Guid.NewGuid();
        _consultationTypeService.GetByIdAsync(consultationTypeId, Arg.Any<CancellationToken>())
            .Returns(Result<ConsultationTypeResponse>.Success(new ConsultationTypeResponse { Id = consultationTypeId, Name = "Others / On-call", Amount = null }));
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Consultation, Description = "On-call review", OrderedAt = DateTime.UtcNow, CatalogItemId = consultationTypeId };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _admissionChargeService.DidNotReceive().CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateAdmissionChargeRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutCatalogItemId_DoesNotPostCharge()
    {
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Diet, Description = "Diabetic diet", OrderedAt = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CatalogItemId.Should().BeNull();
        await _admissionChargeService.DidNotReceive().CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateAdmissionChargeRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCatalogItemCannotBeResolved_StillSucceedsWithoutPostingCharge()
    {
        var serviceId = Guid.NewGuid();
        _diagnosticServiceService.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Result<DiagnosticServiceResponse>.Failure("MASTERS.NOT_FOUND", "not found"));
        var request = new CreateDoctorOrderRequest { OrderType = DoctorOrderType.Radiology, Description = "Chest X-ray", OrderedAt = DateTime.UtcNow, CatalogItemId = serviceId };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _admissionChargeService.DidNotReceive().CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateAdmissionChargeRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
