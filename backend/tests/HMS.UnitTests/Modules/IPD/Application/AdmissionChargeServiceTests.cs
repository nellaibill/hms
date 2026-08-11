using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class AdmissionChargeServiceTests
{
    private readonly IAdmissionChargeRepository _repository = Substitute.For<IAdmissionChargeRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly AdmissionChargeService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public AdmissionChargeServiceTests()
    {
        _sut = new AdmissionChargeService(_repository, _admissionRepository);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PostsChargeAndReturnsSuccess()
    {
        var request = new CreateAdmissionChargeRequest { ChargeType = ChargeType.BedCharge, Amount = 1500m, Remarks = "ICU bed, day 1" };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChargeType.Should().Be(ChargeType.BedCharge);
        result.Value.Amount.Should().Be(1500m);
        await _repository.Received(1).AddAsync(Arg.Any<AdmissionCharge>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreateAdmissionChargeRequest { ChargeType = ChargeType.NursingCharge, Amount = 500m };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
        await _repository.DidNotReceive().AddAsync(Arg.Any<AdmissionCharge>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_ReturnsMappedCharges()
    {
        var charge = AdmissionCharge.Create(_admissionId, ChargeType.AdmissionCharge, 2000m, null, null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(new List<AdmissionCharge> { charge });

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(c => c.Amount == 2000m && c.ChargeType == ChargeType.AdmissionCharge);
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
