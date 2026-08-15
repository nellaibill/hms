using FluentAssertions;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Application;

public class AuthenticationServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ITenantContext _tenantContext = new TenantContext();
    private readonly AuthenticationService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AuthenticationServiceTests()
    {
        // Mirrors what HMS.Api's TenantResolutionMiddleware already did before this
        // service's LoginAsync is ever called in production — see that middleware's own
        // doc comment for why tenant resolution can't happen inside AuthenticationService
        // itself.
        _tenantContext.SetTenant(_tenantId, "Host=localhost;Database=hms_legacy");

        _sut = new AuthenticationService(
            _userRepository, _roleRepository, _passwordHasher, _jwtTokenGenerator, _tenantContext, NullLogger<AuthenticationService>.Instance);
    }

    // A single user + role + password, wired to pass every rule, so each failure test only
    // needs to break the one condition it's testing.
    private (User User, Role Role) SeedActiveUserWithMatchingRole(string loginType = "doctor", string roleName = "Doctor / Consultant")
    {
        var role = Role.Create(roleName, null, false, 0, null);
        var user = User.Create("dr.ada", "Ada", "Lovelace", "ada@example.com", null, role.Id, null);
        user.SetPasswordHash("stored-hash", null);

        _userRepository.GetByUsernameAsync("dr.ada", Arg.Any<CancellationToken>()).Returns(user);
        _roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _passwordHasher.VerifyPassword("correct-password", "stored-hash").Returns(true);
        _jwtTokenGenerator
            .GenerateToken(user.Id, user.Username, role.Id, role.Name, loginType, Arg.Any<IEnumerable<string>>(), _tenantId)
            .Returns(("a.jwt.token", 3600));

        return (user, role);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        var (user, role) = SeedActiveUserWithMatchingRole();
        var request = new LoginRequest { LoginType = "doctor", Username = "dr.ada", Password = "correct-password" };

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("a.jwt.token");
        result.Value.ExpiresIn.Should().Be(3600);
        result.Value.User.Id.Should().Be(user.Id);
        result.Value.User.RoleName.Should().Be(role.Name);
        result.Value.User.LoginType.Should().Be("doctor");
        user.LastLoginAt.Should().NotBeNull();
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WhenUsernameNotFound_ReturnsGenericInvalidLoginFailure()
    {
        _userRepository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var request = new LoginRequest { LoginType = "doctor", Username = "nobody", Password = "whatever" };

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthenticationErrorCodes.InvalidLogin);
        result.Error.Should().Be("Invalid username or password.");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsGenericInvalidLoginFailure()
    {
        SeedActiveUserWithMatchingRole();
        _passwordHasher.VerifyPassword("wrong-password", "stored-hash").Returns(false);
        var request = new LoginRequest { LoginType = "doctor", Username = "dr.ada", Password = "wrong-password" };

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthenticationErrorCodes.InvalidLogin);
    }

    [Fact]
    public async Task LoginAsync_WhenUserHasNoPasswordSetYet_ReturnsGenericInvalidLoginFailure()
    {
        var role = Role.Create("Doctor / Consultant", null, false, 0, null);
        var user = User.Create("dr.ada", "Ada", "Lovelace", "ada@example.com", null, role.Id, null);
        _userRepository.GetByUsernameAsync("dr.ada", Arg.Any<CancellationToken>()).Returns(user);
        var request = new LoginRequest { LoginType = "doctor", Username = "dr.ada", Password = "anything" };

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthenticationErrorCodes.InvalidLogin);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ReturnsGenericInvalidLoginFailure()
    {
        var (user, _) = SeedActiveUserWithMatchingRole();
        user.Deactivate(null);
        var request = new LoginRequest { LoginType = "doctor", Username = "dr.ada", Password = "correct-password" };

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthenticationErrorCodes.InvalidLogin);
    }

    [Fact]
    public async Task LoginAsync_WhenLoginTypeDoesNotMatchAssignedRole_ReturnsGenericInvalidLoginFailure()
    {
        // Seeded role is "Doctor / Consultant", but the caller selects "nurse".
        SeedActiveUserWithMatchingRole();
        var request = new LoginRequest { LoginType = "nurse", Username = "dr.ada", Password = "correct-password" };

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthenticationErrorCodes.InvalidLogin);
        await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_DoesNotCallSaveChanges_WhenAnyRuleFails()
    {
        _userRepository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var request = new LoginRequest { LoginType = "doctor", Username = "nobody", Password = "whatever" };

        await _sut.LoginAsync(request, CancellationToken.None);

        await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WhenTenantContextIsNotResolved_Throws()
    {
        // Should be unreachable in production (TenantResolutionMiddleware always resolves
        // it first) — this proves the defensive guard fires rather than silently querying
        // whatever connection an unresolved tenant-aware DbContext might fall back to.
        var unresolvedTenantContext = new TenantContext();
        var sut = new AuthenticationService(
            _userRepository, _roleRepository, _passwordHasher, _jwtTokenGenerator, unresolvedTenantContext, NullLogger<AuthenticationService>.Instance);
        var request = new LoginRequest { LoginType = "doctor", Username = "dr.ada", Password = "correct-password" };

        var act = () => sut.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
