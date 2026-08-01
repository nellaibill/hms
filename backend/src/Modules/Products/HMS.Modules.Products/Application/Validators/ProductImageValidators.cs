using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class UpdateProductImageRequestValidator : AbstractValidator<UpdateProductImageRequest>
{
    public UpdateProductImageRequestValidator()
    {
        RuleFor(x => x.ImageType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
