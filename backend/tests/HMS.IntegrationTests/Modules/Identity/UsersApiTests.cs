using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.IntegrationTests.Modules.Identity;

/// <summary>
/// Black-box tests against the real HTTP surface (no internal types referenced),
/// covering the CRUD + soft-delete + activate/deactivate flows from
/// docs/modules/Identity/Users.md. Requires Docker (Testcontainers) to run.
/// </summary>
public class UsersApiTests : IClassFixture<UsersApiFactory>
{
    private readonly HttpClient _client;

    public UsersApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object NewUserPayload(string? email = null, string? username = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new
        {
            username = username ?? $"ada-{suffix}",
            firstName = "Ada",
            lastName = "Lovelace",
            email = email ?? $"ada-{suffix}@example.com",
        };
    }

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedUser()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", NewUserPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        created!.Data!.Id.Should().NotBeEmpty();
        created.Data.IsActive.Should().BeTrue();

        var getResponse = await _client.GetAsync($"/api/v1/users/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        var payload = NewUserPayload();

        (await _client.PostAsJsonAsync("/api/v1/users", payload)).StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await _client.PostAsJsonAsync("/api/v1/users", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await second.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("IDENTITY.USER_EMAIL_DUPLICATE");
        error.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_WithDuplicateUsername_ReturnsConflict()
    {
        var username = $"ada-{Guid.NewGuid():N}";

        (await _client.PostAsJsonAsync("/api/v1/users", NewUserPayload(username: username)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await _client.PostAsJsonAsync("/api/v1/users", NewUserPayload(username: username));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await second.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("IDENTITY.USER_USERNAME_DUPLICATE");
        error.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_WithMissingFirstName_ReturnsBadRequestWithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users",
            new { firstName = "", lastName = "Lovelace", email = $"ada-{Guid.NewGuid():N}@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ValidationErrors.Should().NotBeNullOrEmpty();
        error.ValidationErrors!.Should().Contain(e => e.Field == "FirstName");
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateThenDeactivate_TogglesStatus()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", NewUserPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        var id = created!.Data!.Id;

        var deactivateResponse = await _client.PostAsync($"/api/v1/users/{id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        deactivated!.Data!.IsActive.Should().BeFalse();

        var activateResponse = await _client.PostAsync($"/api/v1/users/{id}/activate", null);
        var activated = await activateResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        activated!.Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", NewUserPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        var id = created!.Data!.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/users/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/users/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ReturnsEnvelopeWithPaginationMeta()
    {
        await _client.PostAsJsonAsync("/api/v1/users", NewUserPayload());

        var response = await _client.GetAsync("/api/v1/users?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }
}
