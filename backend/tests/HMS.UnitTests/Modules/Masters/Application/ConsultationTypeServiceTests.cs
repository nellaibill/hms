using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

public class ConsultationTypeServiceTests
{
    private readonly IConsultationTypeRepository _repository = Substitute.For<IConsultationTypeRepository>();
    private readonly ConsultationTypeService _sut;

    public ConsultationTypeServiceTests()
    {
        _sut = new ConsultationTypeService(_repository);
    }

    [Fact]
    public async Task CreateAsync_WithShortName_PersistsItTrimmed()
    {
        var result = await _sut.CreateAsync(
            new CreateConsultationTypeRequest { Name = "Doctor's Consultation (In-house) - Regular", ShortName = "  Regular ", Amount = 300m },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShortName.Should().Be("Regular");
        await _repository.Received(1).AddAsync(Arg.Is<ConsultationType>(c => c.ShortName == "Regular"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithBlankShortName_LeavesItNull(string? shortName)
    {
        var result = await _sut.CreateAsync(
            new CreateConsultationTypeRequest { Name = "Emergency / Casualty Doctor's Consultation", ShortName = shortName },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShortName.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheShortName()
    {
        var consultationType = ConsultationType.Create("Doctor's Consultation (In-house) - Priority", shortName: null, amount: 500m, isActive: true, createdBy: null);
        _repository.GetByIdAsync(consultationType.Id, Arg.Any<CancellationToken>()).Returns(consultationType);

        var result = await _sut.UpdateAsync(
            consultationType.Id,
            new UpdateConsultationTypeRequest { Name = "Doctor's Consultation (In-house) - Priority", ShortName = "Priority", Amount = 500m },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShortName.Should().Be("Priority");
        consultationType.ShortName.Should().Be("Priority");
    }

    [Fact]
    public async Task UpdateAsync_WithBlankShortName_ClearsIt()
    {
        var consultationType = ConsultationType.Create("Doctor's Consultation (In-house) - Priority", shortName: "Priority", amount: 500m, isActive: true, createdBy: null);
        _repository.GetByIdAsync(consultationType.Id, Arg.Any<CancellationToken>()).Returns(consultationType);

        var result = await _sut.UpdateAsync(
            consultationType.Id,
            new UpdateConsultationTypeRequest { Name = "Doctor's Consultation (In-house) - Priority", ShortName = " ", Amount = 500m },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        consultationType.ShortName.Should().BeNull();
    }
}
