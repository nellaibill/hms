using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class VitalsReadingServiceTests
{
    private readonly IVitalsReadingRepository _repository = Substitute.For<IVitalsReadingRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly VitalsReadingService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public VitalsReadingServiceTests()
    {
        _sut = new VitalsReadingService(_repository, _admissionRepository);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_RecordsReadingAndReturnsSuccess()
    {
        var request = new CreateVitalsReadingRequest
        {
            RecordedAt = DateTime.UtcNow,
            TemperatureF = 98.6m,
            PulseRate = 78,
            RespiratoryRate = 18,
            BloodPressureSystolic = 120,
            BloodPressureDiastolic = 80,
            SpO2Percent = 98,
        };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PulseRate.Should().Be(78);
        result.Value.BloodPressureSystolic.Should().Be(120);
        await _repository.Received(1).AddAsync(Arg.Any<VitalsReading>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreateVitalsReadingRequest { RecordedAt = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
        await _repository.DidNotReceive().AddAsync(Arg.Any<VitalsReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_ReturnsMappedReadings()
    {
        var reading = VitalsReading.Create(_admissionId, DateTime.UtcNow, 99.1m, 88, 20, 130, 85, 97, 70m, 170m, 3, 110m, null, "slightly elevated", null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(new List<VitalsReading> { reading });

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.PulseRate == 88 && r.PainScore == 3);
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
