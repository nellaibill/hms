using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateStockAdjustmentReasonRequestValidator : AbstractValidator<CreateStockAdjustmentReasonRequest>
{
    public CreateStockAdjustmentReasonRequestValidator()
    {
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.ReasonName).NotEmpty().MaximumLength(150);
    }
}

internal class UpdateStockAdjustmentReasonRequestValidator : AbstractValidator<UpdateStockAdjustmentReasonRequest>
{
    public UpdateStockAdjustmentReasonRequestValidator()
    {
        RuleFor(x => x.ReasonName).NotEmpty().MaximumLength(150);
    }
}
