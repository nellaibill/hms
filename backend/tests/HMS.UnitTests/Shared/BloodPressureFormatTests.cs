using FluentAssertions;
using HMS.Modules.OpdConsultation.Application.Validators;
using HMS.Modules.OpdConsultation.Contracts;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.UnitTests.Shared;

public class BloodPressureFormatTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("120/80")]
    [InlineData(" 120 / 80 ")]
    [InlineData("120/80 mmHg")]
    [InlineData("90/60mmhg")]
    [InlineData("300/200")]
    [InlineData("40/20")]
    public void IsValidOrEmpty_AcceptsBlankOrAPlausibleReading(string? value)
    {
        BloodPressureFormat.IsValidOrEmpty(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("120")]
    [InlineData("120-80")]
    [InlineData("120/80/60")]
    [InlineData("1200/80")]
    [InlineData("301/80")]
    [InlineData("120/19")]
    [InlineData("120/201")]
    [InlineData("39/20")]
    [InlineData("80/120")]
    [InlineData("80/80")]
    public void IsValidOrEmpty_RejectsTextOutOfRangeOrTransposedReadings(string value)
    {
        BloodPressureFormat.IsValidOrEmpty(value).Should().BeFalse();
    }

    [Fact]
    public void OpdConsultationValidator_RejectsNonNumericBloodPressure()
    {
        var result = new SaveOpdConsultationRequestValidator().Validate(new SaveOpdConsultationRequest { BloodPressure = "abc" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(SaveOpdConsultationRequest.BloodPressure))
            .Which.ErrorMessage.Should().Be(BloodPressureFormat.Message);
    }

    [Fact]
    public void OpdConsultationValidator_AcceptsAValidBloodPressure()
    {
        var result = new SaveOpdConsultationRequestValidator().Validate(new SaveOpdConsultationRequest { BloodPressure = "120/80" });

        result.IsValid.Should().BeTrue();
    }
}
