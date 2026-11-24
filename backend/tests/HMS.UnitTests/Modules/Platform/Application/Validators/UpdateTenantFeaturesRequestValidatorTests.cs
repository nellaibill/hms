using FluentAssertions;
using HMS.Modules.Platform.Application.Validators;
using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.UnitTests.Modules.Platform.Application.Validators;

public class UpdateTenantFeaturesRequestValidatorTests
{
    private readonly UpdateTenantFeaturesRequestValidator _sut = new();

    [Fact]
    public void Validate_WithMandatoryFeaturesAndPharmacyPlusProducts_Passes()
    {
        var request = new UpdateTenantFeaturesRequest { EnabledFeatures = [.. FeatureCatalog.Mandatory, "pharmacy", "products"] };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingAMandatoryFeature_Fails()
    {
        var request = new UpdateTenantFeaturesRequest { EnabledFeatures = FeatureCatalog.Mandatory.Skip(1).ToList() };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPharmacyButNotProducts_Fails()
    {
        // Also covers the "disable Products while Pharmacy stays enabled" case: this
        // validates the full desired set every time, not just newly-added keys.
        var request = new UpdateTenantFeaturesRequest { EnabledFeatures = [.. FeatureCatalog.Mandatory, "pharmacy"] };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EnabledFeatures");
    }

    [Fact]
    public void Validate_WithUnrecognizedFeatureKey_Fails()
    {
        var request = new UpdateTenantFeaturesRequest { EnabledFeatures = [.. FeatureCatalog.Mandatory, "not-a-real-feature"] };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEverySubFeature_Passes()
    {
        var request = new UpdateTenantFeaturesRequest { EnabledFeatures = FeatureCatalog.Mandatory, EnabledSubFeatures = FeatureCatalog.SubFeatureKeys };

        _sut.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNoSubFeatures_Passes()
    {
        var request = new UpdateTenantFeaturesRequest { EnabledFeatures = FeatureCatalog.Mandatory, EnabledSubFeatures = [] };

        _sut.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAModuleKeyAsASubFeature_Fails()
    {
        var request = new UpdateTenantFeaturesRequest { EnabledFeatures = FeatureCatalog.Mandatory, EnabledSubFeatures = ["hr"] };

        _sut.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithASubFeatureButNotWhatItNeeds_Fails()
    {
        // Attendance picks staff from the Employees list.
        var request = new UpdateTenantFeaturesRequest
        {
            EnabledFeatures = FeatureCatalog.Mandatory,
            EnabledSubFeatures = FeatureCatalog.SubFeatureKeys.Where(key => key != "hr.employees" && key != "hr.leave-requests").ToList(),
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("'hr.attendance' requires 'hr.employees'"));
    }
}
