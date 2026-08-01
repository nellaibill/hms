using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductPriceRequestValidator : AbstractValidator<CreateProductPriceRequest>
{
    public CreateProductPriceRequestValidator()
    {
        RuleFor(x => x.PriceType).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue);
    }
}

internal class UpdateProductPriceRequestValidator : AbstractValidator<UpdateProductPriceRequest>
{
    public UpdateProductPriceRequestValidator()
    {
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue);
    }
}
