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
}
