using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateDesignationRequestValidator : AbstractValidator<CreateDesignationRequest>
{
    public CreateDesignationRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

internal class UpdateDesignationRequestValidator : AbstractValidator<UpdateDesignationRequest>
{
    public UpdateDesignationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
