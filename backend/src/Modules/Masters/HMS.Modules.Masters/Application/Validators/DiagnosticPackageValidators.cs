using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDiagnosticPackageRequestValidator : AbstractValidator<CreateDiagnosticPackageRequest>
{
    public CreateDiagnosticPackageRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.TotalPrice).GreaterThanOrEqualTo(0).WithMessage("Total price cannot be negative.");

        // A package with zero tests isn't a bundle worth creating — mirrors
        // CreateInvoiceRequestValidator's identical Items.NotEmpty() rule.
        RuleFor(x => x.ServiceIds).NotEmpty().WithMessage("A package must contain at least one test.");
    }
}

internal class UpdateDiagnosticPackageRequestValidator : AbstractValidator<UpdateDiagnosticPackageRequest>
{
    public UpdateDiagnosticPackageRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.TotalPrice).GreaterThanOrEqualTo(0).WithMessage("Total price cannot be negative.");
    }
}

internal class AddDiagnosticPackageItemRequestValidator : AbstractValidator<AddDiagnosticPackageItemRequest>
{
    public AddDiagnosticPackageItemRequestValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
    }
}
