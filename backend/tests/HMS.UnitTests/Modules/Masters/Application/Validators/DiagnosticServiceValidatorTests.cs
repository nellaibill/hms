using FluentValidation.TestHelper;
using HMS.Modules.Masters.Application.Validators;
using HMS.Modules.Masters.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application.Validators;

public class DiagnosticServiceValidatorTests
{
    private static CreateDiagnosticServiceRequest ValidRequest(DiagnosticTestServiceType serviceType = DiagnosticTestServiceType.Laboratory) => new()
    {
        Code = "CBC",
        Name = "Complete Blood Count",
        CategoryId = Guid.NewGuid(),
        ServiceType = serviceType,
        IsOutsourced = false,
        Price = 250m,
        IsActive = true,
    };

    [Fact]
    public void CreateValidator_RejectsProcedureServiceType()
    {
        var validator = new CreateDiagnosticServiceRequestValidator();
        var request = ValidRequest(DiagnosticTestServiceType.Procedure);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ServiceType);
    }

    [Theory]
    [InlineData(DiagnosticTestServiceType.Laboratory)]
    [InlineData(DiagnosticTestServiceType.Radiology)]
    public void CreateValidator_AcceptsLaboratoryOrRadiology(DiagnosticTestServiceType serviceType)
    {
        var validator = new CreateDiagnosticServiceRequestValidator();
        var request = ValidRequest(serviceType);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ServiceType);
    }

    [Fact]
    public void CreateValidator_WhenOutsourcedWithoutProvider_HasValidationError()
    {
        var validator = new CreateDiagnosticServiceRequestValidator();
        var request = ValidRequest() with { IsOutsourced = true, ProviderId = null };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ProviderId);
    }

    [Fact]
    public void CreateValidator_WhenOutsourcedWithProvider_HasNoValidationError()
    {
        var validator = new CreateDiagnosticServiceRequestValidator();
        var request = ValidRequest() with { IsOutsourced = true, ProviderId = Guid.NewGuid() };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ProviderId);
    }
}
