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

    // CreateUserRequest.RoleId must reference a real role, so every payload creates one
    // via the real Roles API first — a fresh role per call (unique name) rather than a
    // shared/cached one, to avoid any cross-test ordering assumptions.
    private async Task<Guid> CreateRoleIdAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            name = $"role-{Guid.NewGuid():N}",
            displayOrder = 0,
            permissionKeys = new[] { "patient-management.view" },
        });

        var created = await response.Content.ReadFromJsonAsync<ApiResponse<RoleResponse>>();
        return created!.Data!.Id;
    }

    private async Task<object> NewUserPayloadAsync(string? email = null, string? username = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new
        {
            username = username ?? $"ada-{suffix}",
            firstName = "Ada",
            lastName = "Lovelace",
            email = email ?? $"ada-{suffix}@example.com",
            phoneNumber = "9876543210",
            roleId = await CreateRoleIdAsync(),
        };
    }

    [Fact]
    public async Task CreateThenGetById_ReturnsTheCreatedUser()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync());
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
        var payload = await NewUserPayloadAsync();

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

        (await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync(username: username)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync(username: username));

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
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync());
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
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync());
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
        await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync());

        var response = await _client.GetAsync("/api/v1/users?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserResponse>>>();
        body!.Data.Should().NotBeNull();
        body.Meta.Should().NotBeNull();
    }

    // Real JPEG signature (FF D8 FF ...) — UserService's magic-byte check rejects anything
    // that doesn't start with this, so a fake byte array won't pass as a valid upload here.
    private static readonly byte[] ValidJpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    [Fact]
    public async Task UploadProfilePhoto_WhenUserNotFound_ReturnsNotFound()
    {
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(ValidJpegBytes), "photo", "photo.jpg" },
        };

        var response = await _client.PostAsync($"/api/v1/users/{Guid.NewGuid()}/profile-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("IDENTITY.USER_NOT_FOUND");
    }

    [Fact]
    public async Task UploadProfilePhoto_WithEmptyFile_ReturnsBadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent([]), "photo", "empty.jpg" },
        };

        var response = await _client.PostAsync($"/api/v1/users/{created!.Data!.Id}/profile-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProfilePhoto_WithValidJpeg_UpdatesProfilePhotoUrlAndPersistsIt()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        created!.Data!.ProfilePhotoUrl.Should().BeNull();

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(ValidJpegBytes), "photo", "photo.jpg" },
        };

        var uploadResponse = await _client.PostAsync($"/api/v1/users/{created.Data.Id}/profile-photo", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        uploaded!.Data!.ProfilePhotoUrl.Should().Be($"uploads/users/{created.Data.Id}.jpg");

        var getResponse = await _client.GetAsync($"/api/v1/users/{created.Data.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        fetched!.Data!.ProfilePhotoUrl.Should().Be($"uploads/users/{created.Data.Id}.jpg");
    }

    [Fact]
    public async Task UploadProfilePhoto_WithCorruptedImage_ReturnsBadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", await NewUserPayloadAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        using var content = new MultipartFormDataContent
        {
            // "virus.jpg" — right extension, but the bytes aren't a real JPEG.
            { new ByteArrayContent("not a real image"u8.ToArray()), "photo", "virus.jpg" },
        };

        var response = await _client.PostAsync($"/api/v1/users/{created!.Data!.Id}/profile-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("IDENTITY.USER_INVALID_FILE");
    }
}
