using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class AdmissionAdvanceServiceTests
{
    private readonly IAdmissionAdvanceRepository _repository = Substitute.For<IAdmissionAdvanceRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly AdmissionAdvanceService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public AdmissionAdvanceServiceTests()
    {
        _sut = new AdmissionAdvanceService(_repository, _admissionRepository);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PostsAdvanceAndReturnsSuccess()
    {
        var request = new CreateAdmissionAdvanceRequest { Amount = 5000m, Method = PaymentMethod.Cash, Remarks = "Advance at admission" };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(5000m);
        result.Value.Method.Should().Be(PaymentMethod.Cash);
        await _repository.Received(1).AddAsync(Arg.Any<AdmissionAdvance>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreateAdmissionAdvanceRequest { Amount = 1000m, Method = PaymentMethod.Upi };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
        await _repository.DidNotReceive().AddAsync(Arg.Any<AdmissionAdvance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_ReturnsMappedAdvances()
    {
        var advance = AdmissionAdvance.Create(_admissionId, 3000m, PaymentMethod.Card, "TXN123", null, null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(new List<AdmissionAdvance> { advance });

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Amount == 3000m && a.Method == PaymentMethod.Card && a.ReferenceNumber == "TXN123");
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
    }
}
