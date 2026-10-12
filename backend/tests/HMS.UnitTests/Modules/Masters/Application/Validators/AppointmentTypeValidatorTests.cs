using FluentValidation.TestHelper;
using HMS.Modules.Masters.Application.Validators;
using HMS.Modules.Masters.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application.Validators;

public class AppointmentTypeValidatorTests
{
    private static CreateAppointmentTypeRequest ValidRequest(int? priority = null) => new()
    {
        Name = "Regular (Walk-in)",
        IsActive = true,
        Priority = priority,
    };

    [Fact]
    public void CreateValidator_WithNoPriority_IsValid()
    {
        var validator = new CreateAppointmentTypeRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void CreateValidator_WithPositivePriority_IsValid()
    {
        var validator = new CreateAppointmentTypeRequestValidator();

        var result = validator.TestValidate(ValidRequest(priority: 1));

        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateValidator_WithNonPositivePriority_IsInvalid(int priority)
    {
        var validator = new CreateAppointmentTypeRequestValidator();

        var result = validator.TestValidate(ValidRequest(priority));

        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateValidator_WithNonPositivePriority_IsInvalid(int priority)
    {
        var validator = new UpdateAppointmentTypeRequestValidator();

        var result = validator.TestValidate(new UpdateAppointmentTypeRequest { Name = "Regular (Walk-in)", IsActive = true, Priority = priority });

        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }
}
