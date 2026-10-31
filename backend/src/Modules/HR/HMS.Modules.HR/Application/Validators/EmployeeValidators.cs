using FluentValidation;
using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application.Validators;

internal class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).NotNull();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmergencyContactPhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.DesignationId).NotEmpty();
        RuleFor(x => x.JoiningDate).NotNull();

        When(x => x.Login is not null, () =>
        {
            RuleFor(x => x.Login!).SetValidator(new EmployeeLoginRequestValidator());
            RuleFor(x => x.UserId).Null().WithMessage("Choose either a new login or an existing one, not both.");
            RuleFor(x => x.Phone).Must(phone => MobileNumber.ToTenDigits(phone) is not null).WithMessage(EmployeeLoginRequestValidator.PhoneMessage);
        });
    }
}

internal class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).NotNull();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmergencyContactPhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.DesignationId).NotEmpty();
        RuleFor(x => x.JoiningDate).NotNull();

        When(x => x.Login is not null, () =>
        {
            RuleFor(x => x.Login!).SetValidator(new EmployeeLoginRequestValidator());
            RuleFor(x => x.UserId).Null().WithMessage("Choose either a new login or an existing one, not both.");
            RuleFor(x => x.Phone).Must(phone => MobileNumber.ToTenDigits(phone) is not null).WithMessage(EmployeeLoginRequestValidator.PhoneMessage);
        });
    }
}

/// <summary>
/// Mirrors Identity's CreateUserRequestValidator rules for the three login fields (that
/// validator is internal to Identity), so a bad username or weak password is reported against
/// the Login access card's own fields before anything is saved.
/// </summary>
internal class EmployeeLoginRequestValidator : AbstractValidator<EmployeeLoginRequest>
{
    // Identity requires a login's phone to be exactly 10 digits; the employee's Phone (max 20,
    // free text) is what the login uses, after MobileNumber.ToTenDigits.
    public const string PhoneMessage = "A login needs a 10-digit mobile number. Enter the phone as 10 digits.";

    public EmployeeLoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may contain only letters, numbers, dots, underscores and hyphens.");
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters.")
            .Matches(PasswordPolicy.ComplexityRegex)
            .WithMessage(PasswordPolicy.ComplexityMessage);
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role is required.");
    }
}
