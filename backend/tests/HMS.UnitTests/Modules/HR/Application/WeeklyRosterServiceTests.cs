using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class WeeklyRosterServiceTests
{
    private static readonly DateOnly WeekStartDate = new(2026, 8, 3);

    private readonly IWeeklyRosterRepository _repository = Substitute.For<IWeeklyRosterRepository>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly WeeklyRosterService _sut;

    public WeeklyRosterServiceTests()
    {
        // Happy-path defaults: a valid, non-duplicate department/week. Tests for the
        // "invalid department"/"duplicate roster" failure paths override these per-test.
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<DepartmentResponse>.Success(new DepartmentResponse()));
        _repository.ExistsForDepartmentAndWeekAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        _sut = new WeeklyRosterService(_repository, _departmentService);
    }

    [Fact]
    public async Task CreateAsync_CreatesRosterAndReturnsSuccess()
    {
        var request = new CreateWeeklyRosterRequest { WeekStartDate = WeekStartDate, DepartmentId = Guid.NewGuid() };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WeekStartDate.Should().Be(WeekStartDate);
        await _repository.Received(1).AddAsync(Arg.Any<WeeklyRoster>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturnsSuccess()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, Guid.NewGuid(), false, null, null);
        _repository.GetByIdAsync(roster.Id, Arg.Any<CancellationToken>()).Returns(roster);

        var newDepartmentId = Guid.NewGuid();
        var request = new UpdateWeeklyRosterRequest
        {
            WeekStartDate = new DateOnly(2026, 8, 10),
            DepartmentId = newDepartmentId,
            Published = true,
            PublishedDate = new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc),
        };

        var result = await _sut.UpdateAsync(roster.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DepartmentId.Should().Be(newDepartmentId);
        result.Value.Published.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WeeklyRoster?)null);

        var request = new UpdateWeeklyRosterRequest { WeekStartDate = WeekStartDate, DepartmentId = Guid.NewGuid() };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, Guid.NewGuid(), false, null, null);
        _repository.GetByIdAsync(roster.Id, Arg.Any<CancellationToken>()).Returns(roster);

        var result = await _sut.GetByIdAsync(roster.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(roster.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WeeklyRoster?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, Guid.NewGuid(), false, null, null);
        _repository.GetPagedAsync(Arg.Any<WeeklyRosterListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<WeeklyRoster> { roster }, 1));

        var result = await _sut.GetPagedAsync(new WeeklyRosterListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(w => w.Id == roster.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, Guid.NewGuid(), false, null, null);
        _repository.GetByIdAsync(roster.Id, Arg.Any<CancellationToken>()).Returns(roster);

        var result = await _sut.DeleteAsync(roster.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        roster.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WeeklyRoster?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task PublishAsync_WhenFound_PublishesAndReturnsSuccess()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, Guid.NewGuid(), false, null, null);
        _repository.GetByIdAsync(roster.Id, Arg.Any<CancellationToken>()).Returns(roster);

        var result = await _sut.PublishAsync(roster.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Published.Should().BeTrue();
        result.Value.PublishedDate.Should().NotBeNull();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WeeklyRoster?)null);

        var result = await _sut.PublishAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task CopyAsync_WhenFound_CreatesNewUnpublishedRosterForTargetWeek()
    {
        var departmentId = Guid.NewGuid();
        var source = WeeklyRoster.Create(WeekStartDate, departmentId, true, DateTime.UtcNow, null);
        _repository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);

        var targetWeekStart = new DateOnly(2026, 9, 1);
        var request = new CopyWeeklyRosterRequest { TargetWeekStartDate = targetWeekStart };

        var result = await _sut.CopyAsync(source.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().NotBe(source.Id);
        result.Value.WeekStartDate.Should().Be(targetWeekStart);
        result.Value.DepartmentId.Should().Be(departmentId);
        result.Value.Published.Should().BeFalse();
        result.Value.PublishedDate.Should().BeNull();
        await _repository.Received(1).AddAsync(Arg.Any<WeeklyRoster>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CopyAsync_WhenSourceNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WeeklyRoster?)null);

        var request = new CopyWeeklyRosterRequest { TargetWeekStartDate = new DateOnly(2026, 9, 1) };
        var result = await _sut.CopyAsync(Guid.NewGuid(), request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
        await _repository.DidNotReceive().AddAsync(Arg.Any<WeeklyRoster>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetForMonthAsync_ReturnsMappedPagedResult()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, Guid.NewGuid(), false, null, null);
        _repository.GetForMonthAsync(Arg.Any<MonthlyWeeklyRosterQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<WeeklyRoster> { roster }, 1));

        var query = new MonthlyWeeklyRosterQuery { Year = 2026, Month = 8 };
        var result = await _sut.GetForMonthAsync(query, CancellationToken.None);

        result.Items.Should().ContainSingle(w => w.Id == roster.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WhenDepartmentDoesNotExist_ReturnsInvalidDepartmentFailure()
    {
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<DepartmentResponse>.Failure("MASTERS.NOT_FOUND", "not found"));

        var request = new CreateWeeklyRosterRequest { WeekStartDate = WeekStartDate, DepartmentId = Guid.NewGuid() };
        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidDepartment);
        await _repository.DidNotReceive().AddAsync(Arg.Any<WeeklyRoster>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRosterAlreadyExistsForDepartmentAndWeek_ReturnsDuplicateRosterFailure()
    {
        _repository.ExistsForDepartmentAndWeekAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var request = new CreateWeeklyRosterRequest { WeekStartDate = WeekStartDate, DepartmentId = Guid.NewGuid() };
        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.DuplicateRoster);
        await _repository.DidNotReceive().AddAsync(Arg.Any<WeeklyRoster>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CopyAsync_WhenTargetWeekAlreadyHasARoster_ReturnsDuplicateRosterFailure()
    {
        var source = WeeklyRoster.Create(WeekStartDate, Guid.NewGuid(), false, null, null);
        _repository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        _repository.ExistsForDepartmentAndWeekAsync(source.DepartmentId, Arg.Any<DateOnly>(), null, Arg.Any<CancellationToken>()).Returns(true);

        var request = new CopyWeeklyRosterRequest { TargetWeekStartDate = new DateOnly(2026, 9, 1) };
        var result = await _sut.CopyAsync(source.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.DuplicateRoster);
        await _repository.DidNotReceive().AddAsync(Arg.Any<WeeklyRoster>(), Arg.Any<CancellationToken>());
    }
}
