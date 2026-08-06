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
/// Shift Swap Request (Phase 5), including the CurrentShiftAssignmentId/
/// RequestedShiftAssignmentId referential validation. Reuses UsersApiFactory, same as
/// ShiftsApiTests. Requires Docker (Testcontainers) to run.
/// </summary>
public class ShiftSwapRequestsApiTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public ShiftSwapRequestsApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // CreateSwapRequest.CurrentShiftAssignmentId/RequestedShiftAssignmentId must reference
    // real shift assignments, so every payload creates a shift + assignment via the real
    // APIs first — a fresh pair per call, to avoid any cross-test ordering assumptions.
    private async Task<Guid> CreateShiftAssignmentIdAsync()
    {
        var shiftResponse = await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = "Morning Shift",
            startTime = "08:00:00",
            endTime = "16:00:00",
        });
        var shift = await shiftResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();

        var assignmentResponse = await _client.PostAsJsonAsync("/api/v1/shift-assignments", new
        {
            staffId = Guid.NewGuid(),
            departmentId = Guid.NewGuid(),
            shiftId = shift!.Data!.Id,
            rosterDate = "2026-08-03",
        });
        var assignment = await assignmentResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftAssignmentResponse>>();
        return assignment!.Data!.Id;
    }

    private async Task<object> NewSwapRequestPayloadAsync() => new
    {
        requestedByStaffId = Guid.NewGuid(),
        requestedToStaffId = Guid.NewGuid(),
        currentShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
        requestedShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
        status = "Pending",
        requestedDate = "2026-08-03T09:00:00Z",
        remarks = "Please swap",
    };

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedSwapRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", await NewSwapRequestPayloadAsync());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SwapRequestResponse>>();
        created!.Data!.Id.Should().NotBeEmpty();
        created.Data.Status.Should().Be(SwapRequestStatus.Pending);

        var getResponse = await _client.GetAsync($"/api/v1/shift-swap-requests/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithNonExistentCurrentShiftAssignmentId_ReturnsBadRequestWithInvalidShiftAssignmentError()
    {
        var payload = new
        {
            requestedByStaffId = Guid.NewGuid(),
            requestedToStaffId = Guid.NewGuid(),
            currentShiftAssignmentId = Guid.NewGuid(),
            requestedShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
            status = "Pending",
            requestedDate = "2026-08-03T09:00:00Z",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("HR.INVALID_SHIFT_ASSIGNMENT");
    }

    [Fact]
    public async Task Create_WithNonExistentRequestedShiftAssignmentId_ReturnsBadRequestWithInvalidShiftAssignmentError()
    {
        var payload = new
        {
            requestedByStaffId = Guid.NewGuid(),
            requestedToStaffId = Guid.NewGuid(),
            currentShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
            requestedShiftAssignmentId = Guid.NewGuid(),
            status = "Pending",
            requestedDate = "2026-08-03T09:00:00Z",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("HR.INVALID_SHIFT_ASSIGNMENT");
    }

    [Fact]
    public async Task Create_WithMissingStatus_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", new
        {
            requestedByStaffId = Guid.NewGuid(),
            requestedToStaffId = Guid.NewGuid(),
            currentShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
            requestedShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
            requestedDate = "2026-08-03T09:00:00Z",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors!.Should().Contain(e => e.Field == "Status");
    }

    [Fact]
    public async Task Create_WithAnInvalidEnumValue_ReturnsBadRequestNotServerError()
    {
        // A body where every field fails to bind (here, a SwapRequestStatus value that
        // isn't a real enum member) previously deserialized to a null request instead of
        // tripping [ApiController]'s automatic 400 — passing that null into
        // FluentValidation threw ArgumentNullException, surfacing as a raw 500.
        var response = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", new
        {
            requestedByStaffId = Guid.NewGuid(),
            requestedToStaffId = Guid.NewGuid(),
            currentShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
            requestedShiftAssignmentId = await CreateShiftAssignmentIdAsync(),
            status = "NotARealStatus",
            requestedDate = "2026-08-03T09:00:00Z",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/shift-swap-requests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesStatusAndApprovalFields()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", await NewSwapRequestPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SwapRequestResponse>>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/shift-swap-requests/{created!.Data!.Id}", new
        {
            requestedByStaffId = created.Data.RequestedByStaffId,
            requestedToStaffId = created.Data.RequestedToStaffId,
            currentShiftAssignmentId = created.Data.CurrentShiftAssignmentId,
            requestedShiftAssignmentId = created.Data.RequestedShiftAssignmentId,
            status = "Approved",
            requestedDate = "2026-08-03T09:00:00Z",
            approvedBy = Guid.NewGuid(),
            approvedDate = "2026-08-04T00:00:00Z",
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<SwapRequestResponse>>();
        updated!.Data!.Status.Should().Be(SwapRequestStatus.Approved);
        updated.Data.ApprovedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithAnInvalidEnumValue_ReturnsBadRequestNotServerError()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", await NewSwapRequestPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SwapRequestResponse>>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/shift-swap-requests/{created!.Data!.Id}", new
        {
            requestedByStaffId = created.Data.RequestedByStaffId,
            requestedToStaffId = created.Data.RequestedToStaffId,
            currentShiftAssignmentId = created.Data.CurrentShiftAssignmentId,
            requestedShiftAssignmentId = created.Data.RequestedShiftAssignmentId,
            status = "NotARealStatus",
            requestedDate = "2026-08-03T09:00:00Z",
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/shift-swap-requests/{Guid.NewGuid()}", await NewSwapRequestPayloadAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", await NewSwapRequestPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SwapRequestResponse>>();
        var id = created!.Data!.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/shift-swap-requests/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/shift-swap-requests/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/shift-swap-requests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ReturnsEnvelopeWithPaginationMeta()
    {
        await _client.PostAsJsonAsync("/api/v1/shift-swap-requests", await NewSwapRequestPayloadAsync());

        var response = await _client.GetAsync("/api/v1/shift-swap-requests?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SwapRequestResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }
}
