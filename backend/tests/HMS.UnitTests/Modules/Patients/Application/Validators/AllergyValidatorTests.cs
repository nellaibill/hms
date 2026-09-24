using FluentAssertions;
using HMS.Modules.Patients.Application.Validators;
using HMS.Modules.Patients.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application.Validators;

// REG-01: an allergy saved as 'Drug / Severe' with no substance named is useless to a
// clinician, so the allergen is required both at registration and when adding one later.
public class AllergyValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAllergy_WithoutAllergen_Fails(string? specify)
    {
        var result = new AddAllergyRequestValidator().Validate(new AddAllergyRequest { AllergyType = AllergyType.Drug, Specify = specify, Severity = AllergySeverity.Severe });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Specify");
    }

    [Fact]
    public void AddAllergy_WithAllergen_Passes()
    {
        var result = new AddAllergyRequestValidator().Validate(new AddAllergyRequest { AllergyType = AllergyType.Drug, Specify = "Penicillin", Severity = AllergySeverity.Severe });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegistrationAllergy_WithoutAllergen_Fails()
    {
        var result = new AllergyRequestValidator().Validate(new AllergyRequest { AllergyType = AllergyType.Food, Specify = null, Severity = AllergySeverity.Mild });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Specify");
    }
}
