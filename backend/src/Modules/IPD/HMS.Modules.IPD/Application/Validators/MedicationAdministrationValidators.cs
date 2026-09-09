using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateMedicationAdministrationRequestValidator : AbstractValidator<CreateMedicationAdministrationRequest>
{
    public CreateMedicationAdministrationRequestValidator()
    {
        RuleFor(x => x.ScheduledTime).NotEqual(default(DateTime));
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
