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
/// Shift Management Phase 1. Reuses UsersApiFactory (boots the full HMS.Api host, including
/// this module) rather than an HR-specific factory — no other module has its own either.
/// Requires Docker (Testcontainers) to run.
/// </summary>
public class ShiftsApiTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public ShiftsApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object NewShiftPayload(string? code = null) => new
    {
        code = code ?? $"shift-{Guid.NewGuid():N}",
        name = "Morning Shift",
        startTime = "08:00:00",
        endTime = "16:00:00",
        breakMinutes = 30,
        graceMinutes = 10,
        isNightShift = false,
        isActive = true,
    };

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedShift()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shifts", NewShiftPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();
        created!.Data!.Id.Should().NotBeEmpty();
        created.Data.IsActive.Should().BeTrue();

        var getResponse = await _client.GetAsync($"/api/v1/shifts/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ReturnsBadRequest()
    {
        var payload = NewShiftPayload();

        (await _client.PostAsJsonAsync("/api/v1/shifts", payload)).StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await _client.PostAsJsonAsync("/api/v1/shifts", payload);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await second.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("HR.DUPLICATE_CODE");
    }

    [Fact]
    public async Task Create_WithMissingName_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new { code = $"shift-{Guid.NewGuid():N}", name = "", startTime = "08:00:00", endTime = "16:00:00" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().NotBeNullOrEmpty();
        error.ValidationErrors!.Should().Contain(e => e.Field == "Name");
    }

    [Fact]
    public async Task Create_WithMissingStartTime_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new { code = $"shift-{Guid.NewGuid():N}", name = "Morning", endTime = "16:00:00" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().Contain(e => e.Field == "StartTime");
    }

    [Fact]
    public async Task Create_WithAnUnparsableTime_ReturnsBadRequestNotServerError()
    {
        // A body where a field fails to bind (here, StartTime holding a string that isn't a
        // real TimeOnly) previously deserialized to a null request instead of tripping
        // [ApiController]'s automatic 400 — passing that null into FluentValidation threw
        // ArgumentNullException, surfacing as a raw 500.
        var response = await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = "Morning Shift",
            startTime = "not-a-time",
            endTime = "16:00:00",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/shifts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesFieldsButNotCode()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shifts", NewShiftPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/shifts/{created!.Data!.Id}", new
        {
            name = "Morning (Revised)",
            startTime = "09:00:00",
            endTime = "17:00:00",
            breakMinutes = 45,
            graceMinutes = 15,
            isNightShift = false,
            isActive = false,
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();
        updated!.Data!.Name.Should().Be("Morning (Revised)");
        updated.Data.Code.Should().Be(created.Data.Code);
        updated.Data.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_WithAnUnparsableTime_ReturnsBadRequestNotServerError()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shifts", NewShiftPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();

        var response = await _client.PutAsJsonAsync($"/api/v1/shifts/{created!.Data!.Id}", new
        {
            name = "Morning Shift",
            startTime = "not-a-time",
            endTime = "16:00:00",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/shifts/{Guid.NewGuid()}", new
        {
            name = "Morning",
            startTime = "08:00:00",
            endTime = "16:00:00",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shifts", NewShiftPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();
        var id = created!.Data!.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/shifts/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/shifts/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/shifts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ReturnsEnvelopeWithPaginationMeta()
    {
        await _client.PostAsJsonAsync("/api/v1/shifts", NewShiftPayload());

        var response = await _client.GetAsync("/api/v1/shifts?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ShiftResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaged_WithSearch_FiltersByCodeOrName()
    {
        var uniqueName = $"Zebra-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = uniqueName,
            startTime = "08:00:00",
            endTime = "16:00:00",
        });

        var response = await _client.GetAsync($"/api/v1/shifts?search={uniqueName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ShiftResponse>>>();
        body!.Data.Should().ContainSingle(s => s.Name == uniqueName);
    }

    [Fact]
    public async Task Create_WithIsNightShiftTrue_ButTimesDoNotCrossMidnight_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = "Morning Shift",
            startTime = "08:00:00",
            endTime = "16:00:00",
            isNightShift = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().Contain(e => e.Field == "IsNightShift");
    }

    [Fact]
    public async Task Create_WithIsNightShiftFalse_ButTimesCrossMidnight_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = "Night Shift",
            startTime = "22:00:00",
            endTime = "06:00:00",
            isNightShift = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().Contain(e => e.Field == "IsNightShift");
    }

    [Fact]
    public async Task Create_WithIsNightShiftTrue_AndTimesCrossMidnight_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = "Night Shift",
            startTime = "22:00:00",
            endTime = "06:00:00",
            isNightShift = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Update_WithIsNightShiftTrue_ButTimesDoNotCrossMidnight_ReturnsBadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shifts", NewShiftPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();

        var response = await _client.PutAsJsonAsync($"/api/v1/shifts/{created!.Data!.Id}", new
        {
            name = "Morning Shift",
            startTime = "08:00:00",
            endTime = "16:00:00",
            isNightShift = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().Contain(e => e.Field == "IsNightShift");
    }

    /// <summary>
    /// Reproduces the exact bug report: create a real 22:00–06:00 night shift, then edit
    /// it toggling "Night shift" off without changing the times. The times still cross
    /// midnight, so IsNightShift:false is just as inconsistent as IsNightShift:true was on
    /// a same-day shift — this must be rejected the same way.
    /// </summary>
    [Fact]
    public async Task Update_TogglingIsNightShiftOff_ButTimesStillCrossMidnight_ReturnsBadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/shifts", new
        {
            code = $"shift-{Guid.NewGuid():N}",
            name = "Night Shift",
            startTime = "22:00:00",
            endTime = "06:00:00",
            isNightShift = true,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>();

        var response = await _client.PutAsJsonAsync($"/api/v1/shifts/{created!.Data!.Id}", new
        {
            name = "Night Shift",
            startTime = "22:00:00",
            endTime = "06:00:00",
            isNightShift = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().Contain(e => e.Field == "IsNightShift");
    }
}
