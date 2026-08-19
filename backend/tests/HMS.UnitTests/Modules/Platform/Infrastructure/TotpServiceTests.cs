using FluentAssertions;
using HMS.Modules.Platform.Infrastructure;
using OtpNet;
using Xunit;

namespace HMS.UnitTests.Modules.Platform.Infrastructure;

/// <summary>
/// Exercises the real Otp.NET integration (not a mock) — the one place in this backlog item
/// where getting the crypto wrong would be a silent security failure, so this proves actual
/// RFC 6238 interop rather than just that the wrapper compiles.
/// </summary>
public class TotpServiceTests
{
    private readonly TotpService _sut = new();

    [Fact]
    public void GenerateSecret_ReturnsAValidBase32String()
    {
        var secret = _sut.GenerateSecret();

        var act = () => Base32Encoding.ToBytes(secret);
        act.Should().NotThrow();
        Base32Encoding.ToBytes(secret).Length.Should().Be(20);
    }

    [Fact]
    public void VerifyCode_WithTheCurrentCodeForThatSecret_ReturnsTrue()
    {
        var secret = _sut.GenerateSecret();
        var currentCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

        _sut.VerifyCode(secret, currentCode).Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_WithAWrongCode_ReturnsFalse()
    {
        var secret = _sut.GenerateSecret();
        var currentCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
        var wrongCode = currentCode == "000000" ? "111111" : "000000";

        _sut.VerifyCode(secret, wrongCode).Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithACodeForADifferentSecret_ReturnsFalse()
    {
        var secret = _sut.GenerateSecret();
        var codeForADifferentSecret = new Totp(Base32Encoding.ToBytes(_sut.GenerateSecret())).ComputeTotp();

        _sut.VerifyCode(secret, codeForADifferentSecret).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    public void VerifyCode_WithAnEmptyOrMalformedCode_ReturnsFalseWithoutThrowing(string malformedCode)
    {
        var secret = _sut.GenerateSecret();

        _sut.VerifyCode(secret, malformedCode).Should().BeFalse();
    }

    [Fact]
    public void BuildOtpAuthUri_EncodesTheSecretAndAccountEmail()
    {
        var uri = _sut.BuildOtpAuthUri("JBSWY3DPEHPK3PXP", "admin@example.com");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("secret=JBSWY3DPEHPK3PXP");
        uri.Should().Contain(Uri.EscapeDataString("admin@example.com"));
    }
}
