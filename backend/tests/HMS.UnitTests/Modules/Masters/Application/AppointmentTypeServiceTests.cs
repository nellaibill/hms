using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

public class AppointmentTypeServiceTests
{
    private readonly IAppointmentTypeRepository _repository = Substitute.For<IAppointmentTypeRepository>();
    private readonly AppointmentTypeService _sut;

    public AppointmentTypeServiceTests()
    {
        _sut = new AppointmentTypeService(_repository);
    }

    private static CreateAppointmentTypeRequest NewCreateRequest(int? priority = null) => new()
    {
        Name = "Regular (Walk-in)",
        IsActive = true,
        Priority = priority,
    };

    [Fact]
    public async Task CreateAsync_WithPriority_PersistsIt()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(priority: 1), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Priority.Should().Be(1);
        await _repository.Received(1).AddAsync(Arg.Is<AppointmentType>(a => a.Priority == 1), Arg.Any<CancellationToken>());
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
        var appointmentType = AppointmentType.Create("Regular (Walk-in)", isActive: true, priority: 5, createdBy: null);
        _repository.GetByIdAsync(appointmentType.Id, Arg.Any<CancellationToken>()).Returns(appointmentType);

        var result = await _sut.UpdateAsync(
            appointmentType.Id,
            new UpdateAppointmentTypeRequest { Name = "Regular (Walk-in)", IsActive = true, Priority = 1 },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Priority.Should().Be(1);
        appointmentType.Priority.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WithNoPriority_ClearsIt()
    {
        var appointmentType = AppointmentType.Create("Regular (Walk-in)", isActive: true, priority: 1, createdBy: null);
        _repository.GetByIdAsync(appointmentType.Id, Arg.Any<CancellationToken>()).Returns(appointmentType);

        var result = await _sut.UpdateAsync(
            appointmentType.Id,
            new UpdateAppointmentTypeRequest { Name = "Regular (Walk-in)", IsActive = true },
            actorId: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        appointmentType.Priority.Should().BeNull();
    }
}
