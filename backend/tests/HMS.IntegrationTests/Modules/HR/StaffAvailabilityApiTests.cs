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
/// Staff Availability (Phase 4). Reuses UsersApiFactory, same as ShiftsApiTests. Requires
/// Docker (Testcontainers) to run.
/// </summary>
public class StaffAvailabilityApiTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public StaffAvailabilityApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object NewAvailabilityPayload() => new
    {
        staffId = Guid.NewGuid(),
        startDate = "2026-08-03",
        endDate = "2026-08-10",
        availabilityStatus = "Unavailable",
        reason = "Conference",
    };

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedAvailability()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/staff-availability", NewAvailabilityPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<StaffAvailabilityResponse>>();
        created!.Data!.Id.Should().NotBeEmpty();
        created.Data.AvailabilityStatus.Should().Be(AvailabilityStatus.Unavailable);
        created.Data.Reason.Should().Be("Conference");

        var getResponse = await _client.GetAsync($"/api/v1/staff-availability/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithMissingStaffId_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/staff-availability",
            new { startDate = "2026-08-03", endDate = "2026-08-10", availabilityStatus = "Available" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().NotBeNullOrEmpty();
        error.ValidationErrors!.Should().Contain(e => e.Field == "StaffId");
    }

    [Fact]
    public async Task Create_WithMissingAvailabilityStatus_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/staff-availability",
            new { staffId = Guid.NewGuid(), startDate = "2026-08-03", endDate = "2026-08-10" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors!.Should().Contain(e => e.Field == "AvailabilityStatus");
    }

    [Fact]
    public async Task Create_WithoutReason_Succeeds_ReasonIsOptional()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/staff-availability", new
        {
            staffId = Guid.NewGuid(),
            startDate = "2026-08-03",
            endDate = "2026-08-10",
            availabilityStatus = "Available",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ApiResponse<StaffAvailabilityResponse>>();
        created!.Data!.Reason.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/staff-availability/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/staff-availability", NewAvailabilityPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<StaffAvailabilityResponse>>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/staff-availability/{created!.Data!.Id}", new
        {
            staffId = created.Data.StaffId,
            startDate = "2026-09-01",
            endDate = "2026-09-05",
            availabilityStatus = "Available",
            reason = (string?)null,
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<StaffAvailabilityResponse>>();
        updated!.Data!.AvailabilityStatus.Should().Be(AvailabilityStatus.Available);
        updated.Data.Reason.Should().BeNull();
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/staff-availability/{Guid.NewGuid()}", NewAvailabilityPayload());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/staff-availability", NewAvailabilityPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<StaffAvailabilityResponse>>();
        var id = created!.Data!.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/staff-availability/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/staff-availability/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/staff-availability/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ReturnsEnvelopeWithPaginationMeta()
    {
        await _client.PostAsJsonAsync("/api/v1/staff-availability", NewAvailabilityPayload());

        var response = await _client.GetAsync("/api/v1/staff-availability?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<StaffAvailabilityResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }
}
