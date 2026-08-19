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
    private readonly IPlatformMfaChallengeStore _mfaChallengeStore = Substitute.For<IPlatformMfaChallengeStore>();
    private readonly ITotpService _totpService = Substitute.For<ITotpService>();
    private readonly IPlatformMfaSecretProtector _mfaSecretProtector = Substitute.For<IPlatformMfaSecretProtector>();
    private readonly ILogger<PlatformAuthenticationService> _logger = Substitute.For<ILogger<PlatformAuthenticationService>>();
    private readonly PlatformAuthenticationService _sut;

    public PlatformAuthenticationServiceTests()
    {
        _sut = new PlatformAuthenticationService(
            _repository, _passwordHasher, _jwtTokenGenerator, _mfaChallengeStore, _totpService, _mfaSecretProtector, _logger);

        // Identity protector by default — most tests don't care about encryption, just that
        // the secret round-trips; tests that do care override this.
        _mfaSecretProtector.Protect(Arg.Any<string>()).Returns(ci => ci.Arg<string>());
        _mfaSecretProtector.Unprotect(Arg.Any<string>()).Returns(ci => ci.Arg<string>());
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
        result.Value!.User!.Role.Should().Be("SupportUser");
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

    [Fact]
    public async Task LoginAsync_WhenMfaIsEnabled_ReturnsAChallengeInsteadOfATokenAndDoesNotIssueOne()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.SetPendingMfaSecret("secret");
        user.EnableMfa();
        _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("Sup3rSecret!", user.PasswordHash).Returns(true);
        _mfaChallengeStore.CreateAsync(user.Id, Arg.Any<CancellationToken>()).Returns("challenge-token");

        var result = await _sut.LoginAsync(new PlatformLoginRequest { Email = user.Email, Password = "Sup3rSecret!" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequired.Should().BeTrue();
        result.Value.MfaChallengeToken.Should().Be("challenge-token");
        result.Value.Token.Should().BeNull();
        result.Value.User.Should().BeNull();
        _jwtTokenGenerator.DidNotReceiveWithAnyArgs().GenerateToken(default, default!, default!, default);
    }

    [Fact]
    public async Task VerifyMfaAsync_WithCorrectCode_IssuesTheRealTokenAndConsumesTheChallenge()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.SetPendingMfaSecret("secret");
        user.EnableMfa();
        _mfaChallengeStore.ValidateAsync("challenge-token", Arg.Any<CancellationToken>()).Returns(user.Id);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _totpService.VerifyCode("secret", "123456").Returns(true);
        _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role).Returns(("token-value", 3600));

        var result = await _sut.VerifyMfaAsync(new PlatformMfaVerifyRequest { ChallengeToken = "challenge-token", Code = "123456" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequired.Should().BeFalse();
        result.Value.Token.Should().Be("token-value");
        result.Value.User!.Id.Should().Be(user.Id);
        await _mfaChallengeStore.Received(1).ConsumeAsync("challenge-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyMfaAsync_WithWrongCode_DoesNotConsumeTheChallenge_SoARetryCanStillSucceed()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.SetPendingMfaSecret("secret");
        user.EnableMfa();
        _mfaChallengeStore.ValidateAsync("challenge-token", Arg.Any<CancellationToken>()).Returns(user.Id);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _totpService.VerifyCode("secret", "000000").Returns(false);

        var firstAttempt = await _sut.VerifyMfaAsync(new PlatformMfaVerifyRequest { ChallengeToken = "challenge-token", Code = "000000" }, CancellationToken.None);
        firstAttempt.IsSuccess.Should().BeFalse();
        await _mfaChallengeStore.DidNotReceive().ConsumeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        _totpService.VerifyCode("secret", "123456").Returns(true);
        _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role).Returns(("token-value", 3600));
        var retry = await _sut.VerifyMfaAsync(new PlatformMfaVerifyRequest { ChallengeToken = "challenge-token", Code = "123456" }, CancellationToken.None);

        retry.IsSuccess.Should().BeTrue();
        retry.Value!.Token.Should().Be("token-value");
    }

    [Fact]
    public async Task VerifyMfaAsync_WithWrongCode_ReturnsFailure()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.SetPendingMfaSecret("secret");
        user.EnableMfa();
        _mfaChallengeStore.ValidateAsync("challenge-token", Arg.Any<CancellationToken>()).Returns(user.Id);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _totpService.VerifyCode("secret", "000000").Returns(false);

        var result = await _sut.VerifyMfaAsync(new PlatformMfaVerifyRequest { ChallengeToken = "challenge-token", Code = "000000" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.InvalidMfaCode);
    }

    [Fact]
    public async Task VerifyMfaAsync_WithExpiredOrUnknownChallenge_ReturnsFailureWithoutTouchingTheUser()
    {
        _mfaChallengeStore.ValidateAsync("bogus-token", Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _sut.VerifyMfaAsync(new PlatformMfaVerifyRequest { ChallengeToken = "bogus-token", Code = "123456" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.MfaChallengeInvalid);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMfaStatusAsync_ReflectsWhetherMfaIsCurrentlyEnabled()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var beforeEnabling = await _sut.GetMfaStatusAsync(user.Id, CancellationToken.None);
        beforeEnabling.Value!.Enabled.Should().BeFalse();

        user.SetPendingMfaSecret("secret");
        user.EnableMfa();
        var afterEnabling = await _sut.GetMfaStatusAsync(user.Id, CancellationToken.None);
        afterEnabling.Value!.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetupMfaAsync_StoresAnEncryptedPendingSecretWithoutEnablingMfa()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _totpService.GenerateSecret().Returns("raw-secret");
        _mfaSecretProtector.Protect("raw-secret").Returns("encrypted-secret");
        _totpService.BuildOtpAuthUri("raw-secret", user.Email).Returns("otpauth://totp/example");

        var result = await _sut.SetupMfaAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Secret.Should().Be("raw-secret");
        result.Value.OtpAuthUri.Should().Be("otpauth://totp/example");
        user.MfaSecret.Should().Be("encrypted-secret");
        user.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task EnableMfaAsync_WithCorrectCode_TurnsMfaOn()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.SetPendingMfaSecret("secret");
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _totpService.VerifyCode("secret", "123456").Returns(true);

        var result = await _sut.EnableMfaAsync(user.Id, "123456", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task EnableMfaAsync_WithWrongCode_DoesNotEnableMfa()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.SetPendingMfaSecret("secret");
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _totpService.VerifyCode("secret", "000000").Returns(false);

        var result = await _sut.EnableMfaAsync(user.Id, "000000", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.InvalidMfaCode);
        user.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task DisableMfaAsync_WithCorrectCode_ClearsTheSecretAndTurnsMfaOff()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        user.SetPendingMfaSecret("secret");
        user.EnableMfa();
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _totpService.VerifyCode("secret", "123456").Returns(true);

        var result = await _sut.DisableMfaAsync(user.Id, "123456", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.MfaEnabled.Should().BeFalse();
        user.MfaSecret.Should().BeNull();
    }

    [Fact]
    public async Task DisableMfaAsync_WhenMfaIsNotEnabled_ReturnsFailure()
    {
        var user = NewUser(PlatformRole.SuperAdmin);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.DisableMfaAsync(user.Id, "123456", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PlatformErrorCodes.MfaNotEnabled);
    }
}
