using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateTaxRequestValidator : AbstractValidator<CreateTaxRequest>
{
    public CreateTaxRequestValidator()
    {
        RuleFor(x => x.TaxCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TaxName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TaxType).MaximumLength(20);
        RuleFor(x => x.RatePercent).GreaterThan(0).WithMessage("Tax rate must be greater than 0.");
    }
}

internal class UpdateTaxRequestValidator : AbstractValidator<UpdateTaxRequest>
{
    public UpdateTaxRequestValidator()
    {
        RuleFor(x => x.TaxName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TaxType).MaximumLength(20);
        RuleFor(x => x.RatePercent).GreaterThan(0).WithMessage("Tax rate must be greater than 0.");
    }
}
