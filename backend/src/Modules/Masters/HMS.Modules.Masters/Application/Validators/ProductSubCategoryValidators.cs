using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateProductSubCategoryRequestValidator : AbstractValidator<CreateProductSubCategoryRequest>
{
    public CreateProductSubCategoryRequestValidator()
    {
        RuleFor(x => x.SubCategoryCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.SubCategoryName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

internal class UpdateProductSubCategoryRequestValidator : AbstractValidator<UpdateProductSubCategoryRequest>
{
    public UpdateProductSubCategoryRequestValidator()
    {
        RuleFor(x => x.SubCategoryName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
