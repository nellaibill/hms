using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.WarehouseCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.WarehouseName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
    }
}

internal class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.WarehouseName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
    }
}
