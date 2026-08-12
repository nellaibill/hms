using FluentAssertions;
using HMS.Modules.Patients.Infrastructure.Repositories;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Infrastructure;

/// <summary>
/// Covers PatientRepository.NormalizePhone directly — the piece that lets
/// FindDuplicateAsync catch the same person registered under differently-formatted
/// versions of the same number (docs: PatientRegistration issues list, "Duplicate
/// Detection"). A full FindDuplicateAsync test would need a real/in-memory database;
/// the normalization itself is a pure function and is the part actually under test here.
/// </summary>
public class PatientRepositoryPhoneNormalizationTests
{
    [Theory]
    [InlineData("9876543210", "9876543210")]
    [InlineData("+91-98765-43210", "9876543210")]
    [InlineData("+91 98765 43210", "9876543210")]
    [InlineData("(987) 654-3210", "9876543210")]
    [InlineData("091-9876543210", "9876543210")]
    public void NormalizePhone_StripsFormattingAndCountryCode_ToLast10Digits(string input, string expected)
    {
        PatientRepository.NormalizePhone(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("9876543210", "+91-98765-43210")]
    [InlineData("98765 43210", "(98765) 43210")]
    public void NormalizePhone_ProducesEqualResult_ForDifferentlyFormattedSameNumber(string first, string second)
    {
        PatientRepository.NormalizePhone(first).Should().Be(PatientRepository.NormalizePhone(second));
    }

    [Fact]
    public void NormalizePhone_DoesNotMatch_DifferentNumbers()
    {
        PatientRepository.NormalizePhone("9876543210").Should().NotBe(PatientRepository.NormalizePhone("9876543211"));
    }

    [Fact]
    public void NormalizePhone_ReturnsEmpty_ForSymbolOnlyInput()
    {
        PatientRepository.NormalizePhone("----------").Should().BeEmpty();
    }
}
