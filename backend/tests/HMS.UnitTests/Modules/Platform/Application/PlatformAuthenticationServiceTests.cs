using FluentAssertions;
using HMS.Modules.Platform.Application;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Platform.Application;

public class PlatformAuthenticationServiceTests
{
    private readonly IPlatformUserRepository _repository = Substitute.For<IPlatformUserRepository>();
    private readonly IPlatformPasswordHasher _passwordHasher = Substitute.For<IPlatformPasswordHasher>();
    private readonly IPlatformJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IPlatformJwtTokenGenerator>();
    private readonly ILogger<PlatformAuthenticationService> _logger = Substitute.For<ILogger<PlatformAuthenticationService>>();
    private readonly PlatformAuthenticationService _sut;

    public PlatformAuthenticationServiceTests()
    {
        _sut = new PlatformAuthenticationService(_repository, _passwordHasher, _jwtTokenGenerator, _logger);
    }

    private static PlatformUser NewUser(PlatformRole role) =>
        PlatformUser.Create("Platform Support", "support@hms.example", "hashed", role, createdBy: null);

    [Fact]
    public async Task LoginAsync_WithValidCredentials_PassesTheUsersRoleToTheTokenGeneratorAndResponse()
    {
        var user = NewUser(PlatformRole.SupportUser);
        _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("Sup3rSecret!", user.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, PlatformRole.SupportUser)
            .Returns(("token-value", 3600));

        var result = await _sut.LoginAsync(new PlatformLoginRequest { Email = user.Email, Password = "Sup3rSecret!" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.User.Role.Should().Be("SupportUser");
        _jwtTokenGenerator.Received(1).GenerateToken(user.Id, user.Email, user.FullName, PlatformRole.SupportUser);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsGenericInvalidLoginFailure()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("wrong", user.PasswordHash).Returns(false);

        var result = await _sut.LoginAsync(new PlatformLoginRequest { Email = user.Email, Password = "wrong" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.InvalidLogin);
        _jwtTokenGenerator.DidNotReceiveWithAnyArgs().GenerateToken(default, default!, default!, default);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_IncrementsFailedLoginAttempts()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("wrong", user.PasswordHash).Returns(false);

        await _sut.LoginAsync(new PlatformLoginRequest { Email = user.Email, Password = "wrong" }, CancellationToken.None);

        user.FailedLoginAttempts.Should().Be(1);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_AfterFiveWrongPasswordAttempts_LocksTheAccountOut()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("wrong", user.PasswordHash).Returns(false);

        for (var i = 0; i < 5; i++)
        {
            await _sut.LoginAsync(new PlatformLoginRequest { Email = user.Email, Password = "wrong" }, CancellationToken.None);
        }

        user.IsLockedOut(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsLockedOut_ReturnsGenericFailureWithoutCheckingThePassword()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.RecordFailedLogin(DateTime.UtcNow, maxAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15));
        _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.LoginAsync(new PlatformLoginRequest { Email = user.Email, Password = "Sup3rSecret!" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.InvalidLogin);
        _passwordHasher.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ResetsAnyPriorFailedAttempts()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.RecordFailedLogin(DateTime.UtcNow, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("Sup3rSecret!", user.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, PlatformRole.SuperAdmin).Returns(("token", 3600));

        await _sut.LoginAsync(new PlatformLoginRequest { Email = user.Email, Password = "Sup3rSecret!" }, CancellationToken.None);

        user.FailedLoginAttempts.Should().Be(0);
        user.LockedOutUntil.Should().BeNull();
    }
}
