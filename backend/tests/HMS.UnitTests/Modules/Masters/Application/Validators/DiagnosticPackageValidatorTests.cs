using FluentValidation.TestHelper;
using HMS.Modules.Masters.Application.Validators;
using HMS.Modules.Masters.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application.Validators;

public class DiagnosticPackageValidatorTests
{
    private static CreateDiagnosticPackageRequest ValidRequest(params Guid[] serviceIds) => new()
    {
        Code = "MHC",
        Name = "Master Health Checkup",
        TotalPrice = 1500m,
        IsActive = true,
        ServiceIds = serviceIds.Length > 0 ? serviceIds : [Guid.NewGuid()],
    };

    [Fact]
    public void CreateValidator_WithZeroItems_HasValidationError()
    {
        var validator = new CreateDiagnosticPackageRequestValidator();
        var request = ValidRequest() with { ServiceIds = [] };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ServiceIds);
    }

    [Fact]
    public void CreateValidator_WithAtLeastOneItem_HasNoValidationError()
    {
        var validator = new CreateDiagnosticPackageRequestValidator();
        var request = ValidRequest();

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ServiceIds);
    }
}
