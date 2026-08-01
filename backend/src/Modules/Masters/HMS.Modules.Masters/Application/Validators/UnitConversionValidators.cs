using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateUnitConversionRequestValidator : AbstractValidator<CreateUnitConversionRequest>
{
    public CreateUnitConversionRequestValidator()
    {
        RuleFor(x => x.FromUomId).NotEmpty();
        RuleFor(x => x.ToUomId).NotEmpty();
        RuleFor(x => x).Must(x => x.FromUomId != x.ToUomId).WithMessage("From Unit and To Unit cannot be the same.").WithName("ToUomId");
        RuleFor(x => x.ConversionFactor).GreaterThan(0);
    }
}

internal class UpdateUnitConversionRequestValidator : AbstractValidator<UpdateUnitConversionRequest>
{
    public UpdateUnitConversionRequestValidator()
    {
        RuleFor(x => x.ConversionFactor).GreaterThan(0);
    }
}
