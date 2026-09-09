using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class MedicationOrderServiceTests
{
    private readonly IMedicationOrderRepository _repository = Substitute.For<IMedicationOrderRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly MedicationOrderService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public MedicationOrderServiceTests()
    {
        _sut = new MedicationOrderService(_repository, _admissionRepository);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PlacesOrderAndReturnsSuccess()
    {
        var request = new CreateMedicationOrderRequest
        {
            DrugName = "Ceftriaxone",
            Dose = "1g",
            Route = "IV",
            Frequency = "BD",
            StartDate = DateTime.UtcNow,
            OrderedAt = DateTime.UtcNow,
        };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(MedicationOrderStatus.Active);
        result.Value.DrugName.Should().Be("Ceftriaxone");
        await _repository.Received(1).AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreateMedicationOrderRequest { DrugName = "Paracetamol", Dose = "500mg", Route = "PO", Frequency = "TDS", StartDate = DateTime.UtcNow, OrderedAt = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_ReturnsMappedOrders()
    {
        var order = MedicationOrder.Create(_admissionId, "Paracetamol", "500mg", "PO", "TDS", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(new List<MedicationOrder> { order });

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(o => o.DrugName == "Paracetamol" && o.Status == MedicationOrderStatus.Active);
    }

    [Fact]
    public async Task DiscontinueAsync_WithValidOrder_DiscontinuesAndReturnsSuccess()
    {
        var order = MedicationOrder.Create(_admissionId, "Paracetamol", "500mg", "PO", "TDS", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.DiscontinueAsync(_admissionId, order.Id, new DiscontinueMedicationOrderRequest { Reason = "Fever resolved" }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(MedicationOrderStatus.Discontinued);
        result.Value.DiscontinuedReason.Should().Be("Fever resolved");
    }

    [Fact]
    public async Task DiscontinueAsync_WhenOrderNotFound_ReturnsMedicationOrderNotFoundFailure()
    {
        var orderId = Guid.NewGuid();
        _repository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((MedicationOrder?)null);

        var result = await _sut.DiscontinueAsync(_admissionId, orderId, new DiscontinueMedicationOrderRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.MedicationOrderNotFound);
    }

    [Fact]
    public async Task DiscontinueAsync_WhenOrderBelongsToDifferentAdmission_ReturnsMedicationOrderNotFoundFailure()
    {
        var otherAdmissionId = Guid.NewGuid();
        var order = MedicationOrder.Create(otherAdmissionId, "Paracetamol", "500mg", "PO", "TDS", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.DiscontinueAsync(_admissionId, order.Id, new DiscontinueMedicationOrderRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.MedicationOrderNotFound);
    }

    [Fact]
    public async Task DiscontinueAsync_WhenAlreadyDiscontinued_ReturnsAlreadyDiscontinuedFailure()
    {
        var order = MedicationOrder.Create(_admissionId, "Paracetamol", "500mg", "PO", "TDS", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);
        order.Discontinue(null, null);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.DiscontinueAsync(_admissionId, order.Id, new DiscontinueMedicationOrderRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.MedicationOrderAlreadyDiscontinued);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
