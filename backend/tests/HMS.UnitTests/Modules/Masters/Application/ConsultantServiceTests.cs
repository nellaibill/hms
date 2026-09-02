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
    private readonly ConsultantService _sut;

    public ConsultantServiceTests()
    {
        _sut = new ConsultantService(_repository, _departmentRepository);
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
        var consultant = Consultant.Create("Dr. Karthikeyan", departmentId: null, specialization: null, isActive: true, priority: 5, createdBy: null);
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
}
