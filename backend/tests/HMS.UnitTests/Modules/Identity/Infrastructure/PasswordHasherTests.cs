using FluentAssertions;
using HMS.Modules.Identity.Infrastructure;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_NeverReturnsThePlaintextPassword()
    {
        var hash = _sut.HashPassword("Sup3rSecret!");

        hash.Should().NotBe("Sup3rSecret!");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void VerifyPassword_WithTheCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.HashPassword("Sup3rSecret!");

        _sut.VerifyPassword("Sup3rSecret!", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithTheWrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("Sup3rSecret!");

        _sut.VerifyPassword("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_CalledTwiceForTheSamePassword_ProducesDifferentHashes()
    {
        // PasswordHasher<T> salts each hash, so two hashes of the same password never match
        // byte-for-byte — a basic defense against rainbow-table attacks.
        var first = _sut.HashPassword("Sup3rSecret!");
        var second = _sut.HashPassword("Sup3rSecret!");

        first.Should().NotBe(second);
        _sut.VerifyPassword("Sup3rSecret!", first).Should().BeTrue();
        _sut.VerifyPassword("Sup3rSecret!", second).Should().BeTrue();
    }
}
