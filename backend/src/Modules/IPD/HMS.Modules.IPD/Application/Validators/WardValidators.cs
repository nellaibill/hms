using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateWardRequestValidator : AbstractValidator<CreateWardRequest>
{
    public CreateWardRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.WardType).IsInEnum();
    }
}

internal class UpdateWardRequestValidator : AbstractValidator<UpdateWardRequest>
{
    public UpdateWardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.WardType).IsInEnum();
    }
}
