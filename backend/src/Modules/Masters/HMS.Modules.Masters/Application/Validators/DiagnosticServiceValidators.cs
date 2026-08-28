using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDiagnosticServiceRequestValidator : AbstractValidator<CreateDiagnosticServiceRequest>
{
    public CreateDiagnosticServiceRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ServiceType).IsInEnum();

        // Procedure stays exclusively on the old DiagnosticTest — this normalized entity only
        // ever covers Laboratory/Radiology (see Domain/DiagnosticService.cs).
        RuleFor(x => x.ServiceType)
            .NotEqual(DiagnosticTestServiceType.Procedure)
            .WithMessage("Procedure services must be created as a DiagnosticTest, not a DiagnosticService.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.ProviderId)
            .NotNull()
            .When(x => x.IsOutsourced)
            .WithMessage("A provider is required when the service is outsourced.");
    }
}

internal class UpdateDiagnosticServiceRequestValidator : AbstractValidator<UpdateDiagnosticServiceRequest>
{
    public UpdateDiagnosticServiceRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ServiceType).IsInEnum();

        RuleFor(x => x.ServiceType)
            .NotEqual(DiagnosticTestServiceType.Procedure)
            .WithMessage("Procedure services must be created as a DiagnosticTest, not a DiagnosticService.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.ProviderId)
            .NotNull()
            .When(x => x.IsOutsourced)
            .WithMessage("A provider is required when the service is outsourced.");
    }
}
