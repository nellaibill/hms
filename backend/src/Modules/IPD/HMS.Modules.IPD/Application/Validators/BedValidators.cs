using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateBedRequestValidator : AbstractValidator<CreateBedRequest>
{
    public CreateBedRequestValidator()
    {
        RuleFor(x => x.WardId).NotEmpty();
        RuleFor(x => x.BedNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BedType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.DailyCharge).GreaterThan(0);
    }
}

internal class UpdateBedRequestValidator : AbstractValidator<UpdateBedRequest>
{
    public UpdateBedRequestValidator()
    {
        RuleFor(x => x.BedType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.DailyCharge).GreaterThan(0);
    }
}
