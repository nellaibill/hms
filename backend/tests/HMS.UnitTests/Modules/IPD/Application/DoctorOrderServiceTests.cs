using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class DoctorOrderServiceTests
{
    private readonly IDoctorOrderRepository _repository = Substitute.For<IDoctorOrderRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly DoctorOrderService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public DoctorOrderServiceTests()
    {
        _sut = new DoctorOrderService(_repository, _admissionRepository);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
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
}
