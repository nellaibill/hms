using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HMS.IntegrationTests.Modules.Identity;
using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.IntegrationTests.Modules.HR;

/// <summary>
/// Black-box tests against the real HTTP surface, covering the CRUD + soft-delete flow for
/// Weekly Roster (Phase 3). Reuses UsersApiFactory, same as ShiftsApiTests. Requires Docker
/// (Testcontainers) to run.
/// </summary>
public class WeeklyRostersApiTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public WeeklyRostersApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object NewRosterPayload() => new
    {
        weekStartDate = "2026-08-03",
        departmentId = Guid.NewGuid(),
        published = false,
    };

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedRoster()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();
        created!.Data!.Id.Should().NotBeEmpty();
        created.Data.Published.Should().BeFalse();

        var getResponse = await _client.GetAsync($"/api/v1/weekly-rosters/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithMissingDepartmentId_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", new { weekStartDate = "2026-08-03" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().NotBeNullOrEmpty();
        error.ValidationErrors!.Should().Contain(e => e.Field == "DepartmentId");
    }

    [Fact]
    public async Task Create_WithMissingWeekStartDate_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", new { departmentId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors!.Should().Contain(e => e.Field == "WeekStartDate");
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/weekly-rosters/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesFieldsIncludingPublishState()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/weekly-rosters/{created!.Data!.Id}", new
        {
            weekStartDate = "2026-08-10",
            departmentId = created.Data.DepartmentId,
            published = true,
            publishedDate = "2026-08-09T00:00:00Z",
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();
        updated!.Data!.Published.Should().BeTrue();
        updated.Data.PublishedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/weekly-rosters/{Guid.NewGuid()}", NewRosterPayload());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();
        var id = created!.Data!.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/weekly-rosters/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/weekly-rosters/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/weekly-rosters/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ReturnsEnvelopeWithPaginationMeta()
    {
        await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());

        var response = await _client.GetAsync("/api/v1/weekly-rosters?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<WeeklyRosterResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }

    [Fact]
    public async Task Publish_SetsPublishedAndPublishedDate()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        var publishResponse = await _client.PostAsync($"/api/v1/weekly-rosters/{created!.Data!.Id}/publish", null);

        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await publishResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();
        published!.Data!.Published.Should().BeTrue();
        published.Data.PublishedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Publish_CalledTwice_IsIdempotent()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        var first = await _client.PostAsync($"/api/v1/weekly-rosters/{created!.Data!.Id}/publish", null);
        var firstBody = await first.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        var second = await _client.PostAsync($"/api/v1/weekly-rosters/{created.Data.Id}/publish", null);
        var secondBody = await second.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        secondBody!.Data!.PublishedDate.Should().Be(firstBody!.Data!.PublishedDate);
    }

    [Fact]
    public async Task Publish_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/v1/weekly-rosters/{Guid.NewGuid()}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Copy_CreatesNewUnpublishedRosterForTargetWeek_AndDoesNotCopyPublishState()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();
        await _client.PostAsync($"/api/v1/weekly-rosters/{created!.Data!.Id}/publish", null);

        var copyResponse = await _client.PostAsJsonAsync($"/api/v1/weekly-rosters/{created.Data.Id}/copy", new
        {
            targetWeekStartDate = "2026-09-01",
        });

        copyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var copy = await copyResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();
        copy!.Data!.Id.Should().NotBe(created.Data.Id);
        copy.Data.WeekStartDate.Should().Be(new DateOnly(2026, 9, 1));
        copy.Data.DepartmentId.Should().Be(created.Data.DepartmentId);
        copy.Data.Published.Should().BeFalse();
        copy.Data.PublishedDate.Should().BeNull();
    }

    [Fact]
    public async Task Copy_WithMissingTargetWeekStartDate_ReturnsBadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", NewRosterPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        var response = await _client.PostAsJsonAsync($"/api/v1/weekly-rosters/{created!.Data!.Id}/copy", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Copy_WhenSourceNotFound_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/weekly-rosters/{Guid.NewGuid()}/copy", new
        {
            targetWeekStartDate = "2026-09-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMonthly_ReturnsOnlyRostersWhoseWeekStartDateFallsInTheGivenMonth()
    {
        var departmentId = Guid.NewGuid();
        var inMonthResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", new
        {
            weekStartDate = "2026-10-05",
            departmentId,
            published = false,
        });
        var inMonth = await inMonthResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        var outOfMonthResponse = await _client.PostAsJsonAsync("/api/v1/weekly-rosters", new
        {
            weekStartDate = "2026-11-02",
            departmentId,
            published = false,
        });
        var outOfMonth = await outOfMonthResponse.Content.ReadFromJsonAsync<ApiResponse<WeeklyRosterResponse>>();

        var response = await _client.GetAsync("/api/v1/weekly-rosters/monthly?year=2026&month=10&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<WeeklyRosterResponse>>>();
        body!.Data.Should().Contain(w => w.Id == inMonth!.Data!.Id);
        body.Data.Should().NotContain(w => w.Id == outOfMonth!.Data!.Id);
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task GetMonthly_WithInvalidMonth_ReturnsBadRequest(int year, int month)
    {
        var response = await _client.GetAsync($"/api/v1/weekly-rosters/monthly?year={year}&month={month}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
