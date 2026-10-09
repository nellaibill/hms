using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

public class ConsultantServiceTests
{
    private readonly IConsultantRepository _repository = Substitute.For<IConsultantRepository>();
    private readonly IDepartmentRepository _departmentRepository = Substitute.For<IDepartmentRepository>();
    private readonly IConsultationTypeRepository _consultationTypeRepository = Substitute.For<IConsultationTypeRepository>();
    private readonly IConsultantFileStorage _fileStorage = Substitute.For<IConsultantFileStorage>();
    private readonly ConsultantService _sut;

    public ConsultantServiceTests()
    {
        _sut = new ConsultantService(_repository, _departmentRepository, _consultationTypeRepository, _fileStorage);
    }

    private static CreateConsultantRequest NewCreateRequest(int? priority = null) => new()
    {
        Name = "Dr. Karthikeyan",
        IsActive = true,
        Priority = priority,
    };

    [Fact]
    public async Task CreateAsync_WithPriority_PersistsIt()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(priority: 1), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Priority.Should().Be(1);
        await _repository.Received(1).AddAsync(Arg.Is<Consultant>(c => c.Priority == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNoPriority_LeavesItNull()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Priority.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangesThePriority()
    {
        var consultant = Consultant.Create(
            "Dr. Karthikeyan",
            departmentId: null,
            specialization: null,
            isActive: true,
            priority: 5,
            availableDays: [],
            visitStartTime: null,
            visitEndTime: null,
            consultationTypes: [],
            createdBy: null);
        _repository.GetByIdAsync(consultant.Id, Arg.Any<CancellationToken>()).Returns(consultant);

        var result = await _sut.UpdateAsync(
            consultant.Id,
            new UpdateConsultantRequest { Name = "Dr. Karthikeyan", IsActive = true, Priority = 1 },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Priority.Should().Be(1);
        consultant.Priority.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WhenConsultantDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Consultant?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateConsultantRequest { Name = "Dr. Karthikeyan", IsActive = true }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task CreateAsync_WithConsultationTypeIds_PersistsAndReturnsThem()
    {
        var consultationTypeId = Guid.NewGuid();
        _consultationTypeRepository.ExistsAsync(consultationTypeId, Arg.Any<CancellationToken>()).Returns(true);

        var request = NewCreateRequest() with { ConsultationTypeCharges = [new ConsultationTypeChargeDto(consultationTypeId, null)] };
        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ConsultationTypeCharges.Should().ContainSingle().Which.ConsultationTypeId.Should().Be(consultationTypeId);
    }

    [Fact]
    public async Task CreateAsync_WithAnInvalidConsultationTypeId_ReturnsFailure()
    {
        var consultationTypeId = Guid.NewGuid();
        _consultationTypeRepository.ExistsAsync(consultationTypeId, Arg.Any<CancellationToken>()).Returns(false);

        var request = NewCreateRequest() with { ConsultationTypeCharges = [new ConsultationTypeChargeDto(consultationTypeId, null)] };
        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.InvalidReference);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Consultant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTheFullConsultationTypeSet()
    {
        var oldConsultationTypeId = Guid.NewGuid();
        var newConsultationTypeId = Guid.NewGuid();
        _consultationTypeRepository.ExistsAsync(newConsultationTypeId, Arg.Any<CancellationToken>()).Returns(true);

        var consultant = Consultant.Create(
            "Dr. Karthikeyan",
            departmentId: null,
            specialization: null,
            isActive: true,
            priority: null,
            availableDays: [],
            visitStartTime: null,
            visitEndTime: null,
            consultationTypes: [new ConsultationTypeSelection(oldConsultationTypeId, null)],
            createdBy: null);
        _repository.GetByIdAsync(consultant.Id, Arg.Any<CancellationToken>()).Returns(consultant);

        var result = await _sut.UpdateAsync(
            consultant.Id,
            new UpdateConsultantRequest { Name = "Dr. Karthikeyan", IsActive = true, ConsultationTypeCharges = [new ConsultationTypeChargeDto(newConsultationTypeId, null)] },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ConsultationTypeCharges.Should().ContainSingle().Which.ConsultationTypeId.Should().Be(newConsultationTypeId);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAConsultantChargePerConsultationType()
    {
        var consultationTypeId = Guid.NewGuid();
        _consultationTypeRepository.ExistsAsync(consultationTypeId, Arg.Any<CancellationToken>()).Returns(true);

        var consultant = Consultant.Create(
            "Dr. Karthikeyan",
            departmentId: null,
            specialization: null,
            isActive: true,
            priority: null,
            availableDays: [],
            visitStartTime: null,
            visitEndTime: null,
            consultationTypes: [new ConsultationTypeSelection(consultationTypeId, null)],
            createdBy: null);
        _repository.GetByIdAsync(consultant.Id, Arg.Any<CancellationToken>()).Returns(consultant);

        var result = await _sut.UpdateAsync(
            consultant.Id,
            new UpdateConsultantRequest { Name = "Dr. Karthikeyan", IsActive = true, ConsultationTypeCharges = [new ConsultationTypeChargeDto(consultationTypeId, 200m)] },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ConsultationTypeCharges.Should().ContainSingle().Which.ConsultantCharge.Should().Be(200m);
    }

    [Fact]
    public async Task UploadPhotoAsync_WithAValidJpeg_SetsPhotoUrl()
    {
        var consultant = Consultant.Create(
            "Dr. Karthikeyan",
            departmentId: null,
            specialization: null,
            isActive: true,
            priority: null,
            availableDays: [],
            visitStartTime: null,
            visitEndTime: null,
            consultationTypes: [],
            createdBy: null);
        _repository.GetByIdAsync(consultant.Id, Arg.Any<CancellationToken>()).Returns(consultant);
        _fileStorage.SavePhotoAsync(consultant.Id, "photo.jpg", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/consultants/" + consultant.Id + ".jpg");

        // Minimal real JPEG signature (FF D8 FF) — UploadPhotoAsync sniffs actual bytes, not
        // the filename/content-type alone.
        using var content = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46]);

        var result = await _sut.UploadPhotoAsync(
            consultant.Id, content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PhotoUrl.Should().Be("uploads/consultants/" + consultant.Id + ".jpg");
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenTheExtensionChanges_DeletesThePreviousFile()
    {
        var consultant = Consultant.Create(
            "Dr. Karthikeyan",
            departmentId: null,
            specialization: null,
            isActive: true,
            priority: null,
            availableDays: [],
            visitStartTime: null,
            visitEndTime: null,
            consultationTypes: [],
            createdBy: null);
        consultant.SetPhoto("uploads/Tenant/t/consultants/old.png", null);
        _repository.GetByIdAsync(consultant.Id, Arg.Any<CancellationToken>()).Returns(consultant);
        _fileStorage.SavePhotoAsync(consultant.Id, "photo.jpg", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/Tenant/t/consultants/new.jpg");
        using var content = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46]);

        await _sut.UploadPhotoAsync(consultant.Id, content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        await _fileStorage.Received(1).DeleteAsync("uploads/Tenant/t/consultants/old.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenConsultantDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Consultant?)null);
        using var content = new MemoryStream([0xFF, 0xD8, 0xFF]);

        var result = await _sut.UploadPhotoAsync(
            Guid.NewGuid(), content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.NotFound);
    }

    [Fact]
    public async Task UploadPhotoAsync_WithADisallowedExtension_ReturnsInvalidFile()
    {
        var consultant = Consultant.Create(
            "Dr. Karthikeyan",
            departmentId: null,
            specialization: null,
            isActive: true,
            priority: null,
            availableDays: [],
            visitStartTime: null,
            visitEndTime: null,
            consultationTypes: [],
            createdBy: null);
        _repository.GetByIdAsync(consultant.Id, Arg.Any<CancellationToken>()).Returns(consultant);
        using var content = new MemoryStream([0x25, 0x50, 0x44, 0x46]);

        var result = await _sut.UploadPhotoAsync(
            consultant.Id, content, "document.pdf", "application/pdf", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(MastersErrorCodes.InvalidFile);
    }

    [Fact]
    public async Task CreateAsync_WithASecondVisitingSlot_PersistsBothSlots()
    {
        var request = NewCreateRequest() with
        {
            VisitStartTime = new TimeOnly(10, 0),
            VisitEndTime = new TimeOnly(13, 0),
            VisitStartTime2 = new TimeOnly(16, 0),
            VisitEndTime2 = new TimeOnly(19, 0),
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitStartTime2.Should().Be(new TimeOnly(16, 0));
        result.Value!.VisitEndTime2.Should().Be(new TimeOnly(19, 0));
    }

    [Fact]
    public async Task CreateAsync_WithoutASecondVisitingSlot_LeavesItNull()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitStartTime2.Should().BeNull();
        result.Value!.VisitEndTime2.Should().BeNull();
    }

    [Fact]
    public void Create_WithOnlyHalfOfTheSecondSlot_Throws()
    {
        var act = () => Consultant.Create(
            "Dr. Karthikeyan", null, null, true, null, [],
            new TimeOnly(10, 0), new TimeOnly(13, 0), [], null,
            visitStartTime2: new TimeOnly(16, 0), visitEndTime2: null);

        act.Should().Throw<ArgumentException>().WithMessage("*Second visit*");
    }

    [Fact]
    public void Create_WithASecondSlotEndingBeforeItStarts_Throws()
    {
        var act = () => Consultant.Create(
            "Dr. Karthikeyan", null, null, true, null, [],
            new TimeOnly(10, 0), new TimeOnly(13, 0), [], null,
            visitStartTime2: new TimeOnly(19, 0), visitEndTime2: new TimeOnly(16, 0));

        act.Should().Throw<ArgumentException>().WithMessage("*Second visit*");
    }
}
