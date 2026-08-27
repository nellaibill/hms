using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateAppointmentTypeRequestValidator : AbstractValidator<CreateAppointmentTypeRequest>
{
    public CreateAppointmentTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

internal class UpdateAppointmentTypeRequestValidator : AbstractValidator<UpdateAppointmentTypeRequest>
{
    public UpdateAppointmentTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
