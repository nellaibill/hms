using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateProductGroupRequestValidator : AbstractValidator<CreateProductGroupRequest>
{
    public CreateProductGroupRequestValidator()
    {
        RuleFor(x => x.GroupCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SubCategoryId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

internal class UpdateProductGroupRequestValidator : AbstractValidator<UpdateProductGroupRequest>
{
    public UpdateProductGroupRequestValidator()
    {
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SubCategoryId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
