using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateMedicationOrderRequestValidator : AbstractValidator<CreateMedicationOrderRequest>
{
    public CreateMedicationOrderRequestValidator()
    {
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dose).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Route).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartDate).NotEqual(default(DateTime));
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");
        RuleFor(x => x.Instructions).MaximumLength(2000);
        RuleFor(x => x.OrderedAt).NotEqual(default(DateTime));
    }
}

internal class DiscontinueMedicationOrderRequestValidator : AbstractValidator<DiscontinueMedicationOrderRequest>
{
    public DiscontinueMedicationOrderRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
