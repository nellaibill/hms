using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateStorageLocationRequestValidator : AbstractValidator<CreateStorageLocationRequest>
{
    public CreateStorageLocationRequestValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.LocationCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.LocationType).MaximumLength(20);
    }
}

internal class UpdateStorageLocationRequestValidator : AbstractValidator<UpdateStorageLocationRequest>
{
    public UpdateStorageLocationRequestValidator()
    {
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.LocationType).MaximumLength(20);
    }
}
