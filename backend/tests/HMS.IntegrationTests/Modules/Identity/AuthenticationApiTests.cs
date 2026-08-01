using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HMS.Modules.Identity.Contracts;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.IntegrationTests.Modules.Identity;

/// <summary>
/// Black-box tests against the real HTTP surface for POST /api/v1/auth/login and the
/// protected GET /api/v1/auth/me, per the Enterprise Login Module spec. Requires Docker
/// (Testcontainers) to run — same limitation as UsersApiTests.
/// </summary>
public class AuthenticationApiTests : IClassFixture<UsersApiFactory>
{
    private const string Password = "Sup3rSecret!";

    private readonly HttpClient _client;

    public AuthenticationApiTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // "doctor" is one of the web login page's fixed LoginType values; LoginTypes.cs maps it
    // to the Role display name "Doctor / Consultant" — a role with that exact name has to
    // exist (and be assigned to the user) for a "doctor" login to succeed.
    private async Task<(Guid UserId, string Username)> CreateLoggableUserAsync(string roleName = "Doctor / Consultant")
    {
        var roleResponse = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            name = roleName,
            displayOrder = 0,
            permissionKeys = new[] { "patient-management.view" },
        });
        var role = await roleResponse.Content.ReadFromJsonAsync<ApiResponse<RoleResponse>>();

        var suffix = Guid.NewGuid().ToString("N");
        var username = $"dr-ada-{suffix}";
        var userResponse = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            username,
            firstName = "Ada",
            lastName = "Lovelace",
            email = $"ada-{suffix}@example.com",
            phoneNumber = "9876543210",
            roleId = role!.Data!.Id,
        });
        var user = await userResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        var setPasswordResponse = await _client.PostAsJsonAsync(
            $"/api/v1/users/{user!.Data!.Id}/password", new { password = Password });
        setPasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (user.Data.Id, username);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var (userId, username) = await CreateLoggableUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            loginType = "doctor",
            username,
            password = Password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        body!.Data!.Token.Should().NotBeNullOrWhiteSpace();
        body.Data.ExpiresIn.Should().BeGreaterThan(0);
        body.Data.User.Id.Should().Be(userId);
        body.Data.User.LoginType.Should().Be("doctor");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedWithGenericMessage()
    {
        var (_, username) = await CreateLoggableUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            loginType = "doctor",
            username,
            password = "wrong-password",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("IDENTITY.INVALID_LOGIN");
        error.Message.Should().Be("Invalid username or password.");
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorizedWithGenericMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            loginType = "doctor",
            username = $"nobody-{Guid.NewGuid():N}",
            password = Password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be("IDENTITY.INVALID_LOGIN");
    }

    [Fact]
    public async Task Login_WithLoginTypeThatDoesNotMatchAssignedRole_ReturnsUnauthorized()
    {
        var (_, username) = await CreateLoggableUserAsync(roleName: "Doctor / Consultant");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            loginType = "nurse",
            username,
            password = Password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ForAnInactiveUser_ReturnsUnauthorized()
    {
        var (userId, username) = await CreateLoggableUserAsync();
        (await _client.PostAsync($"/api/v1/users/{userId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            loginType = "doctor",
            username,
            password = Password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "someone", "Sup3rSecret!")]
    [InlineData("doctor", "", "Sup3rSecret!")]
    [InlineData("doctor", "someone", "")]
    [InlineData("not-a-real-login-type", "someone", "Sup3rSecret!")]
    public async Task Login_WithMissingOrUnrecognizedFields_ReturnsBadRequest(string loginType, string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { loginType, username, password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithGarbageToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAValidToken_ReturnsTheCallersClaims()
    {
        var (_, username) = await CreateLoggableUserAsync();
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            loginType = "doctor",
            username,
            password = Password,
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Data!.Token);
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, string>>>();
        body!.Data.Should().ContainKey("Username").WhoseValue.Should().Be(username);
    }
}
