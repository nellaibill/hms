using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class MedicationAdministrationServiceTests
{
    private readonly IMedicationAdministrationRepository _repository = Substitute.For<IMedicationAdministrationRepository>();
    private readonly IMedicationOrderRepository _orderRepository = Substitute.For<IMedicationOrderRepository>();
    private readonly MedicationAdministrationService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();
    private readonly MedicationOrder _order;

    public MedicationAdministrationServiceTests()
    {
        _sut = new MedicationAdministrationService(_repository, _orderRepository);

        _order = MedicationOrder.Create(_admissionId, "Ceftriaxone", "1g", "IV", "BD", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);
        _orderRepository.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(_order);
    }

    [Fact]
    public async Task CreateAsync_WhenGiven_RecordsAdministrationAndReturnsSuccess()
    {
        var request = new CreateMedicationAdministrationRequest { ScheduledTime = DateTime.UtcNow, WasGiven = true, AdministeredAt = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, _order.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasGiven.Should().BeTrue();
        result.Value.AdministeredAt.Should().NotBeNull();
        await _repository.Received(1).AddAsync(Arg.Any<MedicationAdministration>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenNotGiven_RecordsAdministrationWithNullAdministeredAt()
    {
        var request = new CreateMedicationAdministrationRequest { ScheduledTime = DateTime.UtcNow, WasGiven = false, Reason = "Patient refused" };

        var result = await _sut.CreateAsync(_admissionId, _order.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasGiven.Should().BeFalse();
        result.Value.AdministeredAt.Should().BeNull();
        result.Value.Reason.Should().Be("Patient refused");
    }

    [Fact]
    public async Task CreateAsync_WhenOrderNotFound_ReturnsMedicationOrderNotFoundFailure()
    {
        var orderId = Guid.NewGuid();
        _orderRepository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((MedicationOrder?)null);
        var request = new CreateMedicationAdministrationRequest { ScheduledTime = DateTime.UtcNow, WasGiven = true };

        var result = await _sut.CreateAsync(_admissionId, orderId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.MedicationOrderNotFound);
        await _repository.DidNotReceive().AddAsync(Arg.Any<MedicationAdministration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenOrderBelongsToDifferentAdmission_ReturnsMedicationOrderNotFoundFailure()
    {
        var otherAdmissionId = Guid.NewGuid();
        var otherOrder = MedicationOrder.Create(otherAdmissionId, "Paracetamol", "500mg", "PO", "TDS", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);
        _orderRepository.GetByIdAsync(otherOrder.Id, Arg.Any<CancellationToken>()).Returns(otherOrder);
        var request = new CreateMedicationAdministrationRequest { ScheduledTime = DateTime.UtcNow, WasGiven = true };

        var result = await _sut.CreateAsync(_admissionId, otherOrder.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.MedicationOrderNotFound);
    }

    [Fact]
    public async Task GetByMedicationOrderIdAsync_ReturnsMappedAdministrations()
    {
        var administration = MedicationAdministration.Create(_order.Id, DateTime.UtcNow, true, DateTime.UtcNow, null, null, null, null);
        _repository.GetByMedicationOrderIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(new List<MedicationAdministration> { administration });

        var result = await _sut.GetByMedicationOrderIdAsync(_admissionId, _order.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.WasGiven);
    }

    [Fact]
    public async Task GetByMedicationOrderIdAsync_WhenOrderNotFound_ReturnsMedicationOrderNotFoundFailure()
    {
        var orderId = Guid.NewGuid();
        _orderRepository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((MedicationOrder?)null);

        var result = await _sut.GetByMedicationOrderIdAsync(_admissionId, orderId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.MedicationOrderNotFound);
    }
}
