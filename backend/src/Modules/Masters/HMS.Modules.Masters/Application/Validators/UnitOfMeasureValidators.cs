using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateUnitOfMeasureRequestValidator : AbstractValidator<CreateUnitOfMeasureRequest>
{
    public CreateUnitOfMeasureRequestValidator()
    {
        RuleFor(x => x.UomCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UomName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UomType).MaximumLength(20);
    }
}

internal class UpdateUnitOfMeasureRequestValidator : AbstractValidator<UpdateUnitOfMeasureRequest>
{
    public UpdateUnitOfMeasureRequestValidator()
    {
        RuleFor(x => x.UomName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UomType).MaximumLength(20);
    }
}
