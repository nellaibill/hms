using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class ShiftServiceTests
{
    private static readonly TimeOnly Start = new(8, 0);
    private static readonly TimeOnly End = new(16, 0);

    private readonly IShiftRepository _repository = Substitute.For<IShiftRepository>();
    private readonly ShiftService _sut;

    public ShiftServiceTests()
    {
        _sut = new ShiftService(_repository);
    }

    [Fact]
    public async Task CreateAsync_WithNewCode_CreatesShiftAndReturnsSuccess()
    {
        _repository.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var request = new CreateShiftRequest
        {
            Code = "morning",
            Name = "Morning Shift",
            StartTime = Start,
            EndTime = End,
            BreakMinutes = 30,
            GraceMinutes = 10,
            IsNightShift = false,
            IsActive = true,
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("MORNING");
        result.Value.Name.Should().Be("Morning Shift");
        await _repository.Received(1).AddAsync(Arg.Any<Shift>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsDuplicateCodeFailureAndSavesNothing()
    {
        _repository.ExistsByCodeAsync("MORNING", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var request = new CreateShiftRequest { Code = "morning", Name = "Morning Shift", StartTime = Start, EndTime = End };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.DuplicateCode);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Shift>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturnsSuccess()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 30, 10, false, true, null);
        _repository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);

        var request = new UpdateShiftRequest
        {
            Name = "Morning (Revised)",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            BreakMinutes = 45,
            GraceMinutes = 15,
            IsNightShift = false,
            IsActive = false,
        };

        var result = await _sut.UpdateAsync(shift.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Morning (Revised)");
        result.Value.IsActive.Should().BeFalse();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateShiftRequest { Name = "X", StartTime = Start, EndTime = End }, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 0, 0, false, true, null);
        _repository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);

        var result = await _sut.GetByIdAsync(shift.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(shift.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 0, 0, false, true, null);
        _repository.GetPagedAsync(Arg.Any<ShiftListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<Shift> { shift }, 1));

        var result = await _sut.GetPagedAsync(new ShiftListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(s => s.Id == shift.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 0, 0, false, true, null);
        _repository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);

        var result = await _sut.DeleteAsync(shift.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        shift.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }
}
