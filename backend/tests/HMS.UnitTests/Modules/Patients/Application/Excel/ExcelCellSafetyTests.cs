using FluentAssertions;
using HMS.Modules.Patients.Application.Excel;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Excel;

public class ExcelCellSafetyTests
{
    [Theory]
    [InlineData("=SUM(A1:A10)", "'=SUM(A1:A10)")]
    [InlineData("+1+1", "'+1+1")]
    [InlineData("-1+1", "'-1+1")]
    [InlineData("@SUM(1,1)", "'@SUM(1,1)")]
    public void Sanitize_PrefixesAFormulaTriggerWithALeadingApostrophe(string value, string expected)
    {
        ExcelCellSafety.Sanitize(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("Ravi Kumar")]
    [InlineData("P-2026-040012")]
    [InlineData("98765 43210")]
    public void Sanitize_LeavesOrdinaryTextUnchanged(string value)
    {
        ExcelCellSafety.Sanitize(value).Should().Be(value);
    }

    [Fact]
    public void Sanitize_AlsoPrefixesALegitimateValueThatHappensToStartWithATriggerCharacter()
    {
        // An international phone number starting with '+' gets the same apostrophe treatment
        // as a real formula payload — a deliberate, harmless false positive (OWASP's
        // recommended mitigation has no way to distinguish intent from a leading character
        // alone, and a stray leading apostrophe is a non-issue compared to formula execution).
        ExcelCellSafety.Sanitize("+919876543210").Should().Be("'+919876543210");
    }

    [Fact]
    public void Sanitize_ReturnsEmptyStringForNullOrEmptyInput()
    {
        ExcelCellSafety.Sanitize(null).Should().Be(string.Empty);
        ExcelCellSafety.Sanitize(string.Empty).Should().Be(string.Empty);
    }
}
