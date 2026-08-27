using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class LeaveTypeServiceTests
{
    private readonly ILeaveTypeRepository _repository = Substitute.For<ILeaveTypeRepository>();
    private readonly LeaveTypeService _sut;

    public LeaveTypeServiceTests() => _sut = new LeaveTypeService(_repository);

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndReturnsSuccess()
    {
        var request = new CreateLeaveTypeRequest { Code = "CL", Name = "Casual Leave", MaxDaysPerYear = 12, IsPaid = true };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("CL");
        await _repository.Received(1).AddAsync(Arg.Any<LeaveType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsDuplicateCodeFailure()
    {
        _repository.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        var request = new CreateLeaveTypeRequest { Code = "CL", Name = "Casual Leave" };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.DuplicateCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveType?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateLeaveTypeRequest { Name = "X" }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var leaveType = LeaveType.Create("CL", "Casual Leave", 12, true, true, null);
        _repository.GetByIdAsync(leaveType.Id, Arg.Any<CancellationToken>()).Returns(leaveType);

        var result = await _sut.DeleteAsync(leaveType.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveType.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var leaveType = LeaveType.Create("CL", "Casual Leave", 12, true, true, null);
        _repository.GetPagedAsync(Arg.Any<LeaveTypeListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<LeaveType> { leaveType }, 1));

        var result = await _sut.GetPagedAsync(new LeaveTypeListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(l => l.Id == leaveType.Id);
        result.TotalCount.Should().Be(1);
    }
}
