using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class NursingAssessmentServiceTests
{
    private readonly INursingAssessmentRepository _repository = Substitute.For<INursingAssessmentRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly NursingAssessmentService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public NursingAssessmentServiceTests()
    {
        _sut = new NursingAssessmentService(_repository, _admissionRepository);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_RecordsAssessmentAndReturnsSuccess()
    {
        var request = new CreateNursingAssessmentRequest
        {
            AssessedAt = DateTime.UtcNow,
            GeneralCondition = "Comfortable",
            ConsciousnessLevel = "Alert",
            Mobility = "Independent",
            FallRisk = "Low",
            PainScore = 1,
        };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GeneralCondition.Should().Be("Comfortable");
        result.Value.PainScore.Should().Be(1);
        await _repository.Received(1).AddAsync(Arg.Any<NursingAssessment>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreateNursingAssessmentRequest { AssessedAt = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
        await _repository.DidNotReceive().AddAsync(Arg.Any<NursingAssessment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_ReturnsMappedAssessments()
    {
        var assessment = NursingAssessment.Create(_admissionId, DateTime.UtcNow, "Stable", "Alert", "Assisted", "Normal", "Medium", "Low", "Intact", 2, "Resting comfortably", null, null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(new List<NursingAssessment> { assessment });

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.FallRisk == "Medium" && a.PainScore == 2);
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
