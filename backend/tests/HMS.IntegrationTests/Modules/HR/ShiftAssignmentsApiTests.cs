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
/// Shift Assignment (Phase 2). Reuses UsersApiFactory, same as ShiftsApiTests.
/// Requires Docker (Testcontainers) to run.
/// </summary>
public class ShiftAssignmentsApiTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public ShiftAssignmentsApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // CreateShiftAssignmentRequest.ShiftId must reference a real shift, so every payload
    // creates one via the real Shifts API first — a fresh shift per call, to avoid any
    // cross-test ordering assumptions.
    private async Task<Guid> CreateShiftIdAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = "Morning Shift",
            startTime = "08:00:00",
            endTime = "16:00:00",
        });

        var created = await response.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();
        return created!.Data!.Id;
    }

    private async Task<object> NewAssignmentPayloadAsync() => new
    {
        staffId = Guid.NewGuid(),
        departmentId = Guid.NewGuid(),
        shiftId = await CreateShiftIdAsync(),
        rosterDate = "2026-08-04",
        status = "Scheduled",
        remarks = "First day",
    };

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedAssignment()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-assignments", await NewAssignmentPayloadAsync());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftAssignmentResponse>>();
        created!.Data!.Id.Should().NotBeEmpty();
        created.Data.Status.Should().Be(AssignmentStatus.Scheduled);

        var getResponse = await _client.GetAsync($"/api/v1/shift-assignments/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithNonExistentShiftId_ReturnsBadRequestWithInvalidShiftError()
    {
        var payload = new
        {
            staffId = Guid.NewGuid(),
            departmentId = Guid.NewGuid(),
            shiftId = Guid.NewGuid(),
            rosterDate = "2026-08-04",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/shift-assignments", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("HR.INVALID_SHIFT");
    }

    [Fact]
    public async Task Create_WithMissingStaffId_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/shift-assignments",
            new { departmentId = Guid.NewGuid(), shiftId = await CreateShiftIdAsync(), rosterDate = "2026-08-04" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().NotBeNullOrEmpty();
        error.ValidationErrors!.Should().Contain(e => e.Field == "StaffId");
    }

    [Fact]
    public async Task Create_AllowsTheSameStaffOnTheSameDateTwice_NoOverlapCheck()
    {
        var shiftId = await CreateShiftIdAsync();
        var staffId = Guid.NewGuid();
        var payload = new
        {
            staffId,
            departmentId = Guid.NewGuid(),
            shiftId,
            rosterDate = "2026-08-04",
        };

        var first = await _client.PostAsJsonAsync("/api/v1/shift-assignments", payload);
        var second = await _client.PostAsJsonAsync("/api/v1/shift-assignments", payload);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/shift-assignments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-assignments", await NewAssignmentPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftAssignmentResponse>>();
        var newShiftId = await CreateShiftIdAsync();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/shift-assignments/{created!.Data!.Id}", new
        {
            staffId = created.Data.StaffId,
            departmentId = created.Data.DepartmentId,
            shiftId = newShiftId,
            rosterDate = "2026-08-05",
            status = "Completed",
            remarks = "Done",
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftAssignmentResponse>>();
        updated!.Data!.Status.Should().Be(AssignmentStatus.Completed);
        updated.Data.ShiftId.Should().Be(newShiftId);
    }

    [Fact]
    public async Task Update_WithNonExistentShiftId_ReturnsBadRequestWithInvalidShiftError()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-assignments", await NewAssignmentPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftAssignmentResponse>>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/shift-assignments/{created!.Data!.Id}", new
        {
            staffId = created.Data.StaffId,
            departmentId = created.Data.DepartmentId,
            shiftId = Guid.NewGuid(),
            rosterDate = "2026-08-05",
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await updateResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("HR.INVALID_SHIFT");
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/shift-assignments/{Guid.NewGuid()}", new
        {
            staffId = Guid.NewGuid(),
            departmentId = Guid.NewGuid(),
            shiftId = await CreateShiftIdAsync(),
            rosterDate = "2026-08-04",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-assignments", await NewAssignmentPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftAssignmentResponse>>();
        var id = created!.Data!.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/shift-assignments/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/shift-assignments/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/shift-assignments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ReturnsEnvelopeWithPaginationMeta()
    {
        await _client.PostAsJsonAsync("/api/v1/shift-assignments", await NewAssignmentPayloadAsync());

        var response = await _client.GetAsync("/api/v1/shift-assignments?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ShiftAssignmentResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }
}
