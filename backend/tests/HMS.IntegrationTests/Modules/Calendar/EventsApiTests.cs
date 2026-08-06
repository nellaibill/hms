using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HMS.IntegrationTests.Modules.Identity;
using HMS.Modules.Calendar.Contracts;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.IntegrationTests.Modules.Calendar;

/// <summary>
/// Black-box tests against the real HTTP surface, covering the CRUD + soft-delete flow
/// plus the Holiday-uniqueness/Department-existence business rules for Calendar Phase 1,
/// and the monthly view for Phase 2. Reuses UsersApiFactory (boots the full HMS.Api host,
/// including this module) rather than a Calendar-specific factory — no other module has
/// its own either. Requires Docker (Testcontainers) to run.
/// </summary>
public class EventsApiTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public EventsApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object NewEventPayload(string? title = null, string eventType = "Meeting", DateTime? startDate = null, DateTime? endDate = null) => new
    {
        title = title ?? $"Event-{Guid.NewGuid():N}",
        description = "Test event",
        eventType,
        startDate = startDate ?? new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc),
        endDate = endDate ?? new DateTime(2026, 8, 11, 17, 0, 0, DateTimeKind.Utc),
        isAllDay = false,
    };

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedEvent()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EventResponse>>();
        created!.Data!.Id.Should().NotBeEmpty();

        var getResponse = await _client.GetAsync($"/api/v1/events/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithMissingTitle_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/events",
            new { title = "", eventType = "Meeting", startDate = "2026-08-11T09:00:00Z", endDate = "2026-08-11T17:00:00Z" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().Contain(e => e.Field == "Title");
    }

    [Fact]
    public async Task Create_WithStartDateAfterEndDate_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/events",
            NewEventPayload(
                startDate: new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
                endDate: new DateTime(2026, 8, 10, 17, 0, 0, DateTimeKind.Utc)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().Contain(e => e.Field == "EndDate");
    }

    [Fact]
    public async Task Create_WithUnknownDepartmentId_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/events", new
        {
            title = $"Event-{Guid.NewGuid():N}",
            eventType = "Meeting",
            startDate = "2026-08-11T09:00:00Z",
            endDate = "2026-08-11T17:00:00Z",
            departmentId = Guid.NewGuid(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("CALENDAR.INVALID_DEPARTMENT");
    }

    [Fact]
    public async Task Create_TwoHolidaysOnTheSameDate_SecondReturnsBadRequest()
    {
        var holidayDate = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc);

        var first = await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload("Christmas", "Holiday", holidayDate, holidayDate));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload("Christmas Duplicate", "Holiday", holidayDate, holidayDate));
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await second.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("CALENDAR.DUPLICATE_HOLIDAY");
    }

    [Fact]
    public async Task Create_HolidayAndMeetingOnTheSameDate_BothSucceed()
    {
        var date = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        var holiday = await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload("All Saints' Day", "Holiday", date, date));
        holiday.StatusCode.Should().Be(HttpStatusCode.Created);

        var meeting = await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload("Budget Meeting", "Meeting", date, date));
        meeting.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/events/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EventResponse>>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/events/{created!.Data!.Id}", new
        {
            title = "Updated Title",
            description = "Updated description",
            eventType = "Training",
            startDate = "2026-08-12T09:00:00Z",
            endDate = "2026-08-12T17:00:00Z",
            isAllDay = true,
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<EventResponse>>();
        updated!.Data!.Title.Should().Be("Updated Title");
        updated.Data.EventType.Should().Be(EventType.Training);
        updated.Data.IsAllDay.Should().BeTrue();
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/events/{Guid.NewGuid()}", NewEventPayload());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EventResponse>>();
        var id = created!.Data!.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/events/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/events/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/events/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ReturnsEnvelopeWithPaginationMeta()
    {
        await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload());

        var response = await _client.GetAsync("/api/v1/events?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<EventResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaged_FilteredByEventType_OnlyReturnsMatchingType()
    {
        var uniqueTitle = $"Training-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/events", NewEventPayload(uniqueTitle, "Training"));

        var response = await _client.GetAsync("/api/v1/events?eventType=Training&search=" + uniqueTitle);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<EventResponse>>>();
        body!.Data.Should().ContainSingle(e => e.Title == uniqueTitle && e.EventType == EventType.Training);
    }

    [Fact]
    public async Task GetForMonth_ReturnsEventsIntersectingTheGivenMonth()
    {
        var uniqueTitle = $"October-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync(
            "/api/v1/events",
            NewEventPayload(uniqueTitle, "Meeting", new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 15, 17, 0, 0, DateTimeKind.Utc)));

        var response = await _client.GetAsync("/api/v1/events/month?year=2026&month=10&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<EventResponse>>>();
        body!.Data.Should().Contain(e => e.Title == uniqueTitle);
    }

    [Fact]
    public async Task GetForMonth_WithInvalidMonth_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/events/month?year=2026&month=13");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BulkCreate_WithAllValidEvents_CreatesAllAndReturnsSuccessForEach()
    {
        var payload = new
        {
            events = new[]
            {
                NewEventPayload(eventType: "Meeting"),
                NewEventPayload(eventType: "Training"),
                NewEventPayload(eventType: "Maintenance"),
            },
        };

        var response = await _client.PostAsJsonAsync("/api/v1/events/bulk", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BulkCreateEventsResponse>>();
        body!.Data!.SucceededCount.Should().Be(3);
        body.Data.FailedCount.Should().Be(0);
        body.Data.Results.Should().OnlyContain(r => r.Success && r.Event != null);
        body.Data.Results.Select(r => r.Index).Should().Equal(0, 1, 2);
    }

    [Fact]
    public async Task BulkCreate_WithEmptyArray_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/events/bulk", new { events = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BulkCreate_WithOneInvalidItem_ReportsThatItemAsFailedAndStillCreatesTheRest()
    {
        var payload = new
        {
            events = new object[]
            {
                NewEventPayload(eventType: "Meeting"),
                new { title = "", eventType = "Meeting", startDate = "2026-08-11T09:00:00Z", endDate = "2026-08-11T17:00:00Z" },
                NewEventPayload(eventType: "Training"),
            },
        };

        var response = await _client.PostAsJsonAsync("/api/v1/events/bulk", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BulkCreateEventsResponse>>();
        body!.Data!.SucceededCount.Should().Be(2);
        body.Data.FailedCount.Should().Be(1);
        body.Data.Results.Single(r => r.Index == 1).Success.Should().BeFalse();
        body.Data.Results.Single(r => r.Index == 0).Success.Should().BeTrue();
        body.Data.Results.Single(r => r.Index == 2).Success.Should().BeTrue();
    }

    [Fact]
    public async Task BulkCreate_WithTwoHolidaysOnTheSameDateInTheSameBatch_SecondIsReportedAsFailed()
    {
        var holidayDate = new DateTime(2027, 1, 26, 0, 0, 0, DateTimeKind.Utc);

        var payload = new
        {
            events = new[]
            {
                NewEventPayload("Republic Day", "Holiday", holidayDate, holidayDate),
                NewEventPayload("Republic Day Duplicate", "Holiday", holidayDate, holidayDate),
            },
        };

        var response = await _client.PostAsJsonAsync("/api/v1/events/bulk", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BulkCreateEventsResponse>>();
        body!.Data!.SucceededCount.Should().Be(1);
        body.Data.FailedCount.Should().Be(1);
        body.Data.Results.Single(r => r.Index == 0).Success.Should().BeTrue();
        var second = body.Data.Results.Single(r => r.Index == 1);
        second.Success.Should().BeFalse();
        second.ErrorCode.Should().Be("CALENDAR.DUPLICATE_HOLIDAY");
    }

    [Fact]
    public async Task BulkCreate_WithUnknownDepartmentIdOnOneItem_ReportsThatItemAsFailed()
    {
        var payload = new
        {
            events = new object[]
            {
                NewEventPayload(eventType: "Meeting"),
                new
                {
                    title = $"Event-{Guid.NewGuid():N}",
                    eventType = "Meeting",
                    startDate = "2026-08-11T09:00:00Z",
                    endDate = "2026-08-11T17:00:00Z",
                    departmentId = Guid.NewGuid(),
                },
            },
        };

        var response = await _client.PostAsJsonAsync("/api/v1/events/bulk", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BulkCreateEventsResponse>>();
        body!.Data!.FailedCount.Should().Be(1);
        body.Data.Results.Single(r => r.Index == 1).ErrorCode.Should().Be("CALENDAR.INVALID_DEPARTMENT");
    }
}
