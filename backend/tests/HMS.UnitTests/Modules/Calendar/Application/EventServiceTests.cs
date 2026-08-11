using FluentAssertions;
using HMS.Modules.Calendar.Application;
using HMS.Modules.Calendar.Application.Abstractions;
using HMS.Modules.Calendar.Contracts;
using HMS.Modules.Calendar.Domain;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Calendar.Application;

public class EventServiceTests
{
    private static readonly DateTime StartDate = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndDate = new(2026, 8, 11, 17, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DepartmentId = Guid.NewGuid();

    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly EventService _sut;

    public EventServiceTests()
    {
        _sut = new EventService(_repository, _departmentService);

        // Happy-path defaults: the department exists, and no other Holiday already sits
        // on the requested date. Tests for each failure path override these per-test.
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse()));
        _repository.ExistsHolidayOnDateAsync(Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    private static CreateEventRequest NewCreateRequest(EventType eventType = EventType.Meeting, Guid? departmentId = null) => new()
    {
        Title = "Fire Drill",
        Description = "Annual fire safety drill",
        EventType = eventType,
        StartDate = StartDate,
        EndDate = EndDate,
        IsAllDay = false,
        DepartmentId = departmentId,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesEventAndReturnsSuccess()
    {
        var request = NewCreateRequest(departmentId: DepartmentId);

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Fire Drill");
        result.Value.DepartmentId.Should().Be(DepartmentId);
        await _repository.Received(1).AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNoDepartment_SkipsDepartmentCheckAndSucceeds()
    {
        var request = NewCreateRequest(departmentId: null);

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _departmentService.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDepartmentDoesNotExist_ReturnsInvalidDepartmentFailure()
    {
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Failure("HR.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(departmentId: DepartmentId), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CalendarErrorCodes.InvalidDepartment);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithHolidayOnADateThatAlreadyHasOne_ReturnsDuplicateHolidayFailure()
    {
        _repository.ExistsHolidayOnDateAsync(Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(EventType.Holiday), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CalendarErrorCodes.DuplicateHoliday);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNonHolidayEventType_NeverChecksHolidayUniqueness()
    {
        var result = await _sut.CreateAsync(NewCreateRequest(EventType.Meeting), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().ExistsHolidayOnDateAsync(Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Event?)null);

        var updateRequest = new UpdateEventRequest
        {
            Title = "Fire Drill",
            Description = "Annual fire safety drill",
            EventType = EventType.Meeting,
            StartDate = StartDate,
            EndDate = EndDate,
            IsAllDay = false,
        };

        var result = await _sut.UpdateAsync(Guid.NewGuid(), updateRequest, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CalendarErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturnsSuccess()
    {
        var calendarEvent = Event.Create("Old Title", null, EventType.Meeting, StartDate, EndDate, false, null, null);
        _repository.GetByIdAsync(calendarEvent.Id, Arg.Any<CancellationToken>()).Returns(calendarEvent);

        var updateRequest = new UpdateEventRequest
        {
            Title = "New Title",
            Description = "Updated",
            EventType = EventType.Meeting,
            StartDate = StartDate,
            EndDate = EndDate,
            IsAllDay = false,
        };

        var result = await _sut.UpdateAsync(calendarEvent.Id, updateRequest, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("New Title");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var calendarEvent = Event.Create("Meeting", null, EventType.Meeting, StartDate, EndDate, false, null, null);
        _repository.GetByIdAsync(calendarEvent.Id, Arg.Any<CancellationToken>()).Returns(calendarEvent);

        var result = await _sut.GetByIdAsync(calendarEvent.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(calendarEvent.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CalendarErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var calendarEvent = Event.Create("Meeting", null, EventType.Meeting, StartDate, EndDate, false, null, null);
        _repository.GetPagedAsync(Arg.Any<EventListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<Event> { calendarEvent }, 1));

        var result = await _sut.GetPagedAsync(new EventListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(e => e.Id == calendarEvent.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var calendarEvent = Event.Create("Meeting", null, EventType.Meeting, StartDate, EndDate, false, null, null);
        _repository.GetByIdAsync(calendarEvent.Id, Arg.Any<CancellationToken>()).Returns(calendarEvent);

        var result = await _sut.DeleteAsync(calendarEvent.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        calendarEvent.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CalendarErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetForMonthAsync_ReturnsMappedPagedResult()
    {
        var calendarEvent = Event.Create("Meeting", null, EventType.Meeting, StartDate, EndDate, false, null, null);
        _repository.GetForMonthAsync(Arg.Any<MonthlyEventQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<Event> { calendarEvent }, 1));

        var result = await _sut.GetForMonthAsync(new MonthlyEventQuery { Year = 2026, Month = 8 }, CancellationToken.None);

        result.Items.Should().ContainSingle(e => e.Id == calendarEvent.Id);
        result.TotalCount.Should().Be(1);
    }
}
