using FluentAssertions;
using HMS.Modules.Platform.Application.Validators;
using HMS.Modules.Platform.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Platform.Application.Validators;

public class CreateHospitalRequestValidatorTests
{
    private readonly CreateHospitalRequestValidator _sut = new();

    private static CreateHospitalRequest ValidRequest(IReadOnlyList<string>? enabledFeatureKeys = null) => new()
    {
        HospitalName = "Test Hospital",
        HospitalCode = "test-hospital",
        MobileNumber = "9876543210",
        Address = "123 Test Street",
        City = "Chennai",
        State = "Tamil Nadu",
        Pincode = "600001",
        SuperAdminUsername = "admin",
        SuperAdminFirstName = "Admin",
        SuperAdminLastName = "User",
        SuperAdminEmail = "admin@example.com",
        SuperAdminPhoneNumber = "9876543211",
        SuperAdminPassword = "StrongPass@123",
        EnabledFeatureKeys = enabledFeatureKeys ?? [],
        ImportedPatientCapacity = 40000,
    };

    [Fact]
    public void Validate_WithNoOptionalFeatures_Passes()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithPharmacyAndProducts_Passes()
    {
        var result = _sut.Validate(ValidRequest(["pharmacy", "products"]));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithPharmacyButNotProducts_Fails()
    {
        // Pharmacy's DispenseService/StockReceiptService/StockBalanceService call
        // IProductService/IProductBatchService directly and unconditionally — enabling
        // Pharmacy without Products would leave a tenant where every dispense/stock
        // operation 500s against a missing schema. See FeatureCatalog.Dependencies.
        var result = _sut.Validate(ValidRequest(["pharmacy"]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EnabledFeatureKeys");
    }

    [Fact]
    public void Validate_WithUnrecognizedFeatureKey_Fails()
    {
        var result = _sut.Validate(ValidRequest(["not-a-real-feature"]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EnabledFeatureKeys[0]");
    }
}
