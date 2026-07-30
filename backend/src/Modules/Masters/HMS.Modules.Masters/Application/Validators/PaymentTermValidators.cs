using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreatePaymentTermRequestValidator : AbstractValidator<CreatePaymentTermRequest>
{
    public CreatePaymentTermRequestValidator()
    {
        RuleFor(x => x.TermName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Days).GreaterThanOrEqualTo(0);
    }
}

internal class UpdatePaymentTermRequestValidator : AbstractValidator<UpdatePaymentTermRequest>
{
    public UpdatePaymentTermRequestValidator()
    {
        RuleFor(x => x.TermName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Days).GreaterThanOrEqualTo(0);
    }
}
