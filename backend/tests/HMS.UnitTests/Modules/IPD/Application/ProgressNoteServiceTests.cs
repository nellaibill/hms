using FluentAssertions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Application;

public class ProgressNoteServiceTests
{
    private readonly IProgressNoteRepository _repository = Substitute.For<IProgressNoteRepository>();
    private readonly IAdmissionRepository _admissionRepository = Substitute.For<IAdmissionRepository>();
    private readonly ProgressNoteService _sut;
    private readonly Guid _admissionId = Guid.NewGuid();

    public ProgressNoteServiceTests()
    {
        _sut = new ProgressNoteService(_repository, _admissionRepository);

        var admission = Admission.Create("ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, AdmissionType.Elective, "Observation", null);
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(admission);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_RecordsNoteAndReturnsSuccess()
    {
        var request = new CreateProgressNoteRequest
        {
            NoteDateTime = DateTime.UtcNow,
            ClinicalCondition = "Stable",
            Assessment = "Improving post-op",
            Plan = "Continue current medication",
        };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ClinicalCondition.Should().Be("Stable");
        result.Value.Plan.Should().Be("Continue current medication");
        await _repository.Received(1).AddAsync(Arg.Any<ProgressNote>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAdmissionDoesNotExist_ReturnsNotFoundFailure()
    {
        _admissionRepository.GetByIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns((Admission?)null);
        var request = new CreateProgressNoteRequest { NoteDateTime = DateTime.UtcNow };

        var result = await _sut.CreateAsync(_admissionId, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(IPDErrorCodes.NotFound);
        await _repository.DidNotReceive().AddAsync(Arg.Any<ProgressNote>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAdmissionIdAsync_ReturnsMappedNotes()
    {
        var note = ProgressNote.Create(_admissionId, DateTime.UtcNow, "Alert", "Improving", "Post-op recovery", "Stable vitals", "Continue monitoring", "Mobilize as tolerated", null, null);
        _repository.GetByAdmissionIdAsync(_admissionId, Arg.Any<CancellationToken>()).Returns(new List<ProgressNote> { note });

        var result = await _sut.GetByAdmissionIdAsync(_admissionId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(n => n.ClinicalCondition == "Alert" && n.Plan == "Continue monitoring");
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
